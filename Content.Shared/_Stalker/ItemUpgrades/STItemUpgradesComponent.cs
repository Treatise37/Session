using System.Collections.Generic;
using Content.Shared._Stalker.ItemUpgrades.Prototypes;
using Robust.Shared.GameObjects;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.Manager.Attributes;

namespace Content.Shared._Stalker.ItemUpgrades;

[RegisterComponent]
public sealed partial class STItemUpgradesComponent : Component
{
    [DataField]
    public ProtoId<STUpgradeTreePrototype>? Tree;

    [DataField]
    public List<STItemUpgradeEntry> Upgrades = new();

    [DataField]
    public HashSet<string> InstalledUpgrades = new();

    [DataField]
    public string? SelectedBranch;

    public float? BaseWeight;
}

[DataDefinition]
public sealed partial class STItemUpgradeEntry
{
    [DataField(required: true)]
    public string Id = string.Empty;

    [DataField(required: true)]
    public string Name = string.Empty;

    [DataField]
    public string? BranchId;

    [DataField]
    public string? BranchName;

    [DataField]
    public List<string> RequiredUpgrades = new();

    [DataField]
    public Dictionary<string, int> RequiredMaterials = new();

    [DataField]
    public List<string> RequiredTools = new();

    [DataField]
    public float WeightMultiplier = 1f;

    [DataField]
    public STItemGunUpgradeModifier? Gun;

    [DataField]
    public STItemArmorUpgradeModifier? Armor;
}

[DataDefinition]
public sealed partial class STItemGunUpgradeModifier
{
    [DataField]
    public float FireRateMultiplier = 1f;

    [DataField]
    public float MinAngleMultiplier = 1f;

    [DataField]
    public float MaxAngleMultiplier = 1f;

    [DataField]
    public float AngleIncreaseMultiplier = 1f;

    [DataField]
    public float AngleDecayMultiplier = 1f;
}

[DataDefinition]
public sealed partial class STItemArmorUpgradeModifier
{
    [DataField]
    public List<STArmorCoefficientModifier> Coefficients = new();

    [DataField]
    public List<STArmorFlatModifier> FlatReductions = new();
}

[DataDefinition]
public sealed partial class STArmorCoefficientModifier
{
    [DataField(required: true)]
    public string DamageType = string.Empty;

    [DataField]
    public float Multiplier = 1f;
}

[DataDefinition]
public sealed partial class STArmorFlatModifier
{
    [DataField(required: true)]
    public string DamageType = string.Empty;

    [DataField]
    public float Add = 0f;
}