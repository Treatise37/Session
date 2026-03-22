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

    public override void Initialize()
    {
        base.Initialize();

        _nodeCache = _proto.EnumeratePrototypes<PersistentCraftNodePrototype>().ToList();

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
            return new PersistentCraftState(
                false,
                BuildBranchStates(CreateDefaultBranchProfiles()),
                new List<PersistentCraftNodeState>(),
                new List<string>());
        }

        return new PersistentCraftState(
            profile.Loaded,
            BuildBranchStates(profile.BranchProgress),
            BuildNodeStates(profile.BranchProgress),
            profile.UnlockedNodes.OrderBy(id => id).ToList());
    }

    public bool IsLoaded(EntityUid uid)
    {
        return TryComp(uid, out PersistentCraftProfileComponent? profile) && profile.Loaded;
    }

    public bool HasNode(EntityUid uid, string nodeId)
    {
        return TryComp(uid, out PersistentCraftProfileComponent? profile) &&
               profile.UnlockedNodes.Contains(nodeId);
    }

    public bool MeetsRequirement(
        EntityUid uid,
        PersistentCraftBranch branch,
        int tier,
        int subTier = PersistentCraftingHelper.MainTierSubLevel)
    {
        return HasNode(uid, PersistentCraftingHelper.BuildNodeId(branch, tier, subTier));
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

        if (node.NodeType == PersistentCraftNodeType.MainTier)
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
        if (branchProfile.Level < PersistentCraftingHelper.GetNodeRequiredBranchLevel(node))
        {
            _popup.PopupEntity(Loc.GetString("persistent-craft-popup-tier-locked"), user, user);
            return;
        }

        if (node.Prerequisites.Any(prerequisite => !profile.UnlockedNodes.Contains(prerequisite)))
        {
            _popup.PopupEntity(Loc.GetString("persistent-craft-popup-prerequisite"), user, user);
            return;
        }

        if (branchProfile.AvailablePoints < node.Cost)
        {
            _popup.PopupEntity(Loc.GetString("persistent-craft-popup-not-enough-points"), user, user);
            return;
        }

        branchProfile.AvailablePoints -= node.Cost;
        branchProfile.SpentPoints += node.Cost;
        profile.UnlockedNodes.Add(node.ID);
        EnsureNodeProfile(profile, node);

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
        var levelUp = GrantCraftExperience(args.User, recipe);

        _popup.PopupEntity(
            Loc.GetString("persistent-craft-station-popup-crafted", ("recipe", ResolveRecipeName(recipe))),
            args.User,
            args.User);

        if (levelUp.LevelsGained > 0)
        {
            _popup.PopupEntity(
                Loc.GetString(
                    "persistent-craft-popup-level-up",
                    ("branch", Loc.GetString(PersistentCraftingHelper.GetBranchLocKey(levelUp.Branch))),
                    ("level", PersistentCraftingHelper.FormatLevel(levelUp.NewLevel, levelUp.NewSubLevel)),
                    ("points", levelUp.LevelsGained)),
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
                ApplyNodeProgress(profile.BranchProgress, saveData.Nodes);
            }

            EnsureAutoTierNodesUnlocked(profile);
            EnsureNodeProfiles(profile);
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
                var branchProfile = GetOrCreateBranchProfile(profile, node.Branch);
                if (branchProfile.Level < PersistentCraftingHelper.GetNodeRequiredBranchLevel(node))
                    continue;

                if (node.NodeType == PersistentCraftNodeType.MainTier)
                {
                    if (profile.UnlockedNodes.Add(node.ID))
                        changed = true;

                    continue;
                }

                if (node.Cost > 0)
                    continue;

                if (node.Prerequisites.Any(prerequisite => !profile.UnlockedNodes.Contains(prerequisite)))
                    continue;

                if (profile.UnlockedNodes.Add(node.ID))
                {
                    EnsureNodeProfile(profile, node);
                    changed = true;
                }
            }
        }
    }

    private async Task SaveProfileAsync(EntityUid uid, PersistentCraftProfileComponent profile)
    {
        try
        {
            var totalAvailablePoints = profile.BranchProgress.Values.Sum(branch => Math.Max(0, branch.AvailablePoints));
            var totalSpentPoints = profile.BranchProgress.Values.Sum(branch => Math.Max(0, branch.SpentPoints));

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
                            AvailablePoints = Math.Max(0, pair.Value.AvailablePoints),
                            SpentPoints = Math.Max(0, pair.Value.SpentPoints),
                            Level = Math.Max(PersistentCraftingHelper.InitialLevel, pair.Value.Level),
                            SubLevel = PersistentCraftingHelper.MainTierSubLevel,
                            Experience = Math.Max(0, pair.Value.Experience),
                        })
                        .ToList(),
                    Nodes = profile.BranchProgress
                        .OrderBy(pair => pair.Key)
                        .SelectMany(pair => pair.Value.NodeProgress
                            .OrderBy(node => node.Key)
                            .Select(node => new PersistentCraftNodeSaveData
                            {
                                NodeId = node.Key,
                                MasteryLevel = Math.Max(PersistentCraftingHelper.InitialNodeMasteryLevel, node.Value.MasteryLevel),
                                Experience = Math.Max(0, node.Value.Experience),
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
            existing.SubLevel = PersistentCraftingHelper.MainTierSubLevel;
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
                    SubLevel = PersistentCraftingHelper.MainTierSubLevel,
                })
                .ToList(),
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
                SubLevel = PersistentCraftingHelper.MainTierSubLevel,
                Experience = Math.Max(0, branch.Experience),
            };
        }

        return result;
    }

    private static void ApplyNodeProgress(
        Dictionary<PersistentCraftBranch, PersistentCraftBranchProfile> branches,
        IEnumerable<PersistentCraftNodeSaveData> nodes)
    {
        foreach (var node in nodes)
        {
            if (string.IsNullOrWhiteSpace(node.NodeId) ||
                !TryGetBranchFromNodeId(node.NodeId, out var branch))
            {
                continue;
            }

            var branchProfile = GetOrCreateBranchProfile(branches, branch);
            branchProfile.NodeProgress[node.NodeId] = new PersistentCraftNodeProfile
            {
                MasteryLevel = Math.Clamp(node.MasteryLevel, PersistentCraftingHelper.InitialNodeMasteryLevel, PersistentCraftingHelper.MaxNodeMasteryLevel),
                Experience = Math.Max(0, node.Experience),
            };
        }
    }

    private static List<PersistentCraftBranchState> BuildBranchStates(
        Dictionary<PersistentCraftBranch, PersistentCraftBranchProfile> branches)
    {
        var result = new List<PersistentCraftBranchState>();

        foreach (var branch in PersistentCraftingHelper.EnumerateBranches())
        {
            var profile = GetOrCreateBranchProfile(branches, branch);
            result.Add(new PersistentCraftBranchState(
                branch,
                profile.AvailablePoints,
                profile.SpentPoints,
                profile.Level,
                profile.SubLevel,
                profile.Experience,
                PersistentCraftingHelper.GetExperienceForNextLevel(profile.Level)));
        }

        return result;
    }

    private List<PersistentCraftNodeState> BuildNodeStates(
        Dictionary<PersistentCraftBranch, PersistentCraftBranchProfile> branches)
    {
        var result = new List<PersistentCraftNodeState>();

        foreach (var branch in PersistentCraftingHelper.EnumerateBranches())
        {
            var branchProfile = GetOrCreateBranchProfile(branches, branch);
            foreach (var (nodeId, nodeProfile) in branchProfile.NodeProgress.OrderBy(pair => pair.Key))
            {
                if (!_proto.TryIndex<PersistentCraftNodePrototype>(nodeId, out var node) ||
                    PersistentCraftingHelper.IsMainTierNode(node))
                {
                    continue;
                }

                var masteryLevel = Math.Clamp(
                    nodeProfile.MasteryLevel,
                    PersistentCraftingHelper.InitialNodeMasteryLevel,
                    PersistentCraftingHelper.MaxNodeMasteryLevel);

                result.Add(new PersistentCraftNodeState(
                    nodeId,
                    masteryLevel,
                    Math.Max(0, nodeProfile.Experience),
                    PersistentCraftingHelper.GetNodeExperienceForNextLevel(node, masteryLevel)));
            }
        }

        return result;
    }

    private bool MeetsRecipeRequirement(EntityUid user, PersistentCraftRecipePrototype recipe)
    {
        var requiredNode = string.IsNullOrWhiteSpace(recipe.RequiredNode)
            ? PersistentCraftingHelper.BuildNodeId(recipe.Branch, recipe.Tier, recipe.SubTier)
            : recipe.RequiredNode;

        if (string.IsNullOrWhiteSpace(requiredNode) ||
            !TryComp(user, out PersistentCraftProfileComponent? profile))
        {
            return false;
        }

        return HasNodeUnlockedOrAutoAvailable(profile, requiredNode);
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

        if (node.Cost > 0)
            return false;

        var branchProfile = GetOrCreateBranchProfile(profile, node.Branch);
        if (branchProfile.Level < PersistentCraftingHelper.GetNodeRequiredBranchLevel(node))
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

    private PersistentCraftLevelUpResult GrantCraftExperience(EntityUid user, PersistentCraftRecipePrototype recipe)
    {
        if (!TryComp(user, out PersistentCraftProfileComponent? profile))
        {
            return new PersistentCraftLevelUpResult(
                recipe.Branch,
                0,
                PersistentCraftingHelper.InitialLevel,
                PersistentCraftingHelper.MainTierSubLevel);
        }

        var branchProfile = GetOrCreateBranchProfile(profile, recipe.Branch);
        branchProfile.Level = Math.Max(PersistentCraftingHelper.InitialLevel, branchProfile.Level);
        branchProfile.SubLevel = PersistentCraftingHelper.MainTierSubLevel;
        branchProfile.Experience = Math.Max(0, branchProfile.Experience);

        branchProfile.Experience += PersistentCraftingHelper.GetExperienceReward(recipe);

        var levelsGained = 0;
        while (branchProfile.Experience >= PersistentCraftingHelper.GetExperienceForNextLevel(branchProfile.Level))
        {
            branchProfile.Experience -= PersistentCraftingHelper.GetExperienceForNextLevel(branchProfile.Level);
            branchProfile.Level += 1;
            branchProfile.SubLevel = PersistentCraftingHelper.MainTierSubLevel;
            branchProfile.AvailablePoints += 1;
            levelsGained += 1;
        }

        EnsureAutoTierNodesUnlocked(profile);
        EnsureNodeProfiles(profile);
        GrantNodeExperience(profile, recipe);

        return new PersistentCraftLevelUpResult(
            recipe.Branch,
            levelsGained,
            branchProfile.Level,
            branchProfile.SubLevel);
    }

    private float GetEffectiveCraftTime(EntityUid user, PersistentCraftRecipePrototype recipe)
    {
        var reduction = GetUnlockedNodeEffects(user, recipe)
            .Where(entry => entry.Node.CraftTimeReductionPercent > 0)
            .Sum(entry => entry.Node.CraftTimeReductionPercent * GetNodeEffectMultiplier(entry.Profile));

        var multiplier = Math.Max(0.25f, 1f - reduction / 100f);
        return MathF.Max(0.25f, recipe.CraftTime * multiplier);
    }

    private int GetEffectiveIngredientAmount(
        EntityUid user,
        PersistentCraftRecipePrototype recipe,
        PersistentCraftIngredient ingredient)
    {
        var reduction = GetUnlockedNodeEffects(user, recipe)
            .Where(entry => entry.Node.MaterialCostReductionPercent > 0)
            .Sum(entry => entry.Node.MaterialCostReductionPercent * GetNodeEffectMultiplier(entry.Profile));

        var multiplier = Math.Max(0.25f, 1f - reduction / 100f);
        return Math.Max(1, (int) MathF.Ceiling(ingredient.Amount * multiplier));
    }

    private IEnumerable<(PersistentCraftNodePrototype Node, PersistentCraftNodeProfile Profile)> GetUnlockedNodeEffects(EntityUid user, PersistentCraftRecipePrototype recipe)
    {
        if (!TryComp(user, out PersistentCraftProfileComponent? profile))
            yield break;

        foreach (var nodeId in profile.UnlockedNodes)
        {
            if (!_proto.TryIndex<PersistentCraftNodePrototype>(nodeId, out var node))
                continue;

            if (node.Branch != recipe.Branch)
                continue;

            if (PersistentCraftingHelper.GetAffectedTier(node) != recipe.Tier)
                continue;

            yield return (node, GetOrCreateNodeProfile(profile, node));
        }
    }

    private void GrantNodeExperience(PersistentCraftProfileComponent profile, PersistentCraftRecipePrototype recipe)
    {
        var nodeExperience = PersistentCraftingHelper.GetNodeExperienceReward(recipe);
        if (nodeExperience <= 0)
            return;

        foreach (var node in GetNodeMasteryTargets(profile, recipe))
        {
            var branchProfile = GetOrCreateBranchProfile(profile, node.Branch);
            var nodeProfile = GetOrCreateNodeProfile(profile, node);
            if (nodeProfile.MasteryLevel >= PersistentCraftingHelper.MaxNodeMasteryLevel)
                continue;

            nodeProfile.Experience += nodeExperience;

            while (nodeProfile.MasteryLevel < PersistentCraftingHelper.MaxNodeMasteryLevel)
            {
                var nextExperience = PersistentCraftingHelper.GetNodeExperienceForNextLevel(node, nodeProfile.MasteryLevel);
                if (nextExperience <= 0 || nodeProfile.Experience < nextExperience)
                    break;

                nodeProfile.Experience -= nextExperience;
                nodeProfile.MasteryLevel += 1;
            }

            if (nodeProfile.MasteryLevel >= PersistentCraftingHelper.MaxNodeMasteryLevel)
            {
                nodeProfile.MasteryLevel = PersistentCraftingHelper.MaxNodeMasteryLevel;
                nodeProfile.Experience = 0;
                branchProfile.AvailablePoints += 1;
            }
        }
    }

    private IEnumerable<PersistentCraftNodePrototype> GetNodeMasteryTargets(
        PersistentCraftProfileComponent profile,
        PersistentCraftRecipePrototype recipe)
    {
        var requiredNodeId = string.IsNullOrWhiteSpace(recipe.RequiredNode)
            ? PersistentCraftingHelper.BuildNodeId(recipe.Branch, recipe.Tier, recipe.SubTier)
            : recipe.RequiredNode;

        foreach (var nodeId in profile.UnlockedNodes)
        {
            if (!_proto.TryIndex<PersistentCraftNodePrototype>(nodeId, out var node) ||
                PersistentCraftingHelper.IsMainTierNode(node) ||
                node.Branch != recipe.Branch ||
                node.Tier != recipe.Tier)
            {
                continue;
            }

            var isRecipeNode = node.NodeType == PersistentCraftNodeType.RecipeUnlock && node.ID == requiredNodeId;
            var isPassiveTierNode = node.NodeType != PersistentCraftNodeType.RecipeUnlock;

            if (isRecipeNode || isPassiveTierNode)
                yield return node;
        }
    }

    private static float GetNodeEffectMultiplier(PersistentCraftNodeProfile profile)
    {
        var max = PersistentCraftingHelper.MaxNodeMasteryLevel;
        var level = Math.Clamp(profile.MasteryLevel, PersistentCraftingHelper.InitialNodeMasteryLevel, max);
        if (max <= PersistentCraftingHelper.InitialNodeMasteryLevel)
            return 1f;

        var normalized = (level - PersistentCraftingHelper.InitialNodeMasteryLevel) /
                         (float) (max - PersistentCraftingHelper.InitialNodeMasteryLevel);
        return 0.5f + normalized * 0.5f;
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

    private void EnsureNodeProfiles(PersistentCraftProfileComponent profile)
    {
        foreach (var nodeId in profile.UnlockedNodes)
        {
            if (!_proto.TryIndex<PersistentCraftNodePrototype>(nodeId, out var node) ||
                PersistentCraftingHelper.IsMainTierNode(node))
            {
                continue;
            }

            EnsureNodeProfile(profile, node);
        }
    }

    private PersistentCraftNodeProfile EnsureNodeProfile(PersistentCraftProfileComponent profile, PersistentCraftNodePrototype node)
    {
        return GetOrCreateNodeProfile(profile, node);
    }

    private static PersistentCraftNodeProfile GetOrCreateNodeProfile(
        PersistentCraftProfileComponent profile,
        PersistentCraftNodePrototype node)
    {
        var branchProfile = GetOrCreateBranchProfile(profile, node.Branch);
        return GetOrCreateNodeProfile(branchProfile, node.ID);
    }

    private static PersistentCraftNodeProfile GetOrCreateNodeProfile(
        PersistentCraftBranchProfile branchProfile,
        string nodeId)
    {
        if (!branchProfile.NodeProgress.TryGetValue(nodeId, out var profile))
        {
            profile = new PersistentCraftNodeProfile();
            branchProfile.NodeProgress[nodeId] = profile;
        }

        profile.MasteryLevel = Math.Clamp(
            profile.MasteryLevel,
            PersistentCraftingHelper.InitialNodeMasteryLevel,
            PersistentCraftingHelper.MaxNodeMasteryLevel);
        profile.Experience = Math.Max(0, profile.Experience);
        return profile;
    }

    private sealed class PersistentCraftSaveData
    {
        public List<PersistentCraftBranchSaveData> Branches { get; set; } = new();
        public List<PersistentCraftNodeSaveData> Nodes { get; set; } = new();
        public List<string> UnlockedNodes { get; set; } = new();
    }

    private sealed class PersistentCraftBranchSaveData
    {
        public PersistentCraftBranch Branch { get; set; }
        public int AvailablePoints { get; set; }
        public int SpentPoints { get; set; }
        public int Level { get; set; } = PersistentCraftingHelper.InitialLevel;
        public int SubLevel { get; set; } = PersistentCraftingHelper.MainTierSubLevel;
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
        public int MasteryLevel { get; set; } = PersistentCraftingHelper.InitialNodeMasteryLevel;
        public int Experience { get; set; }
    }

    private readonly record struct PersistentCraftLevelUpResult(
        PersistentCraftBranch Branch,
        int LevelsGained,
        int NewLevel,
        int NewSubLevel);
}
