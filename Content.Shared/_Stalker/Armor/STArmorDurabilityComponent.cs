using System.Collections.Generic;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Shared._Stalker.Armor;

[RegisterComponent]
public sealed partial class STArmorDurabilityComponent : Component
{
    [DataField("maxDurability")]
    public float MaxDurability = 100f;

    [DataField("currentDurability")]
    public float CurrentDurability = 100f;

    [DataField("durabilityLossPerDamage")]
    public float DurabilityLossPerDamage = 0.08f;

    [DataField("minProtectionFactor")]
    public float MinProtectionFactor = 0.4f;

    [DataField("affectedDamageTypes")]
    public List<string> AffectedDamageTypes = new()
    {
        "Blunt",
        "Piercing",
        "Heat",
    };
}