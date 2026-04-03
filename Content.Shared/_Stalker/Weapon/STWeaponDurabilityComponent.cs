using Robust.Shared.GameObjects;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization.Manager.Attributes;

namespace Content.Shared._Stalker.Weapon;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class STWeaponDurabilityComponent : Component
{
    [DataField("maxDurability"), AutoNetworkedField]
    public float MaxDurability = 100f;

    [DataField("currentDurability"), AutoNetworkedField]
    public float CurrentDurability = 100f;

    [DataField("durabilityLossPerShot"), AutoNetworkedField]
    public float DurabilityLossPerShot = 0.05f;

    [DataField("canJam"), AutoNetworkedField]
    public bool CanJam = true;

    public float Ratio => MaxDurability <= 0f ? 1f : CurrentDurability / MaxDurability;
}