using Robust.Shared.GameStates;

namespace Content.Shared._RD.Watcher;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(fieldDeltas: true)]
public sealed partial class RDWatcherTargetComponent : Component
{
    [DataField, AutoNetworkedField]
    public string GroupId = string.Empty;

    [ViewVariables, AutoNetworkedField]
    public EntityUid? Watcher;
}
