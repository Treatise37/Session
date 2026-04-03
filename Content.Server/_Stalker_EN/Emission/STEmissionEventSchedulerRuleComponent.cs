using Content.Shared.Destructible.Thresholds;
using Robust.Shared.Prototypes;

namespace Content.Server._Stalker_EN.Emission;

[RegisterComponent]
public sealed partial class STEmissionEventSchedulerRuleComponent : Component
{
    [DataField]
    public float MinimumTimeUntilFirstEvent = 7200f;

    [DataField]
    public MinMax MinMaxEventTiming = new(7200, 14400);

    [DataField(required: true)]
    public EntProtoId ScheduledGameRule = "STEmissionEvent";

    [DataField]
    public float TimeUntilNextEvent;

    public bool Initialized;
}