using Content.Shared.Roles;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;

namespace Content.Client.Roles;

public sealed class RoleRulesSystem : EntitySystem
{
    private RoleRulesWindow? _window;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeNetworkEvent<ShowJobRulesWindowEvent>(OnShowJobRulesWindowEvent);
    }

    private void OnShowJobRulesWindowEvent(ShowJobRulesWindowEvent ev, EntitySessionEventArgs args)
    {
        _window?.Close();

        var jobName = Loc.GetString(ev.JobNameLocId);
        var rules = Loc.GetString(ev.RulesLocId);

        _window = new RoleRulesWindow(jobName, rules);
        _window.OnClose += () => _window = null;
        _window.OpenCentered();
    }
}
