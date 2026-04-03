using System;
using System.Collections.Generic;
using System.Linq;
using Content.Shared._Stalker.ItemUpgrades.Prototypes;
using Content.Shared.Armor;
using Content.Shared.Examine;
using Content.Shared.Weapons.Ranged.Events;
using Robust.Shared.GameObjects;
using Robust.Shared.Maths;
using Robust.Shared.Prototypes;

namespace Content.Shared._Stalker.ItemUpgrades;

public sealed partial class STItemUpgradeSystem : EntitySystem
{
    [Dependency] private readonly IPrototypeManager _prototype = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<STItemUpgradesComponent, ComponentStartup>(OnStartup);
        SubscribeLocalEvent<STItemUpgradesComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<STItemUpgradesComponent, ExaminedEvent>(OnExamined);
        SubscribeLocalEvent<STItemUpgradesComponent, GunRefreshModifiersEvent>(OnGunRefreshModifiers);
        SubscribeLocalEvent<STItemUpgradesComponent, CoefficientQueryEvent>(OnArmorCoefficientQuery);
    }

    private void OnStartup(Entity<STItemUpgradesComponent> ent, ref ComponentStartup args)
    {
        EnsureTreeLoaded(ent);
        RefreshUpgrades(ent);
    }

    private void OnMapInit(Entity<STItemUpgradesComponent> ent, ref MapInitEvent args)
    {
        EnsureTreeLoaded(ent);
        RefreshUpgrades(ent);
    }

    private void OnExamined(Entity<STItemUpgradesComponent> ent, ref ExaminedEvent args)
    {
        if (!args.IsInDetailsRange)
            return;

        var installed = ent.Comp.Upgrades
            .Where(x => ent.Comp.InstalledUpgrades.Contains(x.Id))
            .Select(x => x.Name)
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Distinct()
            .OrderBy(x => x)
            .ToList();

        if (installed.Count == 0)
        {
            args.PushMarkup("[color=gray]Модификации: отсутствуют[/color]");
            return;
        }

        args.PushMarkup($"[color=yellow]Модификации:[/color] {string.Join(", ", installed)}");
    }

    private void OnGunRefreshModifiers(Entity<STItemUpgradesComponent> ent, ref GunRefreshModifiersEvent args)
    {
        foreach (var upgrade in GetInstalledUpgrades(ent))
        {
            if (upgrade.Gun == null)
                continue;

            args.FireRate *= upgrade.Gun.FireRateMultiplier;
            args.MinAngle = MultiplyAngle(args.MinAngle, upgrade.Gun.MinAngleMultiplier);
            args.MaxAngle = MultiplyAngle(args.MaxAngle, upgrade.Gun.MaxAngleMultiplier);
            args.AngleIncrease = MultiplyAngle(args.AngleIncrease, upgrade.Gun.AngleIncreaseMultiplier);
            args.AngleDecay = MultiplyAngle(args.AngleDecay, upgrade.Gun.AngleDecayMultiplier);
        }

        args.FireRate = MathF.Max(args.FireRate, 0.01f);
        args.MinAngle = ClampAngleToZero(args.MinAngle);
        args.MaxAngle = ClampAngleToZero(args.MaxAngle);
        args.AngleIncrease = ClampAngleToZero(args.AngleIncrease);
        args.AngleDecay = ClampAngleToZero(args.AngleDecay);
    }

    private void OnArmorCoefficientQuery(Entity<STItemUpgradesComponent> ent, ref CoefficientQueryEvent args)
    {
        foreach (var upgrade in GetInstalledUpgrades(ent))
        {
            if (upgrade.Armor == null)
                continue;

            foreach (var coefficient in upgrade.Armor.Coefficients)
            {
                if (string.IsNullOrWhiteSpace(coefficient.DamageType))
                    continue;

                if (args.DamageModifiers.Coefficients.TryGetValue(coefficient.DamageType, out var current))
                    args.DamageModifiers.Coefficients[coefficient.DamageType] = current * coefficient.Multiplier;
                else
                    args.DamageModifiers.Coefficients[coefficient.DamageType] = coefficient.Multiplier;
            }

            foreach (var flat in upgrade.Armor.FlatReductions)
            {
                if (string.IsNullOrWhiteSpace(flat.DamageType))
                    continue;

                if (args.DamageModifiers.FlatReduction.TryGetValue(flat.DamageType, out var current))
                    args.DamageModifiers.FlatReduction[flat.DamageType] = current + flat.Add;
                else
                    args.DamageModifiers.FlatReduction[flat.DamageType] = flat.Add;
            }
        }
    }

    public bool CanInstallUpgrade(Entity<STItemUpgradesComponent> ent, string upgradeId)
    {
        if (string.IsNullOrWhiteSpace(upgradeId))
            return false;

        var upgrade = GetUpgrade(ent, upgradeId);
        if (upgrade == null)
            return false;

        if (ent.Comp.InstalledUpgrades.Contains(upgradeId))
            return false;

        if (!AreRequirementsMet(ent, upgrade))
            return false;

        if (!IsBranchCompatible(ent, upgrade))
            return false;

        return true;
    }

    public bool TryInstallUpgrade(Entity<STItemUpgradesComponent> ent, string upgradeId)
    {
        if (!CanInstallUpgrade(ent, upgradeId))
            return false;

        var upgrade = GetUpgrade(ent, upgradeId)!;
        ent.Comp.InstalledUpgrades.Add(upgrade.Id);

        if (string.IsNullOrWhiteSpace(ent.Comp.SelectedBranch) &&
            !string.IsNullOrWhiteSpace(upgrade.BranchId))
        {
            ent.Comp.SelectedBranch = upgrade.BranchId;
        }

        RefreshUpgrades(ent);
        return true;
    }

    public void ResetUpgrades(Entity<STItemUpgradesComponent> ent)
    {
        ent.Comp.InstalledUpgrades.Clear();
        ent.Comp.SelectedBranch = null;
        RefreshUpgrades(ent);
    }

    public STItemUpgradeEntry? GetUpgrade(Entity<STItemUpgradesComponent> ent, string upgradeId)
    {
        return ent.Comp.Upgrades.FirstOrDefault(x => x.Id == upgradeId);
    }

    public List<STItemUpgradeEntry> GetAvailableUpgrades(Entity<STItemUpgradesComponent> ent)
    {
        return ent.Comp.Upgrades.Where(x => CanInstallUpgrade(ent, x.Id)).ToList();
    }

    private void EnsureTreeLoaded(Entity<STItemUpgradesComponent> ent)
    {
        if (ent.Comp.Tree == null)
            return;

        if (!_prototype.TryIndex(ent.Comp.Tree.Value, out STUpgradeTreePrototype? tree))
            return;

        ent.Comp.Upgrades = CloneUpgrades(tree.Upgrades);
    }

    private static List<STItemUpgradeEntry> CloneUpgrades(List<STItemUpgradeEntry> source)
    {
        var result = new List<STItemUpgradeEntry>(source.Count);

        foreach (var upgrade in source)
        {
            result.Add(CloneUpgrade(upgrade));
        }

        return result;
    }

    private static STItemUpgradeEntry CloneUpgrade(STItemUpgradeEntry source)
    {
        return new STItemUpgradeEntry
        {
            Id = source.Id,
            Name = source.Name,
            BranchId = source.BranchId,
            BranchName = source.BranchName,
            RequiredUpgrades = new List<string>(source.RequiredUpgrades),
            RequiredMaterials = new Dictionary<string, int>(source.RequiredMaterials),
            RequiredTools = new List<string>(source.RequiredTools),
            WeightMultiplier = source.WeightMultiplier,
            Gun = source.Gun == null ? null : new STItemGunUpgradeModifier
            {
                FireRateMultiplier = source.Gun.FireRateMultiplier,
                MinAngleMultiplier = source.Gun.MinAngleMultiplier,
                MaxAngleMultiplier = source.Gun.MaxAngleMultiplier,
                AngleIncreaseMultiplier = source.Gun.AngleIncreaseMultiplier,
                AngleDecayMultiplier = source.Gun.AngleDecayMultiplier
            },
            Armor = source.Armor == null ? null : new STItemArmorUpgradeModifier
            {
                Coefficients = source.Armor.Coefficients
                    .Select(x => new STArmorCoefficientModifier
                    {
                        DamageType = x.DamageType,
                        Multiplier = x.Multiplier
                    })
                    .ToList(),
                FlatReductions = source.Armor.FlatReductions
                    .Select(x => new STArmorFlatModifier
                    {
                        DamageType = x.DamageType,
                        Add = x.Add
                    })
                    .ToList()
            }
        };
    }

    private bool AreRequirementsMet(Entity<STItemUpgradesComponent> ent, STItemUpgradeEntry upgrade)
    {
        foreach (var required in upgrade.RequiredUpgrades)
        {
            if (!ent.Comp.InstalledUpgrades.Contains(required))
                return false;
        }

        return true;
    }

    private bool IsBranchCompatible(Entity<STItemUpgradesComponent> ent, STItemUpgradeEntry upgrade)
    {
        if (string.IsNullOrWhiteSpace(upgrade.BranchId))
            return true;

        if (string.IsNullOrWhiteSpace(ent.Comp.SelectedBranch))
            return true;

        return string.Equals(ent.Comp.SelectedBranch, upgrade.BranchId, StringComparison.Ordinal);
    }

    private void RefreshUpgrades(Entity<STItemUpgradesComponent> ent)
    {
        ValidateInstalledUpgrades(ent);
        RefreshWeight(ent);
        RefreshGun(ent);
    }

    private void ValidateInstalledUpgrades(Entity<STItemUpgradesComponent> ent)
    {
        var validIds = ent.Comp.Upgrades
            .Select(x => x.Id)
            .ToHashSet();

        ent.Comp.InstalledUpgrades.RemoveWhere(id => !validIds.Contains(id));

        if (!string.IsNullOrWhiteSpace(ent.Comp.SelectedBranch))
        {
            var hasBranchUpgrade = ent.Comp.Upgrades.Any(x =>
                ent.Comp.InstalledUpgrades.Contains(x.Id) &&
                x.BranchId == ent.Comp.SelectedBranch);

            if (!hasBranchUpgrade)
                ent.Comp.SelectedBranch = null;
        }
    }

    private void RefreshWeight(Entity<STItemUpgradesComponent> ent)
    {
        if (!TryComp<Content.Shared._Stalker.Weight.STWeightComponent>(ent, out var weight))
            return;

        if (ent.Comp.BaseWeight == null)
            ent.Comp.BaseWeight = weight.Self;

        var multiplier = GetInstalledUpgrades(ent)
            .Aggregate(1f, (current, upgrade) => current * upgrade.WeightMultiplier);

        weight.Self = ent.Comp.BaseWeight.Value * multiplier;
        Dirty(ent, weight);
    }

    private void RefreshGun(Entity<STItemUpgradesComponent> ent)
    {
    }

    private IEnumerable<STItemUpgradeEntry> GetInstalledUpgrades(Entity<STItemUpgradesComponent> ent)
    {
        return ent.Comp.Upgrades.Where(x => ent.Comp.InstalledUpgrades.Contains(x.Id));
    }

    private static Angle MultiplyAngle(Angle angle, float multiplier)
    {
        return Angle.FromDegrees(angle.Degrees * multiplier);
    }

    private static Angle ClampAngleToZero(Angle angle)
    {
        return angle.Degrees < 0f ? Angle.Zero : angle;
    }

    public void RestoreUpgrades(Entity<STItemUpgradesComponent> ent, IEnumerable<string> installedUpgradeIds, string? selectedBranch)
    {
        EnsureTreeLoaded(ent);

        ent.Comp.InstalledUpgrades.Clear();
        ent.Comp.SelectedBranch = null;

        var pending = installedUpgradeIds
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Distinct()
            .ToList();

        var madeProgress = true;
        while (pending.Count > 0 && madeProgress)
        {
            madeProgress = false;

            for (var i = pending.Count - 1; i >= 0; i--)
            {
                var upgradeId = pending[i];

                if (!CanInstallUpgrade(ent, upgradeId))
                    continue;

                var upgrade = GetUpgrade(ent, upgradeId);
                if (upgrade == null)
                    continue;

                ent.Comp.InstalledUpgrades.Add(upgrade.Id);

                if (string.IsNullOrWhiteSpace(ent.Comp.SelectedBranch) &&
                    !string.IsNullOrWhiteSpace(upgrade.BranchId))
                {
                    ent.Comp.SelectedBranch = upgrade.BranchId;
                }

                pending.RemoveAt(i);
                madeProgress = true;
            }
        }

        if (!string.IsNullOrWhiteSpace(selectedBranch))
            ent.Comp.SelectedBranch = selectedBranch;

        RefreshUpgrades(ent);
    }
}