using Content.Shared._Stalker.Weight;
using Content.Shared._Stalker_EN.Weight.Modifier;
using Content.Shared.StatusEffectNew;
using Content.Shared.StatusEffectNew.Components;
using Robust.Shared.Prototypes;

namespace Content.Shared._Stalker_EN.Weight.Systems;

/// <summary>
/// Applies temporary weight modifiers through status effects.
/// </summary>
public sealed class STWeightModStatusSystem : EntitySystem
{
    [Dependency] private readonly StatusEffectsSystem _status = default!;
    [Dependency] private readonly STWeightSystem _weightSystem = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<STWeightSelfModifierComponent, StatusEffectRemovedEvent>(OnWeightModRemoved);
    }

    private void OnWeightModRemoved(Entity<STWeightSelfModifierComponent> ent, ref StatusEffectRemovedEvent args)
    {
        if (TryComp(args.Target, out STWeightComponent? weight))
        {
            weight.Self -= ent.Comp.Modifier;
            Dirty(args.Target, weight);
            _weightSystem.TryUpdateWeight(args.Target);
        }
    }

    public bool TryUpdateWeightModDuration(EntityUid uid, EntProtoId effectProto, TimeSpan? duration, float modifier)
    {
        var hasExistingEffect = _status.HasStatusEffect(uid, effectProto);

        if (!_status.TryUpdateStatusEffectDuration(uid, effectProto, out var statusEffect, duration ?? TimeSpan.FromSeconds(2)))
            return false;

        if (statusEffect is { } statusUid && TryComp(statusUid, out STWeightSelfModifierComponent? comp))
        {
            comp.Modifier = modifier;
            Dirty(statusUid, comp);
        }

        if (!hasExistingEffect && TryComp(uid, out STWeightComponent? weight))
        {
            weight.Self += modifier;
            Dirty(uid, weight);
            _weightSystem.TryUpdateWeight(uid);
        }

        return true;
    }
}