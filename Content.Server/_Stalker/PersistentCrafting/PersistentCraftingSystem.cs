using System.Collections.Generic;
using System.Linq;
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

    private PersistentCraftBranchRegistry _branchRegistry = default!;
    private PersistentCraftProfileService _profileService = default!;
    private PersistentCraftProfileRepository _profileRepository = default!;
    private PersistentCraftUnlockService _unlockService = default!;
    private PersistentCraftCraftExecutionService _craftExecutionService = default!;
    private List<PersistentCraftNodePrototype> _nodeCache = new();

    public override void Initialize()
    {
        base.Initialize();

        _branchRegistry = PersistentCraftBranchRegistry.Create(_proto);
        _nodeCache = _proto.EnumeratePrototypes<PersistentCraftNodePrototype>().ToList();
        _profileService = new PersistentCraftProfileService(_proto, _branchRegistry, _nodeCache);
        _profileRepository = new PersistentCraftProfileRepository(_db, _branchRegistry);
        _unlockService = new PersistentCraftUnlockService(_profileService);
        _craftExecutionService = new PersistentCraftCraftExecutionService(
            EntityManager,
            _tag,
            _stacks,
            _hands,
            _profileService);
        ValidateRecipeIngredientDefinitions();

        SubscribeLocalEvent<PlayerSpawnCompleteEvent>(OnPlayerSpawnComplete);
        SubscribeLocalEvent<PersistentCraftAccessComponent, ComponentStartup>(OnAccessStartup);
        SubscribeLocalEvent<PersistentCraftAccessComponent, ComponentShutdown>(OnAccessShutdown);
        SubscribeLocalEvent<PersistentCraftAccessComponent, OpenPersistentCraftMenuActionEvent>(OnOpenCraftMenu);
        SubscribeLocalEvent<PersistentCraftAccessComponent, PersistentCraftDoAfterEvent>(OnCraftDoAfter);
        SubscribeNetworkEvent<RequestPersistentCraftStateEvent>(OnRequestState);
        SubscribeNetworkEvent<RequestPersistentCraftRecipeEvent>(OnRequestCraftRecipe);
        SubscribeNetworkEvent<RequestPersistentCraftUnlockEvent>(OnRequestUnlock);
    }

    private void ValidateRecipeIngredientDefinitions()
    {
        foreach (var recipe in _proto.EnumeratePrototypes<PersistentCraftRecipePrototype>())
        {
            for (var index = 0; index < recipe.Ingredients.Count; index++)
            {
                var ingredient = recipe.Ingredients[index];
                var selectorKind = ingredient.GetSelectorKind();
                var selectorValue = ingredient.GetSelectorValue();

                if (selectorKind == PersistentCraftIngredientSelectorKind.None)
                {
                    Log.Warning($"[PersistentCraft] Recipe '{recipe.ID}' ingredient #{index} has no selector (proto/stackType/tag).");
                }
                else if (selectorKind == PersistentCraftIngredientSelectorKind.InvalidMultiple)
                {
                    Log.Warning($"[PersistentCraft] Recipe '{recipe.ID}' ingredient #{index} has multiple selectors set. proto='{ingredient.Proto ?? string.Empty}', stackType='{ingredient.StackType ?? string.Empty}', tag='{ingredient.Tag ?? string.Empty}'.");
                }

                if (ingredient.Amount <= 0)
                {
                    Log.Warning($"[PersistentCraft] Recipe '{recipe.ID}' ingredient #{index} has non-positive amount '{ingredient.Amount}'. Selector={selectorKind} Value='{selectorValue}'.");
                }
            }
        }
    }

    public PersistentCraftState GetState(EntityUid uid)
    {
        if (!TryComp(uid, out PersistentCraftProfileComponent? profile))
        {
            var defaultProfile = new PersistentCraftProfileComponent
            {
                BranchProgress = _profileService.CreateDefaultBranchProfiles(),
            };

            return new PersistentCraftState(
                false,
                _profileService.BuildBranchStates(defaultProfile),
                new List<string>());
        }

        return new PersistentCraftState(
            profile.Loaded,
            _profileService.BuildBranchStates(profile),
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

        profile.BranchProgress = _profileService.CreateDefaultBranchProfiles();
        profile.UnlockedNodes.Clear();
        _profileService.EnsureAutoTierNodesUnlocked(profile);
        _profileService.NormalizeBranchPoints(profile);
        profile.Loaded = true;
        profile.PersistenceDisabled = false;

        await SaveProfileAsync(uid, profile);
        SendStateToAttachedActor(uid);
        return true;
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
        profile.LoadGeneration++;
        profile.UserId = args.Player.UserId.UserId;
        profile.CharacterName = args.Profile.Name;
        profile.BranchProgress = _profileService.CreateDefaultBranchProfiles();
        profile.UnlockedNodes.Clear();
        _profileService.EnsureAutoTierNodesUnlocked(profile);
        _profileService.NormalizeBranchPoints(profile);
        profile.Loaded = false;
        profile.PersistenceDisabled = false;

        _ = LoadProfileAsync(args.Mob, profile.UserId, profile.CharacterName, profile.LoadGeneration);
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

        if (!_craftExecutionService.MeetsRecipeRequirement(user, recipe))
        {
            PopupUser(user, "persistent-craft-station-popup-skill-locked");
            SendState(args.SenderSession, user);
            return;
        }

        var maxCraftCount = _craftExecutionService.GetMaxCraftCount(user, recipe);
        if (maxCraftCount <= 0)
        {
            PopupUser(user, "persistent-craft-station-popup-missing-items");
            SendState(args.SenderSession, user);
            return;
        }

        var requestedCount = Math.Clamp(ev.Amount, 1, maxCraftCount);
        if (!TryStartCraftDoAfter(user, recipe, requestedCount, requestedCount))
            return;

        var startedText = requestedCount > 1
            ? Loc.GetString(
                "persistent-craft-station-popup-batch-started",
                ("recipe", ResolveRecipeName(recipe)),
                ("count", requestedCount))
            : Loc.GetString("persistent-craft-station-popup-started", ("recipe", ResolveRecipeName(recipe)));

        _popup.PopupEntity(
            startedText,
            user,
            user);
    }

    private void OnRequestUnlock(RequestPersistentCraftUnlockEvent ev, EntitySessionEventArgs args)
    {
        if (args.SenderSession.AttachedEntity is not { Valid: true } user)
            return;

        if (!HasComp<PersistentCraftAccessComponent>(user))
            return;

        if (!TryComp(user, out PersistentCraftProfileComponent? profile))
            return;

        if (!profile.Loaded)
        {
            _popup.PopupEntity(Loc.GetString("persistent-craft-popup-loading"), user, user);
            SendState(args.SenderSession, user);
            return;
        }

        if (!_proto.TryIndex<PersistentCraftNodePrototype>(ev.NodeId, out var node))
            return;

        if (!_unlockService.TryUnlockNode(profile, node, out var failure))
        {
            var failureLoc = failure switch
            {
                PersistentCraftUnlockFailure.AutoUnlockedNode => "persistent-craft-popup-tier-auto",
                PersistentCraftUnlockFailure.AlreadyUnlocked => "persistent-craft-popup-already-unlocked",
                PersistentCraftUnlockFailure.MissingPrerequisites => "persistent-craft-popup-prerequisite",
                PersistentCraftUnlockFailure.NotEnoughPoints => "persistent-craft-popup-not-enough-points",
                _ => null,
            };

            if (!string.IsNullOrWhiteSpace(failureLoc))
                _popup.PopupEntity(Loc.GetString(failureLoc), user, user);

            return;
        }

        _ = SaveProfileAsync(user, profile);

        _popup.PopupEntity(
            Loc.GetString("persistent-craft-popup-unlocked", ("skill", ResolveNodeName(node))),
            user,
            user);

        SendState(args.SenderSession, user);
    }

    private void OnCraftDoAfter(EntityUid uid, PersistentCraftAccessComponent component, PersistentCraftDoAfterEvent args)
    {
        if (args.Handled)
            return;

        if (!_proto.TryIndex<PersistentCraftRecipePrototype>(args.RecipeId, out var recipe))
            return;

        if (!Exists(args.User) || args.User != uid)
            return;

        if (args.Cancelled)
        {
            args.Handled = true;

            var isCancelledBatchCraft = args.RequestedCount > 1;
            if (isCancelledBatchCraft)
            {
                var cancelledCraftedCount = Math.Clamp(args.RequestedCount - args.RemainingCount, 0, args.RequestedCount);
                var cancelledText = cancelledCraftedCount > 0
                    ? Loc.GetString(
                        "persistent-craft-station-popup-batch-cancelled-progress",
                        ("recipe", ResolveRecipeName(recipe)),
                        ("crafted", cancelledCraftedCount),
                        ("requested", args.RequestedCount))
                    : Loc.GetString(
                        "persistent-craft-station-popup-batch-cancelled",
                        ("recipe", ResolveRecipeName(recipe)));

                _popup.PopupEntity(cancelledText, args.User, args.User);
            }

            SendStateToAttachedActor(args.User);
            return;
        }

        args.Handled = true;

        if (!IsLoaded(args.User))
        {
            PopupUser(args.User, "persistent-craft-popup-loading");
            SendStateToAttachedActor(args.User);
            return;
        }

        if (!_craftExecutionService.MeetsRecipeRequirement(args.User, recipe))
        {
            PopupUser(args.User, "persistent-craft-station-popup-skill-locked");
            SendStateToAttachedActor(args.User);
            return;
        }

        if (!_craftExecutionService.TryPlanIngredientConsumption(args.User, recipe, out var plan))
        {
            PopupUser(args.User, "persistent-craft-station-popup-missing-items");
            SendStateToAttachedActor(args.User);
            return;
        }

        _craftExecutionService.ConsumeIngredientPlan(plan);
        _craftExecutionService.SpawnResults(args.User, recipe);
        _craftExecutionService.GrantCraftPoints(args.User, recipe);
        var craftedCount = Math.Clamp(args.RequestedCount - args.RemainingCount + 1, 1, args.RequestedCount);
        var isBatchCraft = args.RequestedCount > 1;
        var isLastStep = args.RemainingCount <= 1;

        if (!isBatchCraft || isLastStep)
        {
            var craftedText = !isBatchCraft
                ? Loc.GetString("persistent-craft-station-popup-crafted", ("recipe", ResolveRecipeName(recipe)))
                : Loc.GetString(
                    "persistent-craft-station-popup-crafted-batch",
                    ("recipe", ResolveRecipeName(recipe)),
                    ("crafted", craftedCount),
                    ("requested", args.RequestedCount));

            _popup.PopupEntity(
                craftedText,
                args.User,
                args.User);
        }

        var pointsReward = PersistentCraftingHelper.GetPointReward(recipe);
        if (pointsReward > 0 && (!isBatchCraft || isLastStep))
        {
            var totalPointsReward = isBatchCraft ? pointsReward * craftedCount : pointsReward;
            _popup.PopupEntity(
                Loc.GetString("persistent-craft-popup-points-gained", ("points", totalPointsReward)),
                args.User,
                args.User);
        }

        _ = SaveProfileAsync(args.User, Comp<PersistentCraftProfileComponent>(args.User));

        if (!isBatchCraft || isLastStep)
            SendStateToAttachedActor(args.User);

        if (args.RemainingCount > 1 &&
            !TryStartCraftDoAfter(args.User, recipe, args.RemainingCount - 1, args.RequestedCount))
        {
            _popup.PopupEntity(
                Loc.GetString(
                    "persistent-craft-station-popup-batch-stopped",
                    ("recipe", ResolveRecipeName(recipe)),
                    ("crafted", craftedCount),
                    ("requested", args.RequestedCount)),
                args.User,
                args.User);
            SendStateToAttachedActor(args.User);
        }
    }

    private bool TryStartCraftDoAfter(EntityUid user, PersistentCraftRecipePrototype recipe, int remainingCount, int requestedCount)
    {
        var craftTime = _craftExecutionService.GetEffectiveCraftTime(user, recipe);
        var doAfter = new DoAfterArgs(
            EntityManager,
            user,
            craftTime,
            new PersistentCraftDoAfterEvent(recipe.ID, remainingCount, requestedCount),
            user,
            target: user,
            used: user)
        {
            BreakOnMove = true,
            BreakOnDamage = true,
            NeedHand = false,
            RequireCanInteract = true,
            BlockDuplicate = true,
        };

        return _doAfter.TryStartDoAfter(doAfter);
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

    private async Task LoadProfileAsync(EntityUid uid, Guid userId, string characterName, int loadGeneration)
    {
        try
        {
            var loadResult = await _profileRepository.LoadProfileAsync(userId, characterName);

            if (Deleted(uid) ||
                !TryComp(uid, out PersistentCraftProfileComponent? profile) ||
                profile.LoadGeneration != loadGeneration)
                return;

            profile.BranchProgress = _profileService.CreateDefaultBranchProfiles();
            profile.UnlockedNodes.Clear();
            profile.PersistenceDisabled = false;
            var shouldResaveProfile = false;

            if (!loadResult.Success)
            {
                profile.PersistenceDisabled = true;
                Log.Error(loadResult.ErrorMessage ?? $"[PersistentCraft] Persistent craft profile for '{characterName}' ({userId}) is unreadable. Using a temporary in-memory profile without persistence.");

                if (TryComp(uid, out ActorComponent? popupActor))
                {
                    _popup.PopupEntity(
                        Loc.GetString("persistent-craft-load-failed"),
                        uid,
                        popupActor.PlayerSession,
                        PopupType.MediumCaution);
                }
            }
            else if (loadResult.HasSavedProfile && loadResult.SaveData != null)
            {
                var saveData = loadResult.SaveData;
                profile.BranchProgress = _profileService.BuildBranchProfiles(saveData.Branches);
                profile.UnlockedNodes = _profileService.SanitizeUnlockedNodes(saveData.UnlockedNodes, characterName);
                shouldResaveProfile = loadResult.DataChanged || profile.UnlockedNodes.Count != saveData.UnlockedNodes.Count;
            }

            _profileService.EnsureAutoTierNodesUnlocked(profile);
            _profileService.NormalizeBranchPoints(profile);
            profile.Loaded = true;

            if (shouldResaveProfile && !profile.PersistenceDisabled && profile.LoadGeneration == loadGeneration)
                await SaveProfileAsync(uid, profile);

            if (TryComp(uid, out ActorComponent? actor))
                SendState(actor.PlayerSession, uid);
        }
        catch (Exception ex)
        {
            Log.Error($"Failed to load persistent craft profile for {characterName}: {ex}");

            if (Deleted(uid) ||
                !TryComp(uid, out PersistentCraftProfileComponent? profile) ||
                profile.LoadGeneration != loadGeneration)
                return;

            profile.BranchProgress = _profileService.CreateDefaultBranchProfiles();
            profile.UnlockedNodes.Clear();
            profile.PersistenceDisabled = true;
            _profileService.EnsureAutoTierNodesUnlocked(profile);
            _profileService.NormalizeBranchPoints(profile);
            profile.Loaded = true;

            if (TryComp(uid, out ActorComponent? actor))
            {
                _popup.PopupEntity(
                    Loc.GetString("persistent-craft-load-failed"),
                    uid,
                    actor.PlayerSession,
                    PopupType.MediumCaution);
                SendState(actor.PlayerSession, uid);
            }
        }
    }

    private async Task SaveProfileAsync(EntityUid uid, PersistentCraftProfileComponent profile)
    {
        try
        {
            if (profile.PersistenceDisabled)
                return;

            profile.UnlockedNodes = _profileService.SanitizeUnlockedNodes(profile.UnlockedNodes, profile.CharacterName);
            _profileService.EnsureAutoTierNodesUnlocked(profile);
            _profileService.NormalizeBranchPoints(profile);
            await _profileRepository.SaveProfileAsync(profile);
        }
        catch (Exception ex)
        {
            Log.Error($"Failed to save persistent craft profile for {profile.CharacterName}: {ex}");
            if (!Deleted(uid) && TryComp(uid, out ActorComponent? actor))
                _popup.PopupEntity(Loc.GetString("persistent-craft-save-failed"), uid, actor.PlayerSession, PopupType.MediumCaution);
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

    private string ResolveNodeName(PersistentCraftNodePrototype node)
    {
        if (!string.IsNullOrWhiteSpace(node.Name))
        {
            try
            {
                return Loc.GetString(node.Name);
            }
            catch
            {
                return node.Name;
            }
        }

        if (!string.IsNullOrWhiteSpace(node.DisplayProto) &&
            _proto.TryIndex<EntityPrototype>(node.DisplayProto, out var prototype))
        {
            return prototype.Name;
        }

        return node.ID;
    }
}
