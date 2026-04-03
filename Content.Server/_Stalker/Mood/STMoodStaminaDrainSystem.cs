using System;
using System.Collections.Generic;
using Content.Server.Damage.Systems;
using Content.Shared._Stalker.Mood;
using Robust.Shared.Timing;

namespace Content.Server._Stalker.Mood;

public sealed class STMoodStaminaDrainSystem : EntitySystem
{
    [Dependency] private readonly StaminaSystem _stamina = default!;
    [Dependency] private readonly IGameTiming _timing = default!;

    private readonly Dictionary<EntityUid, TimeSpan> _nextDrain = new();

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var now = _timing.CurTime;
        var query = EntityQueryEnumerator<STMoodComponent>();

        while (query.MoveNext(out var uid, out var mood))
        {
            switch (mood.State)
            {
                case STMoodState.Pain:
                    ApplyPeriodicDrain(uid, now, 0.8f, 1.2f);
                    break;

                case STMoodState.Agony:
                    ApplyPeriodicDrain(uid, now, 1.8f, 0.9f);
                    break;

                default:
                    _nextDrain.Remove(uid);
                    break;
            }
        }
    }

    private void ApplyPeriodicDrain(EntityUid uid, TimeSpan now, float staminaDamage, float intervalSeconds)
    {
        if (_nextDrain.TryGetValue(uid, out var nextTime) && now < nextTime)
            return;

        _stamina.TakeStaminaDamage(
            uid,
            staminaDamage,
            visual: false,
            shouldLog: false);

        _nextDrain[uid] = now + TimeSpan.FromSeconds(intervalSeconds);
    }
}