using System;
using System.Collections.Generic;
using Content.Server._Stalker.Dizzy;
using Content.Shared._Stalker.Mood;
using Robust.Shared.Timing;

namespace Content.Server._Stalker.Mood;

public sealed class STMoodDizzySystem : EntitySystem
{
    [Dependency] private readonly DizzySystem _dizzy = default!;
    [Dependency] private readonly IGameTiming _timing = default!;

    private readonly Dictionary<EntityUid, TimeSpan> _nextApply = new();

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
                    ApplyPeriodicDizziness(uid, now, 1.4f, 1.0f);
                    break;

                case STMoodState.Agony:
                    ApplyPeriodicDizziness(uid, now, 2.2f, 0.8f);
                    break;

                default:
                    ClearDizziness(uid);
                    break;
            }
        }
    }

    private void ApplyPeriodicDizziness(EntityUid uid, TimeSpan now, float dizzyPower, float intervalSeconds)
    {
        if (_nextApply.TryGetValue(uid, out var nextTime) && now < nextTime)
            return;

        _dizzy.TryApplyDizziness(uid, dizzyPower);
        _nextApply[uid] = now + TimeSpan.FromSeconds(intervalSeconds);
    }

    private void ClearDizziness(EntityUid uid)
    {
        if (_nextApply.Remove(uid))
            _dizzy.TryRemoveDizziness(uid);
    }
}