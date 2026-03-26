using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Content.Server.Database;
using Content.Shared._Stalker.PersistentCrafting;
using Content.Shared.Actions;
using Content.Shared.DoAfter;
using Content.Shared.GameTicking;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Popups;
using Content.Shared.Stacks;
using Content.Shared.Tag;
using Robust.Server.GameObjects;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;

namespace Content.Server._Stalker.PersistentCrafting;

public sealed class PersistentCraftingSystem : EntitySystem
{
    [Dependency] private readonly IServerDbManager _db = default!;
    [Dependency] private readonly SharedActionsSystem _actions = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;
    [Dependency] private readonly SharedHandsSystem _hands = default!;
    [Dependency] private readonly IPrototypeManager _proto = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedStackSystem _stacks = default!;
    [Dependency] private readonly TagSystem _tag = default!;

    private List<PersistentCraftNodePrototype> _nodeCache = new();
    private List<PersistentCraftRecipePrototype> _recipeCache = new();

    public override void Initialize()
    {
        base.Initialize();

        _nodeCache = _proto.EnumeratePrototypes<PersistentCraftNodePrototype>().ToList();
        _recipeCache = _proto.EnumeratePrototypes<PersistentCraftRecipePrototype>().ToList();

        SubscribeLocalEvent<PlayerSpawnCompleteEvent>(OnPlayerSpawnComplete);
        SubscribeLocalEvent<PersistentCraftAccessComponent, ComponentStartup>(OnAccessStartup);
        SubscribeLocalEvent<PersistentCraftAccessComponent, ComponentShutdown>(OnAccessShutdown);
        SubscribeLocalEvent<PersistentCraftAccessComponent, OpenPersistentCraftMenuActionEvent>(OnOpenCraftMenu);
        SubscribeLocalEvent<PersistentCraftAccessComponent, PersistentCraftDoAfterEvent>(OnCraftDoAfter);
        SubscribeNetworkEvent<RequestPersistentCraftStateEvent>(OnRequestState);
        SubscribeNetworkEvent<RequestPersistentCraftRecipeEvent>(OnRequestCraftRecipe);
        SubscribeNetworkEvent<RequestPersistentCraftUnlockEvent>(OnRequestUnlock);
    }

    public PersistentCraftState GetState(EntityUid uid)
    {
        if (!TryComp(uid, out PersistentCraftProfileComponent? profile))
        {
            var defaultProfile = new PersistentCraftProfileComponent
            {
                BranchProgress = CreateDefaultBranchProfiles(),
            };

            return new PersistentCraftState(
                false,
                BuildBranchStates(defaultProfile),
                new List<PersistentCraftTierState>(),
                new List<string>());
        }

        return new PersistentCraftState(
            profile.Loaded,
            BuildBranchStates(profile),
            BuildTierStates(profile),
            profile.UnlockedNodes.OrderBy(id => id).ToList());
    }

    public bool IsLoaded(EntityUid uid)
    {
        return TryComp(uid, out PersistentCraftProfileComponent? profile) && profile.Loaded;
    }

    public async Task<bool> ResetProfileAsync(EntityUid uid)
    {
        if (!TryComp(uid, out PersistentCraftProfileComponent? profile))
            return false;

        if (profile.UserId == Guid.Empty || string.IsNullOrWhiteSpace(profile.CharacterName))
            return false;

        profile.BranchProgress = CreateDefaultBranchProfiles();
        profile.UnlockedNodes.Clear();
        EnsureAutoTierNodesUnlocked(profile);
        RecalculateBranchPoints(profile);
        profile.Loaded = true;

        await SaveProfileAsync(uid, profile);
        SendStateToAttachedActor(uid);
        return true;
    }

    public bool HasNode(EntityUid uid, string nodeId)
    {
        return TryComp(uid, out PersistentCraftProfileComponent? profile) &&
               profile.UnlockedNodes.Contains(nodeId);
    }

    public bool MeetsRequirement(
        EntityUid uid,
        PersistentCraftBranch branch,
        int tier)
    {
        if (!TryComp(uid, out PersistentCraftProfileComponent? profile))
            return false;

        var requiredNodes = _recipeCache
            .Where(recipe => recipe.Branch == branch && recipe.Tier == tier)
            .Select(recipe => recipe.RequiredNode)
            .Where(nodeId => !string.IsNullOrWhiteSpace(nodeId))
            .Distinct();

        return requiredNodes.Any(nodeId => HasNodeUnlockedOrAutoAvailable(profile, nodeId));
    }

    private void OnAccessStartup(EntityUid uid, PersistentCraftAccessComponent component, ComponentStartup args)
    {
        _actions.AddAction(uid, ref component.ActionEntity, component.Action, uid);
    }

    private void OnAccessShutdown(EntityUid uid, PersistentCraftAccessComponent component, ComponentShutdown args)
    {
        _actions.RemoveAction(uid, component.ActionEntity);
        component.ActionEntity = null;
    }

