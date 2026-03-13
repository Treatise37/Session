using System;
using Robust.Shared.Localization;

namespace Content.Shared._Stalker.Weight;

public sealed partial class STWeightInsideReductionExamineSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<STWeightInsideReductionComponent, Examine.ExaminedEvent>(OnExamined);
    }

    private void OnExamined(Entity<STWeightInsideReductionComponent> entity, ref Examine.ExaminedEvent args)
    {
        if (!args.IsInDetailsRange)
            return;

        if (entity.Comp.ReductionFraction <= 0f)
            return;

        var percent = (int) MathF.Round(entity.Comp.ReductionFraction * 100f);
        args.PushMarkup(Loc.GetString("st-weight-inside-reduction", ("percent", percent)));
    }
}
