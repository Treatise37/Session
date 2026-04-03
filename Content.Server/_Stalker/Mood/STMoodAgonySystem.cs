using System;
using System.Collections.Generic;
using Content.Server.Chat.Systems;
using Content.Shared._Stalker.Mood;
using Content.Shared._Stalker.Speech;
using Content.Shared.Medical;
using Content.Shared.Stunnable;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Server._Stalker.Mood;

public sealed class STMoodAgonySystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly IPrototypeManager _proto = default!;
    [Dependency] private readonly ChatSystem _chat = default!;
    [Dependency] private readonly VomitSystem _vomit = default!;
    [Dependency] private readonly SharedStunSystem _stun = default!;

    private readonly Dictionary<EntityUid, TimeSpan> _nextScream = new();
    private readonly Dictionary<EntityUid, TimeSpan> _nextVomit = new();
    private readonly Dictionary<EntityUid, TimeSpan> _nextKnockdown = new();

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var now = _timing.CurTime;
        var query = EntityQueryEnumerator<STMoodComponent>();

        while (query.MoveNext(out var uid, out var mood))
        {
            if (mood.State != STMoodState.Agony)
            {
                ClearTimers(uid);
                continue;
            }

            EnsureTimers(uid, now);

            if (_nextScream.TryGetValue(uid, out var screamTime) && now >= screamTime)
            {
                TriggerAgonyScream(uid);
                _nextScream[uid] = now + TimeSpan.FromSeconds(_random.NextFloat(6f, 10f));
            }

            if (_nextVomit.TryGetValue(uid, out var vomitTime) && now >= vomitTime)
            {
                TriggerAgonyVomit(uid);
                _nextVomit[uid] = now + TimeSpan.FromSeconds(_random.NextFloat(12f, 20f));
            }

            if (_nextKnockdown.TryGetValue(uid, out var knockdownTime) && now >= knockdownTime)
            {
                TriggerAgonyKnockdown(uid);
                _nextKnockdown[uid] = now + TimeSpan.FromSeconds(_random.NextFloat(8f, 14f));
            }
        }
    }

    private void EnsureTimers(EntityUid uid, TimeSpan now)
    {
        if (!_nextScream.ContainsKey(uid))
            _nextScream[uid] = now + TimeSpan.FromSeconds(_random.NextFloat(1.5f, 4f));

        if (!_nextVomit.ContainsKey(uid))
            _nextVomit[uid] = now + TimeSpan.FromSeconds(_random.NextFloat(5f, 9f));

        if (!_nextKnockdown.ContainsKey(uid))
            _nextKnockdown[uid] = now + TimeSpan.FromSeconds(_random.NextFloat(3f, 6f));
    }

    private void ClearTimers(EntityUid uid)
    {
        _nextScream.Remove(uid);
        _nextVomit.Remove(uid);
        _nextKnockdown.Remove(uid);
    }

    private void TriggerAgonyScream(EntityUid uid)
    {
        if (!TryComp<STVocalComponent>(uid, out var vocal))
            return;

        if (vocal.EmoteSounds is not { } sounds)
            return;

        _chat.TryPlayEmoteSound(uid, _proto.Index(sounds), "STScream");
    }

    private void TriggerAgonyVomit(EntityUid uid)
    {
        _vomit.Vomit(uid, force: true);
    }

    private void TriggerAgonyKnockdown(EntityUid uid)
    {
        _stun.TryKnockdown(
            uid,
            TimeSpan.FromSeconds(_random.NextFloat(2f, 4f)),
            refresh: true,
            autoStand: true,
            drop: false,
            force: true);
    }
}