    private void OnPlayerSpawnComplete(PlayerSpawnCompleteEvent args)
    {
        var profile = EnsureComp<PersistentCraftProfileComponent>(args.Mob);
        profile.UserId = args.Player.UserId.UserId;
        profile.CharacterName = args.Profile.Name;
        profile.BranchProgress = CreateDefaultBranchProfiles();
        profile.UnlockedNodes.Clear();
        EnsureAutoTierNodesUnlocked(profile);
        RecalculateBranchPoints(profile);
        profile.Loaded = false;

        LoadProfileAsync(args.Mob, profile.UserId, profile.CharacterName);
    }

    private void OnOpenCraftMenu(EntityUid uid, PersistentCraftAccessComponent component, OpenPersistentCraftMenuActionEvent args)
    {
        if (args.Handled)
            return;

        args.Handled = true;

        if (!TryComp(args.Performer, out ActorComponent? actor))
            return;

        RaiseNetworkEvent(new OpenPersistentCraftMenuEvent(), actor.PlayerSession);
        SendState(actor.PlayerSession, args.Performer);
    }

    private void OnRequestState(RequestPersistentCraftStateEvent ev, EntitySessionEventArgs args)
    {
        if (args.SenderSession.AttachedEntity is not { Valid: true } user)
            return;

        SendState(args.SenderSession, user);
    }

    private void OnRequestCraftRecipe(RequestPersistentCraftRecipeEvent ev, EntitySessionEventArgs args)
    {
        if (args.SenderSession.AttachedEntity is not { Valid: true } user)
            return;

        if (!HasComp<PersistentCraftAccessComponent>(user))
            return;

        if (!_proto.TryIndex<PersistentCraftRecipePrototype>(ev.RecipeId, out var recipe))
            return;

        if (!IsLoaded(user))
        {
            PopupUser(user, "persistent-craft-popup-loading");
            SendState(args.SenderSession, user);
            return;
        }

        if (!MeetsRecipeRequirement(user, recipe))
        {
            PopupUser(user, "persistent-craft-station-popup-skill-locked");
            SendState(args.SenderSession, user);
            return;
        }

        if (!TryPlanIngredientConsumption(user, recipe, out _))
        {
            PopupUser(user, "persistent-craft-station-popup-missing-items");
            SendState(args.SenderSession, user);
            return;
        }

        var craftTime = GetEffectiveCraftTime(user, recipe);
        var doAfter = new DoAfterArgs(EntityManager, user, craftTime, new PersistentCraftDoAfterEvent(recipe.ID), user, target: user, used: user)
        {
            BreakOnMove = true,
            BreakOnDamage = true,
            NeedHand = false,
            RequireCanInteract = true,
        };

        if (!_doAfter.TryStartDoAfter(doAfter))
            return;

        _popup.PopupEntity(
            Loc.GetString("persistent-craft-station-popup-started", ("recipe", ResolveRecipeName(recipe))),
            user,
            user);
    }

    private void OnRequestUnlock(RequestPersistentCraftUnlockEvent ev, EntitySessionEventArgs args)
    {
        if (args.SenderSession.AttachedEntity is not { Valid: true } user)
            return;

        if (!TryComp(user, out PersistentCraftProfileComponent? profile))
            return;

        if (!profile.Loaded)
        {
            _popup.PopupEntity(Loc.GetString("persistent-craft-popup-loading"), user, user);
            return;
        }

        if (!_proto.TryIndex<PersistentCraftNodePrototype>(ev.NodeId, out var node))
            return;

        if (IsAutoUnlockedNode(node))
        {
            _popup.PopupEntity(Loc.GetString("persistent-craft-popup-tier-auto"), user, user);
            return;
        }

        if (profile.UnlockedNodes.Contains(node.ID))
        {
            _popup.PopupEntity(Loc.GetString("persistent-craft-popup-already-unlocked"), user, user);
            return;
        }

        var branchProfile = GetOrCreateBranchProfile(profile, node.Branch);
        if (!AreNodePrerequisitesMet(profile, node))
        {
            _popup.PopupEntity(Loc.GetString("persistent-craft-popup-prerequisite"), user, user);
            return;
        }

        if (branchProfile.AvailablePoints < node.Cost)
        {
            _popup.PopupEntity(Loc.GetString("persistent-craft-popup-not-enough-points"), user, user);
            return;
        }

        branchProfile.AvailablePoints = Math.Max(0, branchProfile.AvailablePoints - node.Cost);
        profile.UnlockedNodes.Add(node.ID);
        RecalculateBranchPoints(profile);

        _ = SaveProfileAsync(user, profile);

        _popup.PopupEntity(
            Loc.GetString("persistent-craft-popup-unlocked", ("skill", Loc.GetString(node.Name))),
            user,
            user);

        SendState(args.SenderSession, user);
    }

