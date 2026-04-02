using Content.Server.GameTicking;
using Content.Server.GameTicking.Rules;
using Content.Shared.GameTicking.Components;
using Robust.Shared.Random;

namespace Content.Server._Stalker_EN.Emission;

public sealed class STEmissionEventSchedulerRuleSystem : GameRuleSystem<STEmissionEventSchedulerRuleComponent>
{
    [Dependency] private readonly IRobustRandom _random = default!;

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<STEmissionEventSchedulerRuleComponent, GameRuleComponent>();
        while (query.MoveNext(out var uid, out var scheduler, out var gameRule))
        {
            if (!GameTicker.IsGameRuleActive(uid, gameRule))
                continue;

            if (!scheduler.Initialized)
            {
                scheduler.TimeUntilNextEvent = scheduler.MinimumTimeUntilFirstEvent;
                scheduler.Initialized = true;
                continue;
            }

            if (scheduler.TimeUntilNextEvent > 0)
            {
                scheduler.TimeUntilNextEvent -= frameTime;
                continue;
            }

            if (!GameTicker.IsGameRuleActive(scheduler.ScheduledGameRule))
            {
                GameTicker.StartGameRule(scheduler.ScheduledGameRule, out _);
            }

            ResetTimer(scheduler);
        }
    }

    private void ResetTimer(STEmissionEventSchedulerRuleComponent component)
    {
        component.TimeUntilNextEvent = component.MinMaxEventTiming.Next(_random);
    }
}