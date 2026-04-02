using Content.Shared._Stalker.Weight;
using Content.Shared._Stalker_EN.Weight.Systems;
using Content.Shared.EntityEffects;
using Content.Shared.EntityEffects.Effects.StatusEffects;
using Robust.Shared.Prototypes;

namespace Content.Shared._Stalker_EN.EntityEffects.Effects;

/// <summary>
/// Entity effect that applies temporary weight reduction through status effects.
/// </summary>
public sealed partial class STWeightModifierEntityEffectSystem : EntityEffectSystem<STWeightComponent, STWeightModifier>
{
    [Dependency] private readonly STWeightModStatusSystem _weightModStatus = default!;

    protected override void Effect(Entity<STWeightComponent> entity, ref EntityEffectEvent<STWeightModifier> args)
    {
        var effect = args.Effect;
        var time = effect.Time ?? TimeSpan.FromSeconds(2);

        _weightModStatus.TryUpdateWeightModDuration(
            entity.Owner,
            effect.EffectProto,
            time * args.Scale,
            effect.WeightModifier);
    }
}

public sealed partial class STWeightModifier : BaseStatusEntityEffect<STWeightModifier>
{
    [DataField(required: true)]
    public float WeightModifier = 0f;

    [DataField]
    public EntProtoId EffectProto = "ReagentWeight";

    public override string? EntityEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
        => Time == null
            ? null
            : Loc.GetString("entity-effect-guidebook-st-weight-modifier",
                ("chance", Probability),
                ("modifier", WeightModifier),
                ("time", Time.Value.TotalSeconds));
}