    private void OnCraftDoAfter(EntityUid uid, PersistentCraftAccessComponent component, PersistentCraftDoAfterEvent args)
    {
        if (args.Handled || args.Cancelled)
            return;

        args.Handled = true;

        if (!_proto.TryIndex<PersistentCraftRecipePrototype>(args.RecipeId, out var recipe))
            return;

        if (!Exists(args.User) || args.User != uid)
            return;

        if (!IsLoaded(args.User))
        {
            PopupUser(args.User, "persistent-craft-popup-loading");
            SendStateToAttachedActor(args.User);
            return;
        }

        if (!MeetsRecipeRequirement(args.User, recipe))
        {
            PopupUser(args.User, "persistent-craft-station-popup-skill-locked");
            SendStateToAttachedActor(args.User);
            return;
        }

        if (!TryPlanIngredientConsumption(args.User, recipe, out var plan))
        {
            PopupUser(args.User, "persistent-craft-station-popup-missing-items");
            SendStateToAttachedActor(args.User);
            return;
        }

        ConsumeIngredientPlan(plan);
        SpawnResults(args.User, recipe);
        GrantCraftPoints(args.User, recipe);

        _popup.PopupEntity(
            Loc.GetString("persistent-craft-station-popup-crafted", ("recipe", ResolveRecipeName(recipe))),
            args.User,
            args.User);

        var pointsReward = PersistentCraftingHelper.GetPointReward(recipe);
        if (pointsReward > 0)
        {
            _popup.PopupEntity(
                Loc.GetString("persistent-craft-popup-points-gained", ("points", pointsReward)),
                args.User,
                args.User);
        }

        _ = SaveProfileAsync(args.User, Comp<PersistentCraftProfileComponent>(args.User));
        SendStateToAttachedActor(args.User);
    }

    private void SendState(ICommonSession session, EntityUid uid)
    {
        RaiseNetworkEvent(new PersistentCraftStateEvent(GetState(uid)), session);
    }

    private void SendStateToAttachedActor(EntityUid uid)
    {
        if (!TryComp(uid, out ActorComponent? actor))
            return;

        SendState(actor.PlayerSession, uid);
    }

    private async void LoadProfileAsync(EntityUid uid, Guid userId, string characterName)
    {
        try
        {
            var saved = await _db.GetStalkerPersistentCraftProfileAsync(userId, characterName);

            if (Deleted(uid) || !TryComp(uid, out PersistentCraftProfileComponent? profile))
                return;

            profile.BranchProgress = CreateDefaultBranchProfiles();
            profile.UnlockedNodes.Clear();

            if (saved is not null)
            {
                var saveData = DeserializeSaveData(saved.UnlockedNodesJson, saved.AvailablePoints, saved.SpentPoints, characterName);
                profile.BranchProgress = BuildBranchProfiles(saveData.Branches);
                profile.UnlockedNodes = new HashSet<string>(saveData.UnlockedNodes);
                ApplyTierProgress(profile.BranchProgress, saveData.Tiers, saveData.Nodes);
            }

            EnsureAutoTierNodesUnlocked(profile);
            RecalculateBranchPoints(profile);
            profile.Loaded = true;

            if (TryComp(uid, out ActorComponent? actor))
                SendState(actor.PlayerSession, uid);
        }
        catch (Exception ex)
        {
            Log.Error($"Failed to load persistent craft profile for {characterName}: {ex}");
        }
    }

    private void EnsureAutoTierNodesUnlocked(PersistentCraftProfileComponent profile)
    {
        var changed = true;
        while (changed)
        {
            changed = false;

            foreach (var node in _nodeCache)
            {
                if (!IsAutoUnlockedNode(node))
                    continue;

                if (!AreNodePrerequisitesMet(profile, node))
                    continue;

                if (profile.UnlockedNodes.Add(node.ID))
                    changed = true;
            }
        }
    }

    private async Task SaveProfileAsync(EntityUid uid, PersistentCraftProfileComponent profile)
    {
        try
        {
            var totalAvailablePoints = PersistentCraftingHelper.EnumerateBranches()
                .Sum(branch => GetTotalAvailableTierPoints(profile, branch));
            var totalSpentPoints = PersistentCraftingHelper.EnumerateBranches()
                .Sum(branch => GetTotalSpentTierPoints(profile, branch));

            await _db.SetStalkerPersistentCraftProfileAsync(
                profile.UserId,
                profile.CharacterName,
                totalAvailablePoints,
                totalSpentPoints,
                0,
                JsonSerializer.Serialize(new PersistentCraftSaveData
                {
                    Branches = profile.BranchProgress
                        .OrderBy(pair => pair.Key)
                        .Select(pair => new PersistentCraftBranchSaveData
                        {
                            Branch = pair.Key,
                            AvailablePoints = GetTotalAvailableTierPoints(profile, pair.Key),
                            SpentPoints = GetTotalSpentTierPoints(profile, pair.Key),
                            Level = Math.Max(PersistentCraftingHelper.InitialLevel, pair.Value.Level),
                            SubLevel = PersistentCraftingHelper.DefaultSubLevel,
                            Experience = Math.Max(0, pair.Value.Experience),
                        })
                        .ToList(),
                    Tiers = profile.BranchProgress
                        .OrderBy(pair => pair.Key)
                        .SelectMany(pair => pair.Value.TierProgress
                            .OrderBy(tier => tier.Key)
                            .Select(tier => new PersistentCraftTierSaveData
                            {
                                Branch = pair.Key,
                                Tier = tier.Key,
                                ProgressLevel = Math.Max(PersistentCraftingHelper.InitialTierProgressLevel, tier.Value.ProgressLevel),
                                Experience = Math.Max(0, tier.Value.Experience),
                            }))
                        .ToList(),
                    UnlockedNodes = profile.UnlockedNodes.OrderBy(id => id).ToList(),
                }));
        }
        catch (Exception ex)
        {
            Log.Error($"Failed to save persistent craft profile for {profile.CharacterName}: {ex}");
            if (!Deleted(uid) && TryComp(uid, out ActorComponent? actor))
                _popup.PopupEntity(Loc.GetString("persistent-craft-save-failed"), uid, actor.PlayerSession, PopupType.MediumCaution);
        }
    }

