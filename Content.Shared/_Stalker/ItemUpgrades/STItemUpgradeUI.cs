using System;
using System.Collections.Generic;
using Robust.Shared.Serialization;

namespace Content.Shared._Stalker.ItemUpgrades;

[Serializable, NetSerializable]
public sealed class STItemUpgradeItemEntry
{
    public NetEntity Entity;
    public string Name;
    public List<STItemUpgradeEntryView> Upgrades;
    public HashSet<string> InstalledUpgrades;
    public string? SelectedBranch;
    public int DurabilityPercent;
    public string? DurabilityState;
    public int RepairSteelRequired;
    public List<STToolRequirementView> RepairTools;

    public STItemUpgradeItemEntry(
        NetEntity entity,
        string name,
        List<STItemUpgradeEntryView> upgrades,
        HashSet<string> installedUpgrades,
        string? selectedBranch,
        int durabilityPercent,
        string? durabilityState,
        int repairSteelRequired,
        List<STToolRequirementView> repairTools)
    {
        Entity = entity;
        Name = name;
        Upgrades = upgrades;
        InstalledUpgrades = installedUpgrades;
        SelectedBranch = selectedBranch;
        DurabilityPercent = durabilityPercent;
        DurabilityState = durabilityState;
        RepairSteelRequired = repairSteelRequired;
        RepairTools = repairTools;
    }
}

[Serializable, NetSerializable]
public sealed class STItemRequirementView
{
    public string Id;
    public string Name;
    public int Amount;

    public STItemRequirementView(string id, string name, int amount)
    {
        Id = id;
        Name = name;
        Amount = amount;
    }
}

[Serializable, NetSerializable]
public sealed class STToolRequirementView
{
    public string Id;
    public string Name;

    public STToolRequirementView(string id, string name)
    {
        Id = id;
        Name = name;
    }
}

[Serializable, NetSerializable]
public sealed class STItemUpgradeEntryView
{
    public string Id;
    public string Name;
    public string? BranchId;
    public string? BranchName;
    public List<string> RequiredUpgrades;
    public List<STItemRequirementView> RequiredMaterials;
    public List<STToolRequirementView> RequiredTools;
    public float WeightMultiplier;
    public STItemGunUpgradeModifierView? Gun;
    public STItemArmorUpgradeModifierView? Armor;

    public STItemUpgradeEntryView(
        string id,
        string name,
        string? branchId,
        string? branchName,
        List<string> requiredUpgrades,
        List<STItemRequirementView> requiredMaterials,
        List<STToolRequirementView> requiredTools,
        float weightMultiplier,
        STItemGunUpgradeModifierView? gun,
        STItemArmorUpgradeModifierView? armor)
    {
        Id = id;
        Name = name;
        BranchId = branchId;
        BranchName = branchName;
        RequiredUpgrades = requiredUpgrades;
        RequiredMaterials = requiredMaterials;
        RequiredTools = requiredTools;
        WeightMultiplier = weightMultiplier;
        Gun = gun;
        Armor = armor;
    }
}

[Serializable, NetSerializable]
public sealed class STItemGunUpgradeModifierView
{
    public float FireRateMultiplier;
    public float MinAngleMultiplier;
    public float MaxAngleMultiplier;
    public float AngleIncreaseMultiplier;
    public float AngleDecayMultiplier;

    public STItemGunUpgradeModifierView(
        float fireRateMultiplier,
        float minAngleMultiplier,
        float maxAngleMultiplier,
        float angleIncreaseMultiplier,
        float angleDecayMultiplier)
    {
        FireRateMultiplier = fireRateMultiplier;
        MinAngleMultiplier = minAngleMultiplier;
        MaxAngleMultiplier = maxAngleMultiplier;
        AngleIncreaseMultiplier = angleIncreaseMultiplier;
        AngleDecayMultiplier = angleDecayMultiplier;
    }
}

[Serializable, NetSerializable]
public sealed class STArmorCoefficientModifierView
{
    public string DamageType;
    public float Multiplier;

    public STArmorCoefficientModifierView(string damageType, float multiplier)
    {
        DamageType = damageType;
        Multiplier = multiplier;
    }
}

[Serializable, NetSerializable]
public sealed class STArmorFlatModifierView
{
    public string DamageType;
    public float Add;

    public STArmorFlatModifierView(string damageType, float add)
    {
        DamageType = damageType;
        Add = add;
    }
}

[Serializable, NetSerializable]
public sealed class STItemArmorUpgradeModifierView
{
    public List<STArmorCoefficientModifierView> Coefficients;
    public List<STArmorFlatModifierView> FlatReductions;

    public STItemArmorUpgradeModifierView(
        List<STArmorCoefficientModifierView> coefficients,
        List<STArmorFlatModifierView> flatReductions)
    {
        Coefficients = coefficients;
        FlatReductions = flatReductions;
    }
}

[Serializable, NetSerializable]
public sealed class STItemUpgradeBoundUserInterfaceState
{
    public List<STItemUpgradeItemEntry> Items;

    public STItemUpgradeBoundUserInterfaceState(List<STItemUpgradeItemEntry> items)
    {
        Items = items;
    }
}

[Serializable, NetSerializable]
public sealed class STItemUpgradeOpenEvent : EntityEventArgs
{
    public List<STItemUpgradeItemEntry> Items;

    public STItemUpgradeOpenEvent(List<STItemUpgradeItemEntry> items)
    {
        Items = items;
    }
}

[Serializable, NetSerializable]
public sealed class STItemUpgradeInstallRequestEvent : EntityEventArgs
{
    public NetEntity User;
    public NetEntity Item;
    public string UpgradeId;

    public STItemUpgradeInstallRequestEvent(NetEntity user, NetEntity item, string upgradeId)
    {
        User = user;
        Item = item;
        UpgradeId = upgradeId;
    }
}

[Serializable, NetSerializable]
public sealed class STItemUpgradeResetRequestEvent : EntityEventArgs
{
    public NetEntity User;
    public NetEntity Item;

    public STItemUpgradeResetRequestEvent(NetEntity user, NetEntity item)
    {
        User = user;
        Item = item;
    }
}

[Serializable, NetSerializable]
public sealed class STItemRepairRequestEvent : EntityEventArgs
{
    public NetEntity User;
    public NetEntity Item;

    public STItemRepairRequestEvent(NetEntity user, NetEntity item)
    {
        User = user;
        Item = item;
    }
}