using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared._Session.Flag;

[RegisterComponent, NetworkedComponent]
public sealed partial class WarZoneFlagComponent : Component;

[Serializable, NetSerializable]
public enum WarZoneFlagVisuals : byte
{
    Level
}
