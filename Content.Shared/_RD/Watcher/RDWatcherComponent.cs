using System.Numerics;
using Robust.Shared.GameStates;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;

namespace Content.Shared._RD.Watcher;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(fieldDeltas: true)]
public sealed partial class RDWatcherComponent : Component
{
    [ViewVariables(VVAccess.ReadWrite), AutoNetworkedField]
    public HashSet<EntityUid> Entities = new();

    #region Storage

    [ViewVariables(VVAccess.ReadWrite), AutoNetworkedField]
    public List<EntProtoId> VirtualStorage = new();

    #endregion

    #region Meta

    [ViewVariables(VVAccess.ReadWrite), AutoNetworkedField]
    public string GroupId = string.Empty;

    #endregion

    #region Transform

    [ViewVariables(VVAccess.ReadWrite), AutoNetworkedField]
    public Vector2 Position;

    [ViewVariables(VVAccess.ReadWrite), AutoNetworkedField]
    public MapId MapId;

    #endregion
}