    private PersistentCraftSaveData DeserializeSaveData(string json, int legacyAvailablePoints, int legacySpentPoints, string characterName)
    {
        try
        {
            var data = JsonSerializer.Deserialize<PersistentCraftSaveData>(json);
            if (data?.UnlockedNodes != null || data?.Branches != null)
                return NormalizeSaveData(data ?? CreateDefaultSaveData());
        }
        catch (Exception ex)
        {
            Log.Warning($"[PersistentCraft] New-format parse failed for '{characterName}': {ex.Message}");
        }

        try
        {
            var legacyData = JsonSerializer.Deserialize<LegacyPersistentCraftSaveData>(json);
            if (legacyData?.UnlockedNodes != null)
            {
                return ConvertLegacySaveData(
                    legacyData.UnlockedNodes,
                    legacyAvailablePoints,
                    legacySpentPoints);
            }
        }
        catch (Exception ex)
        {
            Log.Warning($"[PersistentCraft] Legacy-format parse failed for '{characterName}': {ex.Message}");
        }

        try
        {
            var unlockedNodes = JsonSerializer.Deserialize<HashSet<string>>(json) ?? new HashSet<string>();
            return ConvertLegacySaveData(unlockedNodes, legacyAvailablePoints, legacySpentPoints);
        }
        catch (Exception ex)
        {
            Log.Error($"[PersistentCraft] All parse attempts failed for '{characterName}', resetting to defaults: {ex.Message}");
            return CreateDefaultSaveData();
        }
    }

    private static PersistentCraftSaveData NormalizeSaveData(PersistentCraftSaveData data)
    {
        var normalized = CreateDefaultSaveData();
        normalized.UnlockedNodes = (data.UnlockedNodes ?? new List<string>())
            .Distinct()
            .OrderBy(id => id)
            .ToList();
        normalized.Nodes = (data.Nodes ?? new List<PersistentCraftNodeSaveData>())
            .Where(node => !string.IsNullOrWhiteSpace(node.NodeId))
            .GroupBy(node => node.NodeId)
            .Select(group => group.Last())
            .OrderBy(node => node.NodeId)
            .ToList();
        normalized.Tiers = (data.Tiers ?? new List<PersistentCraftTierSaveData>())
            .Where(tier => tier.Tier > 0)
            .GroupBy(tier => (tier.Branch, tier.Tier))
            .Select(group => group.Last())
            .OrderBy(tier => tier.Branch)
            .ThenBy(tier => tier.Tier)
            .ToList();

        if (data.Branches == null)
            return normalized;

        foreach (var branchData in data.Branches)
        {
            var existing = normalized.Branches.FirstOrDefault(branch => branch.Branch == branchData.Branch);
            if (existing == null)
                continue;

            existing.AvailablePoints = Math.Max(0, branchData.AvailablePoints);
            existing.SpentPoints = Math.Max(0, branchData.SpentPoints);
            existing.Level = Math.Max(PersistentCraftingHelper.InitialLevel, branchData.Level);
            existing.SubLevel = PersistentCraftingHelper.DefaultSubLevel;
            existing.Experience = Math.Max(0, branchData.Experience);
        }

        return normalized;
    }

    private static PersistentCraftSaveData ConvertLegacySaveData(
        IEnumerable<string> unlockedNodes,
        int legacyAvailablePoints,
        int legacySpentPoints)
    {
        var converted = CreateDefaultSaveData();
        converted.UnlockedNodes = unlockedNodes
            .Distinct()
            .OrderBy(id => id)
            .ToList();

        foreach (var branchData in converted.Branches)
        {
            branchData.Level = Math.Max(
                PersistentCraftingHelper.InitialLevel,
                GetHighestUnlockedTier(branchData.Branch, converted.UnlockedNodes));
        }

        var branchCount = converted.Branches.Count;
        var availableBase = Math.Max(0, legacyAvailablePoints) / branchCount;
        var availableRemainder = Math.Max(0, legacyAvailablePoints) % branchCount;
        var spentBase = Math.Max(0, legacySpentPoints) / branchCount;
        var spentRemainder = Math.Max(0, legacySpentPoints) % branchCount;

        for (var i = 0; i < branchCount; i++)
        {
            converted.Branches[i].AvailablePoints += availableBase + (i < availableRemainder ? 1 : 0);
            converted.Branches[i].SpentPoints += spentBase + (i < spentRemainder ? 1 : 0);
        }

        return converted;
    }

