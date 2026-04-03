using System.Collections.Generic;
using Robust.Shared.GameObjects;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization.Manager.Attributes;

namespace Content.Shared._Stalker.Armor;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class STArmorDurabilityComponent : Component
{
    [DataField("maxDurability"), AutoNetworkedField]
    public float MaxDurability = 100f;

    [DataField("currentDurability"), AutoNetworkedField]
    public float CurrentDurability = 100f;

    [DataField("durabilityLossPerDamage"), AutoNetworkedField]
    public float DurabilityLossPerDamage = 0.08f;

    [DataField("minProtectionFactor"), AutoNetworkedField]
    public float MinProtectionFactor = 0.4f;

    [DataField("affectedDamageTypes"), AutoNetworkedField]
    public List<string> AffectedDamageTypes = new()
    {
        "Blunt",
        "Piercing",
        "Heat",
    };
}