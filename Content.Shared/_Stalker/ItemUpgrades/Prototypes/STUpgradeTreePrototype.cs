using System.Collections.Generic;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.Manager.Attributes;

namespace Content.Shared._Stalker.ItemUpgrades.Prototypes;

[Prototype("stUpgradeTree")]
public sealed partial class STUpgradeTreePrototype : IPrototype
{
    [IdDataField]
    public string ID { get; } = string.Empty;

    [DataField(required: true)]
    public List<STItemUpgradeEntry> Upgrades = new();
}