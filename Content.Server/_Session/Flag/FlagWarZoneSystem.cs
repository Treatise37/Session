using Content.Server._Stalker.WarZone;
using Content.Shared._Session.Flag;
using Content.Shared._Stalker.WarZone;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;

namespace Content.Server._Session.Flag;

public sealed class FlagWarZoneSystem : EntitySystem
{
    [Dependency] private readonly IPrototypeManager _proto = default!;
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;

    private const float UpdateInterval = 1f;
    private float _timer;
    private readonly Dictionary<EntityUid, float> _prevProgress = new();

    public override void Update(float frameTime)
    {
        _timer += frameTime;
        if (_timer < UpdateInterval)
            return;
        _timer = 0f;

        var query = EntityQueryEnumerator<WarZoneFlagComponent, WarZoneComponent, AppearanceComponent>();
        while (query.MoveNext(out var uid, out _, out var wz, out var app))
        {
            if (!_proto.TryIndex<STWarZonePrototype>(wz.ZoneProto, out var wzProto) || wzProto.CaptureTime <= 0f)
                continue;

            var progress = wz.CaptureProgress;

            // Detect capture completion: was progressing, now reset to 0
            if (_prevProgress.TryGetValue(uid, out var prev) && prev >= 0.8f && progress <= 0f)
            {
                var audioPath = (string)wz.ZoneProto == "DutyBase"
                    ? "/Audio/_Session/duty.ogg"
                    : "/Audio/_Session/freedom.ogg";
                _audio.PlayGlobal(audioPath, Filter.Broadcast(), true);
            }
            _prevProgress[uid] = progress;

            int level;
            if (progress <= 0f)      level = 3;
            else if (progress >= 0.8f) level = 1;
            else if (progress >= 0.5f) level = 2;
            else                       level = 3;

            _appearance.SetData(uid, WarZoneFlagVisuals.Level, level, app);
        }
    }
}
