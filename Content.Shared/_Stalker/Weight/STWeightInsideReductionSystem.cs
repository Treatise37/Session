using System;
using Robust.Shared.GameObjects;

namespace Content.Shared._Stalker.Weight;

public sealed partial class STWeightInsideReductionSystem : EntitySystem
{
    public override void Initialize()
    {
        SubscribeLocalEvent<STWeightComponent, GetWeightModifiersEvent>(OnGetWeightModifiers);
    }

    private void OnGetWeightModifiers(EntityUid uid, STWeightComponent comp, ref GetWeightModifiersEvent args)
    {
        if (!TryComp<STWeightInsideReductionComponent>(uid, out var reduction) || reduction.ReductionFraction <= 0f)
            return;

        var multiplier = 1f - (float)Math.Clamp(reduction.ReductionFraction, 0f, 1f);
        args.Inside *= multiplier;
    }
}
