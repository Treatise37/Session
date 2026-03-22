using Robust.Shared.GameObjects;
using Robust.Shared.Serialization.Manager.Attributes;

namespace Content.Shared._Stalker.Weapon;

[RegisterComponent]
public sealed partial class STWeaponDurabilityComponent : Component
{
    [DataField]
    public float MaxDurability = 100f;

    [DataField]
    public float CurrentDurability = 100f;

    [DataField]
    public float DurabilityLossPerShot = 0.05f;

    [DataField]
    public bool CanJam = true;

    public float Ratio => MaxDurability <= 0f ? 1f : CurrentDurability / MaxDurability;
}