    private static int GetHighestUnlockedTier(PersistentCraftBranch branch, IEnumerable<string> unlockedNodes)
    {
        var highestTier = PersistentCraftingHelper.InitialLevel;

        foreach (var nodeId in unlockedNodes)
        {
            if (!TryGetBranchFromNodeId(nodeId, out var nodeBranch) || nodeBranch != branch)
                continue;

            if (TryGetTierFromNodeId(nodeId, out var tier))
                highestTier = Math.Max(highestTier, tier);
        }

        return highestTier;
    }

    private static bool TryGetBranchFromNodeId(string nodeId, out PersistentCraftBranch branch)
    {
        if (nodeId.StartsWith("PersistentCraftWeapon", StringComparison.Ordinal))
        {
            branch = PersistentCraftBranch.Weapon;
            return true;
        }

        if (nodeId.StartsWith("PersistentCraftArmor", StringComparison.Ordinal))
        {
            branch = PersistentCraftBranch.Armor;
            return true;
        }

        if (nodeId.StartsWith("PersistentCraftAnomaly", StringComparison.Ordinal))
        {
            branch = PersistentCraftBranch.Anomaly;
            return true;
        }

        branch = PersistentCraftBranch.Weapon;
        return false;
    }

    private static bool TryGetTierFromNodeId(string nodeId, out int tier)
    {
        var tierIndex = nodeId.LastIndexOf('T');
        if (tierIndex < 0 || tierIndex >= nodeId.Length - 1)
        {
            tier = 0;
            return false;
        }

        var digits = new string(nodeId
            .Skip(tierIndex + 1)
            .TakeWhile(char.IsDigit)
            .ToArray());

        return int.TryParse(digits, out tier);
    }

    private static PersistentCraftSaveData CreateDefaultSaveData()
    {
        return new PersistentCraftSaveData
        {
            Branches = PersistentCraftingHelper.EnumerateBranches()
                .Select(branch => new PersistentCraftBranchSaveData
                {
                    Branch = branch,
                    Level = PersistentCraftingHelper.InitialLevel,
                    SubLevel = PersistentCraftingHelper.DefaultSubLevel,
                })
                .ToList(),
            Tiers = new List<PersistentCraftTierSaveData>(),
            UnlockedNodes = new List<string>(),
        };
    }

    private static Dictionary<PersistentCraftBranch, PersistentCraftBranchProfile> CreateDefaultBranchProfiles()
    {
        return PersistentCraftingHelper.EnumerateBranches()
            .ToDictionary(branch => branch, _ => new PersistentCraftBranchProfile());
    }

    private static Dictionary<PersistentCraftBranch, PersistentCraftBranchProfile> BuildBranchProfiles(
        IEnumerable<PersistentCraftBranchSaveData> branches)
    {
        var result = CreateDefaultBranchProfiles();

        foreach (var branch in branches)
        {
            result[branch.Branch] = new PersistentCraftBranchProfile
            {
                AvailablePoints = Math.Max(0, branch.AvailablePoints),
                SpentPoints = Math.Max(0, branch.SpentPoints),
                Level = Math.Max(PersistentCraftingHelper.InitialLevel, branch.Level),
                SubLevel = PersistentCraftingHelper.DefaultSubLevel,
                Experience = Math.Max(0, branch.Experience),
            };
        }

        return result;
    }

    private void ApplyTierProgress(
        Dictionary<PersistentCraftBranch, PersistentCraftBranchProfile> branches,
        IEnumerable<PersistentCraftTierSaveData> tiers,
        IEnumerable<PersistentCraftNodeSaveData> nodes)
    {
        var appliedTierProgress = false;

        foreach (var tier in tiers)
        {
            if (tier.Tier <= 0)
                continue;

            var branchProfile = GetOrCreateBranchProfile(branches, tier.Branch);
            var maxProgressLevel = GetTierMaxProgressLevel(tier.Branch, tier.Tier);
            branchProfile.TierProgress[tier.Tier] = new PersistentCraftTierProfile
            {
                ProgressLevel = Math.Clamp(tier.ProgressLevel, PersistentCraftingHelper.InitialTierProgressLevel, maxProgressLevel),
                Experience = Math.Max(0, tier.Experience),
            };
            appliedTierProgress = true;
        }

        if (appliedTierProgress)
            return;

        var legacyTierGroups = nodes
            .Where(node => !string.IsNullOrWhiteSpace(node.NodeId) &&
                           TryGetBranchFromNodeId(node.NodeId, out _) &&
                           TryGetTierFromNodeId(node.NodeId, out _))
            .GroupBy(node =>
            {
                TryGetBranchFromNodeId(node.NodeId, out var branch);
                TryGetTierFromNodeId(node.NodeId, out var tier);
                return (branch, tier);
            });

        foreach (var group in legacyTierGroups)
        {
            var maxProgressLevel = GetTierMaxProgressLevel(group.Key.branch, group.Key.tier);
            var progressLevel = Math.Clamp(
                group.Max(node => node.MasteryLevel),
                PersistentCraftingHelper.InitialTierProgressLevel,
                maxProgressLevel);
            var experience = group
                .Where(node => node.MasteryLevel == progressLevel)
                .Select(node => node.Experience)
                .DefaultIfEmpty(0)
                .Max();

            var branchProfile = GetOrCreateBranchProfile(branches, group.Key.branch);
            branchProfile.TierProgress[group.Key.tier] = new PersistentCraftTierProfile
            {
                ProgressLevel = progressLevel,
                Experience = Math.Max(0, experience),
            };
        }
    }

