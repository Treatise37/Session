using System;
using System.Collections.Generic;
using System.Linq;
using Robust.Shared.Serialization;

namespace Content.Shared._Stalker.Storage;

[Serializable, NetSerializable]
public sealed class WeaponItemStalker : IItemStalkerStorage
{
    public string ClassType { get; set; } = "WeaponItemStalker";
    public string PrototypeName { get; set; } = "";
    public uint CountVendingMachine { get; set; } = 1;

    public float CurrentDurability { get; set; }
    public float MaxDurability { get; set; }
    public string? SelectedBranch { get; set; }
    public List<string> InstalledUpgrades { get; set; } = new();
    public float? BaseWeight { get; set; }

    public WeaponItemStalker()
    {
    }

    public WeaponItemStalker(
        string prototypeName,
        float currentDurability,
        float maxDurability,
        string? selectedBranch,
        List<string>? installedUpgrades,
        float? baseWeight,
        uint countVendingMachine = 1)
    {
        PrototypeName = prototypeName;
        CurrentDurability = currentDurability;
        MaxDurability = maxDurability;
        SelectedBranch = selectedBranch;
        InstalledUpgrades = installedUpgrades ?? new List<string>();
        BaseWeight = baseWeight;
        CountVendingMachine = countVendingMachine;
    }

    public string Identifier()
    {
        var upgrades = InstalledUpgrades
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .OrderBy(x => x);

        var upgradesPart = string.Join(",", upgrades);

        return $"{PrototypeName}|dur:{CurrentDurability:0.###}|max:{MaxDurability:0.###}|branch:{SelectedBranch ?? ""}|mods:{upgradesPart}|bw:{BaseWeight?.ToString() ?? ""}";
    }
}