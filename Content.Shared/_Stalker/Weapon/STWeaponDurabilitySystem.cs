using System;
using Content.Shared.Examine;
using Content.Shared.Weapons.Ranged.Components;
using Content.Shared.Weapons.Ranged.Events;
using Content.Shared.Weapons.Ranged.Systems;
using Robust.Shared.GameObjects;
using Robust.Shared.Random;

namespace Content.Shared._Stalker.Weapon;

public sealed class STWeaponDurabilitySystem : EntitySystem
{
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly SharedGunSystem _gun = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<STWeaponDurabilityComponent, ShotAttemptedEvent>(OnShot);
        SubscribeLocalEvent<STWeaponDurabilityComponent, GunRefreshModifiersEvent>(OnRefresh);
        SubscribeLocalEvent<STWeaponDurabilityComponent, ExaminedEvent>(OnExamined);
    }

    private void OnShot(EntityUid uid, STWeaponDurabilityComponent comp, ref ShotAttemptedEvent args)
    {
        comp.CurrentDurability -= comp.DurabilityLossPerShot;
        if (comp.CurrentDurability < 0f)
            comp.CurrentDurability = 0f;

        if (!comp.CanJam)
            return;

        var jamChance = GetJamChance(comp.Ratio);
        if (_random.Prob(jamChance))
        {
            args.Cancel();
            TryJamWeapon(uid);
        }
    }

    private void OnRefresh(EntityUid uid, STWeaponDurabilityComponent comp, ref GunRefreshModifiersEvent args)
    {
        var spreadMultiplier = GetSpreadMultiplier(comp.Ratio);

        args.MinAngle *= spreadMultiplier;
        args.MaxAngle *= spreadMultiplier;
        args.AngleIncrease *= spreadMultiplier;
    }

    private void OnExamined(EntityUid uid, STWeaponDurabilityComponent comp, ref ExaminedEvent args)
    {
        if (!args.IsInDetailsRange)
            return;

        var percent = GetDurabilityPercent(comp);
        var stateText = GetDurabilityStateText(percent);

        args.PushMarkup($"[color=yellow]Состояние:[/color] {stateText}");
    }

    private void TryJamWeapon(EntityUid uid)
    {
        if (!TryComp<ChamberMagazineAmmoProviderComponent>(uid, out var chamber))
            return;

        if (chamber.BoltClosed == null)
            return;

        if (chamber.BoltClosed == false)
            return;

        _gun.SetBoltClosed(uid, chamber, false);
    }

    public void SetDurability(EntityUid uid, STWeaponDurabilityComponent comp, float currentDurability)
    {
        comp.CurrentDurability = Math.Clamp(currentDurability, 0f, comp.MaxDurability);
    }

    public static int GetDurabilityPercent(STWeaponDurabilityComponent comp)
    {
        if (comp.MaxDurability <= 0f)
            return 100;

        var ratio = comp.CurrentDurability / comp.MaxDurability;
        var percent = (int)MathF.Round(ratio * 100f);

        if (percent < 0)
            percent = 0;

        if (percent > 100)
            percent = 100;

        return percent;
    }

    public static string GetDurabilityStateText(int percent)
    {
        if (percent >= 75)
            return "отличное";

        if (percent >= 50)
            return "нормальное";

        if (percent >= 25)
            return "поношенное";

        if (percent >= 10)
            return "сильно поношенное";

        return "аварийное";
    }

    public static int GetRepairSteelRequired(int percent)
    {
        if (percent >= 100)
            return 0;

        if (percent <= 20)
            return 4;

        if (percent <= 40)
            return 3;

        if (percent <= 60)
            return 2;

        return 1;
    }

    private static float GetSpreadMultiplier(float ratio)
    {
        if (ratio >= 0.75f)
            return 1.00f;

        if (ratio >= 0.50f)
            return 1.10f;

        if (ratio >= 0.25f)
            return 1.25f;

        if (ratio >= 0.10f)
            return 1.45f;

        return 1.75f;
    }

    private static float GetJamChance(float ratio)
    {
        if (ratio >= 0.75f)
            return 0.00f;

        if (ratio >= 0.50f)
            return 0.01f;

        if (ratio >= 0.25f)
            return 0.03f;

        if (ratio >= 0.10f)
            return 0.07f;

        return 0.15f;
    }
}