    private List<PersistentCraftTierState> BuildTierStates(
        PersistentCraftProfileComponent profile)
    {
        var result = new List<PersistentCraftTierState>();
        var branches = profile.BranchProgress;

        foreach (var branch in PersistentCraftingHelper.EnumerateBranches())
        {
            var branchProfile = GetOrCreateBranchProfile(branches, branch);

            foreach (var tier in GetKnownTierIds(branch, branchProfile))
            {
                var maxProgressLevel = GetTierMaxProgressLevel(branch, tier);

                result.Add(new PersistentCraftTierState(
                    branch,
                    tier,
                    PersistentCraftingHelper.InitialTierProgressLevel,
                    maxProgressLevel,
                    GetAvailableTierPoints(profile, branch, tier),
                    GetSpentTierPoints(profile, branch, tier),
                    0,
                    0));
            }
        }

        return result;
    }

    private List<PersistentCraftBranchState> BuildBranchStates(
        PersistentCraftProfileComponent profile)
    {
        var result = new List<PersistentCraftBranchState>();
        var branches = profile.BranchProgress;

        foreach (var branch in PersistentCraftingHelper.EnumerateBranches())
        {
            var branchProfile = GetOrCreateBranchProfile(branches, branch);
            NormalizeBranchProfile(branch, branchProfile);
            result.Add(new PersistentCraftBranchState(
                branch,
                1,
                GetTotalAvailableTierPoints(profile, branch),
                GetTotalSpentTierPoints(profile, branch),
                PersistentCraftingHelper.InitialLevel,
                PersistentCraftingHelper.DefaultSubLevel,
                0,
                0));
        }

        return result;
    }

    private bool MeetsRecipeRequirement(EntityUid user, PersistentCraftRecipePrototype recipe)
    {
        if (!TryComp(user, out PersistentCraftProfileComponent? profile))
        {
            return false;
        }

        return HasNodeUnlockedOrAutoAvailable(profile, recipe.RequiredNode);
    }

    private bool HasNodeUnlockedOrAutoAvailable(PersistentCraftProfileComponent profile, string nodeId)
    {
        return HasNodeUnlockedOrAutoAvailable(profile, nodeId, new HashSet<string>());
    }

    private bool HasNodeUnlockedOrAutoAvailable(
        PersistentCraftProfileComponent profile,
        string nodeId,
        HashSet<string> path)
    {
        if (profile.UnlockedNodes.Contains(nodeId))
            return true;

        if (!_proto.TryIndex<PersistentCraftNodePrototype>(nodeId, out var node))
            return false;

        if (!IsAutoUnlockedNode(node))
            return false;

        if (!path.Add(nodeId))
            return false;

        try
        {
            return node.Prerequisites.All(prerequisite => HasNodeUnlockedOrAutoAvailable(profile, prerequisite, path));
        }
        finally
        {
            path.Remove(nodeId);
        }
    }

    private bool TryPlanIngredientConsumption(
        EntityUid user,
        PersistentCraftRecipePrototype recipe,
        out Dictionary<EntityUid, int> plan)
    {
        plan = new Dictionary<EntityUid, int>();
        var availableEntities = PersistentCraftInventoryHelper.CollectAccessibleEntities(EntityManager, user);

        foreach (var ingredient in recipe.Ingredients)
        {
            var remaining = GetEffectiveIngredientAmount(user, recipe, ingredient);

            foreach (var entity in availableEntities)
            {
                if (remaining <= 0)
                    break;

                if (!PersistentCraftInventoryHelper.MatchesIngredient(EntityManager, _proto, _tag, entity, ingredient))
                    continue;

                var reserved = plan.GetValueOrDefault(entity);
                var availableAmount = PersistentCraftInventoryHelper.GetUsableAmount(EntityManager, entity) - reserved;
                if (availableAmount <= 0)
                    continue;

                var taken = Math.Min(availableAmount, remaining);
                plan[entity] = reserved + taken;
                remaining -= taken;
            }

            if (remaining > 0)
            {
                plan.Clear();
                return false;
            }
        }

        return true;
    }

