using System;
using System.Collections.Generic;
using Content.Shared._Stalker.Armor;
using Content.Shared._Stalker.ItemUpgrades;
using Content.Shared._Stalker.Weapon;
using Content.Shared.Interaction;
using Content.Shared.Stacks;
using Content.Shared.Storage.EntitySystems;
using Robust.Shared.Containers;
using Robust.Shared.GameObjects;
using Robust.Shared.Prototypes;
using Robust.Shared.Localization;

namespace Content.Server._Stalker.ItemUpgrades;

public sealed class STItemUpgradeBenchSystem : EntitySystem
{
    private const string RepairSteelStackType = "Steel";
    private const string RepairToolProto = "Screwdriver";

    [Dependency] private readonly SharedContainerSystem _container = default!;
    [Dependency] private readonly SharedStackSystem _stack = default!;
    [Dependency] private readonly STItemUpgradeSystem _upgradeSystem = default!;
    [Dependency] private readonly STArmorDurabilitySystem _armorDurability = default!;
    [Dependency] private readonly IPrototypeManager _prototype = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<STItemUpgradeBenchComponent, ActivateInWorldEvent>(OnUse);
        SubscribeNetworkEvent<STItemUpgradeInstallRequestEvent>(OnInstallRequest);
        SubscribeNetworkEvent<STItemUpgradeResetRequestEvent>(OnResetRequest);
        SubscribeNetworkEvent<STItemRepairRequestEvent>(OnRepairRequest);
    }

    private void OnUse(EntityUid uid, STItemUpgradeBenchComponent comp, ActivateInWorldEvent args)
    {
        SendUpdatedState(args.User);
        args.Handled = true;
    }

    private void OnInstallRequest(STItemUpgradeInstallRequestEvent ev)
    {
        var user = GetEntity(ev.User);
        var item = GetEntity(ev.Item);

        if (user == EntityUid.Invalid || item == EntityUid.Invalid)
            return;

        if (!TryComp<STItemUpgradesComponent>(item, out var upgrades))
            return;

        var target = _upgradeSystem.GetUpgrade((item, upgrades), ev.UpgradeId);
        if (target == null)
            return;

        if (!_upgradeSystem.CanInstallUpgrade((item, upgrades), ev.UpgradeId))
            return;

        if (!HasRequiredTools(user, target))
            return;

        if (!HasRequiredMaterials(user, target))
            return;

        if (!ConsumeRequiredMaterials(user, target))
            return;

        if (!_upgradeSystem.TryInstallUpgrade((item, upgrades), ev.UpgradeId))
            return;

        SendUpdatedState(user);
    }

    private void OnResetRequest(STItemUpgradeResetRequestEvent ev)
    {
        var user = GetEntity(ev.User);
        var item = GetEntity(ev.Item);

        if (user == EntityUid.Invalid || item == EntityUid.Invalid)
            return;

        if (!TryComp<STItemUpgradesComponent>(item, out var upgrades))
            return;

        _upgradeSystem.ResetUpgrades((item, upgrades));
        SendUpdatedState(user);
    }

    private void OnRepairRequest(STItemRepairRequestEvent ev)
    {
        var user = GetEntity(ev.User);
        var item = GetEntity(ev.Item);

        if (user == EntityUid.Invalid || item == EntityUid.Invalid)
            return;

        if (!TryGetRepairData(item, out var steelRequired, out _, out var needsRepair))
            return;

        if (!needsRepair || steelRequired <= 0)
            return;

        if (!HasPrototypeSomewhere(user, RepairToolProto))
            return;

        if (CountStackTypeSomewhere(user, RepairSteelStackType) < steelRequired)
            return;

        if (!ConsumeStackTypeSomewhere(user, RepairSteelStackType, steelRequired))
            return;

        if (TryComp<STWeaponDurabilityComponent>(item, out var weaponDurability))
        {
            weaponDurability.CurrentDurability = weaponDurability.MaxDurability;
        }

        if (TryComp<STArmorDurabilityComponent>(item, out var armorDurability))
        {
            _armorDurability.RestoreDurability(item, armorDurability);
        }

        SendUpdatedState(user);
    }

    private void SendUpdatedState(EntityUid user)
    {
        var items = new List<STItemUpgradeItemEntry>();
        var visited = new HashSet<EntityUid>();
        CollectUpgradeableItems(user, items, visited);

        RaiseNetworkEvent(new STItemUpgradeOpenEvent(items), user);
    }

    private void CollectUpgradeableItems(EntityUid uid, List<STItemUpgradeItemEntry> result, HashSet<EntityUid> visited)
    {
        if (!visited.Add(uid))
            return;

        if (TryComp<STItemUpgradesComponent>(uid, out var upgrades))
        {
            var name = MetaData(uid).EntityName;

            var durabilityState = string.Empty;
            var repairSteelRequired = 0;
            var repairTools = new List<STToolRequirementView>();

            if (TryGetRepairData(uid, out repairSteelRequired, out durabilityState, out _))
                repairTools = BuildRepairToolViews();

            result.Add(new STItemUpgradeItemEntry(
                GetNetEntity(uid),
                name,
                BuildUpgradeViews(upgrades),
                new HashSet<string>(upgrades.InstalledUpgrades),
                upgrades.SelectedBranch,
                0,
                durabilityState,
                repairSteelRequired,
                repairTools));
        }

        if (!TryComp<ContainerManagerComponent>(uid, out var containerManager))
            return;

        foreach (var container in _container.GetAllContainers(uid, containerManager))
        {
            foreach (var ent in container.ContainedEntities)
            {
                CollectUpgradeableItems(ent, result, visited);
            }
        }
    }

    private bool TryGetRepairData(
        EntityUid uid,
        out int steelRequired,
        out string durabilityState,
        out bool needsRepair)
    {
        steelRequired = 0;
        durabilityState = string.Empty;
        needsRepair = false;

        if (TryComp<STWeaponDurabilityComponent>(uid, out var weaponDurability))
        {
            var fraction = weaponDurability.MaxDurability <= 0f
                ? 0f
                : Math.Clamp(weaponDurability.CurrentDurability / weaponDurability.MaxDurability, 0f, 1f);

            durabilityState = GetDurabilityState(fraction);
            steelRequired = GetRepairSteelRequired(fraction);
            needsRepair = fraction < 0.999f;
            return true;
        }

        if (TryComp<STArmorDurabilityComponent>(uid, out var armorDurability))
        {
            var fraction = _armorDurability.GetDurabilityFraction(armorDurability);
            durabilityState = _armorDurability.GetDurabilityState(armorDurability);
            steelRequired = GetRepairSteelRequired(fraction);
            needsRepair = fraction < 0.999f;
            return true;
        }

        return false;
    }

    private static string GetDurabilityState(float fraction)
    {
        if (fraction <= 0.2f)
            return "сильно поношенное";

        if (fraction <= 0.4f)
            return "поношенное";

        if (fraction <= 0.7f)
            return "слегка поношенное";

        return "исправное";
    }

    private static int GetRepairSteelRequired(float fraction)
    {
        if (fraction <= 0.2f)
            return 4;

        if (fraction <= 0.4f)
            return 3;

        if (fraction <= 0.6f)
            return 2;

        if (fraction < 1f)
            return 1;

        return 0;
    }

    private bool HasRequiredTools(EntityUid user, STItemUpgradeEntry upgrade)
    {
        foreach (var requiredToolId in upgrade.RequiredTools)
        {
            if (string.IsNullOrWhiteSpace(requiredToolId))
                continue;

            if (!HasPrototypeSomewhere(user, requiredToolId))
                return false;
        }

        return true;
    }

    private bool HasRequiredMaterials(EntityUid user, STItemUpgradeEntry upgrade)
    {
        foreach (var pair in upgrade.RequiredMaterials)
        {
            if (string.IsNullOrWhiteSpace(pair.Key) || pair.Value <= 0)
                continue;

            if (CountStackTypeSomewhere(user, pair.Key) < pair.Value)
                return false;
        }

        return true;
    }

    private bool ConsumeRequiredMaterials(EntityUid user, STItemUpgradeEntry upgrade)
    {
        foreach (var pair in upgrade.RequiredMaterials)
        {
            if (string.IsNullOrWhiteSpace(pair.Key) || pair.Value <= 0)
                continue;

            if (!ConsumeStackTypeSomewhere(user, pair.Key, pair.Value))
                return false;
        }

        return true;
    }

    private bool HasPrototypeSomewhere(EntityUid user, string prototypeId)
    {
        var visited = new HashSet<EntityUid>();
        return HasPrototypeRecursive(user, prototypeId, visited);
    }

    private bool HasPrototypeRecursive(EntityUid uid, string prototypeId, HashSet<EntityUid> visited)
    {
        if (!visited.Add(uid))
            return false;

        var meta = MetaData(uid);
        if (meta.EntityPrototype?.ID == prototypeId)
            return true;

        if (!TryComp<ContainerManagerComponent>(uid, out var containerManager))
            return false;

        foreach (var container in _container.GetAllContainers(uid, containerManager))
        {
            foreach (var ent in container.ContainedEntities)
            {
                if (HasPrototypeRecursive(ent, prototypeId, visited))
                    return true;
            }
        }

        return false;
    }

    private int CountStackTypeSomewhere(EntityUid user, string stackTypeId)
    {
        var visited = new HashSet<EntityUid>();
        return CountStackTypeRecursive(user, stackTypeId, visited);
    }

    private int CountStackTypeRecursive(EntityUid uid, string stackTypeId, HashSet<EntityUid> visited)
    {
        if (!visited.Add(uid))
            return 0;

        var total = 0;

        if (TryComp<StackComponent>(uid, out var stack) &&
            string.Equals(stack.StackTypeId, stackTypeId, StringComparison.Ordinal))
        {
            total += stack.Count;
        }

        if (!TryComp<ContainerManagerComponent>(uid, out var containerManager))
            return total;

        foreach (var container in _container.GetAllContainers(uid, containerManager))
        {
            foreach (var ent in container.ContainedEntities)
            {
                total += CountStackTypeRecursive(ent, stackTypeId, visited);
            }
        }

        return total;
    }

    private bool ConsumeStackTypeSomewhere(EntityUid user, string stackTypeId, int amount)
    {
        var candidates = new List<EntityUid>();
        var visited = new HashSet<EntityUid>();

        GatherStackTypeEntities(user, stackTypeId, candidates, visited);

        var remaining = amount;

        foreach (var candidate in candidates)
        {
            if (remaining <= 0)
                break;

            if (!TryComp<StackComponent>(candidate, out var stack))
                continue;

            var take = Math.Min(remaining, stack.Count);
            if (take <= 0)
                continue;

            if (!_stack.TryUse((candidate, stack), take))
                continue;

            remaining -= take;
        }

        return remaining <= 0;
    }

    private void GatherStackTypeEntities(EntityUid uid, string stackTypeId, List<EntityUid> result, HashSet<EntityUid> visited)
    {
        if (!visited.Add(uid))
            return;

        if (TryComp<StackComponent>(uid, out var stack) &&
            string.Equals(stack.StackTypeId, stackTypeId, StringComparison.Ordinal))
        {
            result.Add(uid);
        }

        if (!TryComp<ContainerManagerComponent>(uid, out var containerManager))
            return;

        foreach (var container in _container.GetAllContainers(uid, containerManager))
        {
            foreach (var ent in container.ContainedEntities)
            {
                GatherStackTypeEntities(ent, stackTypeId, result, visited);
            }
        }
    }

    private List<STItemRequirementView> BuildMaterialViews(STItemUpgradeEntry upgrade)
    {
        var result = new List<STItemRequirementView>();

        foreach (var pair in upgrade.RequiredMaterials)
        {
            if (string.IsNullOrWhiteSpace(pair.Key) || pair.Value <= 0)
                continue;

            result.Add(new STItemRequirementView(
                pair.Key,
                ResolveMaterialName(pair.Key),
                pair.Value));
        }

        return result;
    }

    private List<STToolRequirementView> BuildToolViews(STItemUpgradeEntry upgrade)
    {
        var result = new List<STToolRequirementView>();

        foreach (var toolId in upgrade.RequiredTools)
        {
            if (string.IsNullOrWhiteSpace(toolId))
                continue;

            result.Add(new STToolRequirementView(
                toolId,
                ResolvePrototypeName(toolId)));
        }

        return result;
    }

    private List<STToolRequirementView> BuildRepairToolViews()
    {
        return new List<STToolRequirementView>
        {
            new(RepairToolProto, ResolvePrototypeName(RepairToolProto))
        };
    }

    private string ResolvePrototypeName(string prototypeId)
    {
        if (_prototype.TryIndex<EntityPrototype>(prototypeId, out var proto))
            return Loc.GetString(proto.Name);

        return prototypeId;
    }

    private static string ResolveMaterialName(string stackTypeId)
    {
        return stackTypeId switch
        {
            "Steel" => "Сталь",
            "Glass" => "Стекло",
            "Cloth" => "Ткань",
            "Plasma" => "Плазма",
            "Plastic" => "Пластик",
            "Wood" => "Дерево",
            "SheetSteel1" => "Сталь",
            "SheetSteel10" => "Сталь",
            "SheetGlass1" => "Стекло",
            "SheetGlass10" => "Стекло",
            "SheetCloth1" => "Ткань",
            "SheetCloth10" => "Ткань",
            _ => stackTypeId
        };
    }

    private List<STItemUpgradeEntryView> BuildUpgradeViews(STItemUpgradesComponent comp)
    {
        var result = new List<STItemUpgradeEntryView>();

        foreach (var upgrade in comp.Upgrades)
        {
            STItemGunUpgradeModifierView? gun = null;
            if (upgrade.Gun != null)
            {
                gun = new STItemGunUpgradeModifierView(
                    upgrade.Gun.FireRateMultiplier,
                    upgrade.Gun.MinAngleMultiplier,
                    upgrade.Gun.MaxAngleMultiplier,
                    upgrade.Gun.AngleIncreaseMultiplier,
                    upgrade.Gun.AngleDecayMultiplier);
            }

            STItemArmorUpgradeModifierView? armor = null;
            if (upgrade.Armor != null)
            {
                var coefficients = new List<STArmorCoefficientModifierView>();
                foreach (var coef in upgrade.Armor.Coefficients)
                {
                    coefficients.Add(new STArmorCoefficientModifierView(coef.DamageType, coef.Multiplier));
                }

                var flatReductions = new List<STArmorFlatModifierView>();
                foreach (var flat in upgrade.Armor.FlatReductions)
                {
                    flatReductions.Add(new STArmorFlatModifierView(flat.DamageType, flat.Add));
                }

                armor = new STItemArmorUpgradeModifierView(coefficients, flatReductions);
            }

            result.Add(new STItemUpgradeEntryView(
                upgrade.Id,
                upgrade.Name,
                upgrade.BranchId,
                upgrade.BranchName,
                new List<string>(upgrade.RequiredUpgrades),
                BuildMaterialViews(upgrade),
                BuildToolViews(upgrade),
                upgrade.WeightMultiplier,
                gun,
                armor));
        }

        return result;
    }
}