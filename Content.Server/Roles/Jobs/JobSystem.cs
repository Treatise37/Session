using System.Globalization;
using Content.Server.Chat.Managers;
// stalker-changes
using Content.Shared.Actions;
// stalker-changes-end
using Content.Shared.Mind;
using Content.Server.Mind;
using Content.Server.Roles;
using Content.Shared.Roles;
using Content.Shared.Roles.Jobs;
using Robust.Shared.Player;

namespace Content.Server.Roles.Jobs;

/// <summary>
///     Handles the job data on mind entities.
/// </summary>
public sealed class JobSystem : SharedJobSystem
{
    [Dependency] private readonly IChatManager _chat = default!;
    [Dependency] private readonly ISharedPlayerManager _player = default!;
    [Dependency] private readonly MindSystem _minds = default!;
    [Dependency] private readonly RoleSystem _roles = default!;
    [Dependency] private readonly SharedActionsSystem _actions = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<RoleAddedEvent>(OnRoleAddedEvent);
        SubscribeLocalEvent<RoleRemovedEvent>(OnRoleRemovedEvent);
        SubscribeLocalEvent<ShowJobRulesActionEvent>(OnShowJobRulesAction);
    }

    private void OnRoleAddedEvent(RoleAddedEvent args)
    {
        MindOnDoGreeting(args.MindId, args.Mind, args);

        if (args.Mind.OwnedEntity is { } ownedEntity && MindTryGetJob(args.MindId, out var prototype))
        {
            ShowJobRules(ownedEntity, args.Silent, prototype);
            AddJobRulesAction(ownedEntity, prototype);
        }

        if (args.RoleTypeUpdate)
            _roles.RoleUpdateMessage(args.Mind);
    }

    private void OnRoleRemovedEvent(RoleRemovedEvent args)
    {
        if (args.Mind.OwnedEntity is { } ownedEntity)
            RemoveJobRulesAction(ownedEntity);

        if (args.RoleTypeUpdate)
            _roles.RoleUpdateMessage(args.Mind);
    }

    private void MindOnDoGreeting(EntityUid mindId, MindComponent component, RoleAddedEvent args)
    {
        if (args.Silent)
            return;

        if (!_player.TryGetSessionById(component.UserId, out var session))
            return;

        if (!MindTryGetJob(mindId, out var prototype))
            return;

        _chat.DispatchServerMessage(session, Loc.GetString("job-greet-introduce-job-name",
            ("jobName", CultureInfo.CurrentCulture.TextInfo.ToTitleCase(prototype.LocalizedName))));

        if (prototype.RequireAdminNotify)
            _chat.DispatchServerMessage(session, Loc.GetString("job-greet-important-disconnect-admin-notify"));

        _chat.DispatchServerMessage(session, Loc.GetString("job-greet-supervisors-warning", ("jobName", prototype.LocalizedName), ("supervisors", Loc.GetString(prototype.Supervisors))));
    }

    // stalker-changes
    private void ShowJobRules(EntityUid user, bool silent, JobPrototype prototype)
    {
        // Don't spam rules if they are hidden or not configured.
        if (silent || string.IsNullOrEmpty(prototype.Rules))
            return;

        if (!_player.TryGetSessionByEntity(user, out var session))
            return;

        RaiseNetworkEvent(new ShowJobRulesWindowEvent(GetNetEntity(user), prototype.Name, prototype.Rules), session);
    }

    private void OnShowJobRulesAction(ShowJobRulesActionEvent args)
    {
        if (!args.Performer.IsValid())
            return;

        if (!_player.TryGetSessionByEntity(args.Performer, out var session))
            return;

        if (!_minds.TryGetMind(session.UserId, out var mindId, out _))
            return;

        if (!MindTryGetJob(mindId, out var prototype))
            return;

        ShowJobRules(args.Performer, false, prototype);
    }

    private void AddJobRulesAction(EntityUid user, JobPrototype prototype)
    {
        if (string.IsNullOrEmpty(prototype.Rules))
            return;

        // Ensure old action is removed when switching roles.
        RemoveJobRulesAction(user);

        var comp = EnsureComp<JobRulesActionComponent>(user);
        _actions.AddAction(user, ref comp.ActionEntity, "ActionShowJobRules");
    }

    private void RemoveJobRulesAction(EntityUid user)
    {
        if (!TryComp<JobRulesActionComponent>(user, out var comp) || comp.ActionEntity == null)
            return;

        _actions.RemoveAction(comp.ActionEntity);
        RemComp<JobRulesActionComponent>(user);
    }
    // stalker-changes-end

    public void MindAddJob(EntityUid mindId, string jobPrototypeId)
    {
        if (MindHasJobWithId(mindId, jobPrototypeId))
            return;

        _roles.MindAddJobRole(mindId, null, false, jobPrototypeId);
    }
}