    private void ConsumeIngredientPlan(Dictionary<EntityUid, int> plan)
    {
        foreach (var (entity, amount) in plan)
        {
            if (amount <= 0 || Deleted(entity))
                continue;

            if (TryComp<StackComponent>(entity, out var stack))
            {
                _stacks.TryUse((entity, stack), amount);
                continue;
            }

            QueueDel(entity);
        }
    }

    private void SpawnResults(EntityUid user, PersistentCraftRecipePrototype recipe)
    {
        foreach (var result in recipe.Results)
        {
            for (var i = 0; i < result.Amount; i++)
            {
                var spawned = Spawn(result.Proto, Transform(user).Coordinates);
                _hands.PickupOrDrop(user, spawned, checkActionBlocker: false, animate: false, dropNear: true);
            }
        }
    }

    private void PopupUser(EntityUid user, string locKey)
    {
        _popup.PopupEntity(Loc.GetString(locKey), user, user);
    }

    private string ResolveRecipeName(PersistentCraftRecipePrototype recipe)
    {
        var displayProto = PersistentCraftingHelper.GetDisplayPrototypeId(recipe);
        if (!string.IsNullOrWhiteSpace(displayProto) &&
            _proto.TryIndex<EntityPrototype>(displayProto, out var prototype))
        {
            return prototype.Name;
        }

        return Loc.GetString(recipe.Name);
    }

    private void GrantCraftPoints(EntityUid user, PersistentCraftRecipePrototype recipe)
    {
        if (!TryComp(user, out PersistentCraftProfileComponent? profile))
            return;

        var branchProfile = GetOrCreateBranchProfile(profile, recipe.Branch);
        branchProfile.AvailablePoints = Math.Max(0, branchProfile.AvailablePoints);
        branchProfile.AvailablePoints += PersistentCraftingHelper.GetPointReward(recipe);

        EnsureAutoTierNodesUnlocked(profile);
        RecalculateBranchPoints(profile);
    }

    private float GetEffectiveCraftTime(EntityUid user, PersistentCraftRecipePrototype recipe)
    {
        _ = user;
        return MathF.Max(0.25f, recipe.CraftTime);
    }

    private int GetEffectiveIngredientAmount(
        EntityUid user,
        PersistentCraftRecipePrototype recipe,
        PersistentCraftIngredient ingredient)
    {
        _ = user;
        _ = recipe;
        return Math.Max(1, ingredient.Amount);
    }

    private static PersistentCraftBranchProfile GetOrCreateBranchProfile(
        PersistentCraftProfileComponent profile,
        PersistentCraftBranch branch)
    {
        return GetOrCreateBranchProfile(profile.BranchProgress, branch);
    }

    private static PersistentCraftBranchProfile GetOrCreateBranchProfile(
        Dictionary<PersistentCraftBranch, PersistentCraftBranchProfile> branches,
        PersistentCraftBranch branch)
    {
        if (!branches.TryGetValue(branch, out var profile))
        {
            profile = new PersistentCraftBranchProfile();
            branches[branch] = profile;
        }

        return profile;
    }

    private PersistentCraftTierProfile GetOrCreateTierProfile(
        PersistentCraftProfileComponent profile,
        PersistentCraftBranch branch,
        int tier)
    {
        var branchProfile = GetOrCreateBranchProfile(profile, branch);
        return GetOrCreateTierProfile(branchProfile, branch, tier);
    }

    private PersistentCraftTierProfile GetOrCreateTierProfile(
        PersistentCraftBranchProfile branchProfile,
        PersistentCraftBranch branch,
        int tier)
    {
        if (!branchProfile.TierProgress.TryGetValue(tier, out var profile))
        {
            profile = new PersistentCraftTierProfile();
            branchProfile.TierProgress[tier] = profile;
        }

        var maxProgressLevel = GetTierMaxProgressLevel(branch, tier);
        profile.ProgressLevel = Math.Clamp(
            profile.ProgressLevel,
            PersistentCraftingHelper.InitialTierProgressLevel,
            maxProgressLevel);
        profile.Experience = Math.Max(0, profile.Experience);
        return profile;
    }

    private IEnumerable<int> GetKnownTierIds(PersistentCraftBranch branch, PersistentCraftBranchProfile branchProfile)
    {
        var maxLevel = GetBranchMaxLevel(branch);
        return _recipeCache
            .Where(recipe => recipe.Branch == branch && recipe.Tier > 0)
            .Select(recipe => recipe.Tier)
            .Concat(branchProfile.TierProgress.Keys)
            .Distinct()
            .Where(tier => tier <= maxLevel)
            .OrderBy(tier => tier);
    }

    private int GetTierMaxProgressLevel(PersistentCraftBranch branch, int tier)
    {
        var tierNodes = _recipeCache
            .Where(recipe => recipe.Branch == branch && recipe.Tier == tier)
            .Select(recipe => recipe.RequiredNode)
            .Where(nodeId => !string.IsNullOrWhiteSpace(nodeId))
            .Distinct()
            .Count();

        var maxProgressLevel = Math.Max(PersistentCraftingHelper.DefaultMaxTierProgressLevel, tierNodes);

        return Math.Max(PersistentCraftingHelper.InitialTierProgressLevel, maxProgressLevel);
    }

    private static bool IsAutoUnlockedNode(PersistentCraftNodePrototype node)
    {
        return node.Cost <= 0;
    }

    private bool AreNodePrerequisitesMet(PersistentCraftProfileComponent profile, PersistentCraftNodePrototype node)
    {
        return node.Prerequisites.All(prerequisite => HasNodeUnlockedOrAutoAvailable(profile, prerequisite));
    }

    private int GetSpentTierPoints(
        PersistentCraftProfileComponent profile,
        PersistentCraftBranch branch,
        int tier)
    {
        var tierNodeIds = _recipeCache
            .Where(recipe => recipe.Branch == branch && recipe.Tier == tier)
            .Select(recipe => recipe.RequiredNode)
            .Where(nodeId => !string.IsNullOrWhiteSpace(nodeId))
            .Distinct()
            .ToHashSet();

        return _nodeCache
            .Where(node => node.Branch == branch &&
                           tierNodeIds.Contains(node.ID) &&
                           node.Cost > 0 &&
                           profile.UnlockedNodes.Contains(node.ID))
            .Sum(node => node.Cost);
    }

    private int GetAvailableTierPoints(
        PersistentCraftProfileComponent profile,
        PersistentCraftBranch branch,
        int tier)
    {
        var branchProfile = GetOrCreateBranchProfile(profile, branch);
        return Math.Max(0, branchProfile.AvailablePoints);
    }

    private int GetTotalAvailableTierPoints(PersistentCraftProfileComponent profile, PersistentCraftBranch branch)
    {
        return Math.Max(0, GetOrCreateBranchProfile(profile, branch).AvailablePoints);
    }

    private int GetTotalSpentTierPoints(PersistentCraftProfileComponent profile, PersistentCraftBranch branch)
    {
        return _nodeCache
            .Where(node => node.Branch == branch &&
                           node.Cost > 0 &&
                           profile.UnlockedNodes.Contains(node.ID))
            .Sum(node => node.Cost);
    }

    private void RecalculateBranchPoints(PersistentCraftProfileComponent profile)
    {
        foreach (var branch in PersistentCraftingHelper.EnumerateBranches())
        {
            var branchProfile = GetOrCreateBranchProfile(profile, branch);
            NormalizeBranchProfile(branch, branchProfile);
            branchProfile.AvailablePoints = Math.Max(0, branchProfile.AvailablePoints);

            branchProfile.SpentPoints = GetTotalSpentTierPoints(profile, branch);
        }
    }

    private int GetBranchMaxLevel(PersistentCraftBranch branch)
    {
        var maxTier = _recipeCache
            .Where(recipe => recipe.Branch == branch && recipe.Tier > 0)
            .Select(recipe => recipe.Tier)
            .DefaultIfEmpty(PersistentCraftingHelper.InitialLevel)
            .Max();

        return Math.Max(PersistentCraftingHelper.InitialLevel, maxTier);
    }

    private void NormalizeBranchProfile(PersistentCraftBranch branch, PersistentCraftBranchProfile branchProfile)
    {
        _ = branch;
        branchProfile.Level = PersistentCraftingHelper.InitialLevel;
        branchProfile.SubLevel = PersistentCraftingHelper.DefaultSubLevel;
        branchProfile.Experience = 0;
    }

    private sealed class PersistentCraftSaveData
    {
        public List<PersistentCraftBranchSaveData> Branches { get; set; } = new();
        public List<PersistentCraftTierSaveData> Tiers { get; set; } = new();
        public List<PersistentCraftNodeSaveData> Nodes { get; set; } = new();
        public List<string> UnlockedNodes { get; set; } = new();
    }

    private sealed class PersistentCraftBranchSaveData
    {
        public PersistentCraftBranch Branch { get; set; }
        public int AvailablePoints { get; set; }
        public int SpentPoints { get; set; }
        public int Level { get; set; } = PersistentCraftingHelper.InitialLevel;
        public int SubLevel { get; set; } = PersistentCraftingHelper.DefaultSubLevel;
        public int Experience { get; set; }
    }

    private sealed class LegacyPersistentCraftSaveData
    {
        public int Level { get; set; } = PersistentCraftingHelper.InitialLevel;
        public int Experience { get; set; }
        public List<string> UnlockedNodes { get; set; } = new();
    }

    private sealed class PersistentCraftNodeSaveData
    {
        public string NodeId { get; set; } = string.Empty;
        public int MasteryLevel { get; set; } = PersistentCraftingHelper.InitialTierProgressLevel;
        public int Experience { get; set; }
    }

    private sealed class PersistentCraftTierSaveData
    {
        public PersistentCraftBranch Branch { get; set; }
        public int Tier { get; set; }
        public int ProgressLevel { get; set; } = PersistentCraftingHelper.InitialTierProgressLevel;
        public int Experience { get; set; }
    }

}
