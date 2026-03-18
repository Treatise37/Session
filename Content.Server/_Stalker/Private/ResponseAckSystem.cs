using Robust.Server.Player;
using Robust.Shared.Network;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Content.Server._Stalker.Private;

/// <summary>
/// Monitors server-to-client message flows by registering expected responses per player, checking deadlines once per second,
/// and disconnecting anyone who fails to acknowledge within the allotted timeout so critical requests never go unanswered.
/// </summary>
public sealed class ResponseAckSystem : EntitySystem
{
    private static readonly TimeSpan UpdateTimeInterval = TimeSpan.FromSeconds(1);
    private static readonly TimeSpan DefaultTimeout = TimeSpan.FromSeconds(10);

    [Dependency] private readonly IGameTiming _timing = null!;
    [Dependency] private readonly IPlayerManager _player = null!;

    private readonly Dictionary<NetUserId, Dictionary<Type, TimeSpan>> _pendingAcks = new();
    private ISawmill _logger = null!;
    private TimeSpan _lastUpdateTime = TimeSpan.Zero;

    public override void Initialize()
    {
        base.Initialize();

        _logger = Logger.GetSawmill("scr.server");
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        if (_timing.CurTime < _lastUpdateTime.Add(UpdateTimeInterval))
            return;
        _lastUpdateTime = _timing.CurTime;

        ProcessPending();
    }

    public void ExpectAck(NetUserId userId, Type responseType, TimeSpan? timeout = null)
    {
        timeout ??= DefaultTimeout;
        _logger.Debug($"Expecting user {userId} to send {responseType}.");
        _pendingAcks.GetOrNew(userId)[responseType] = _timing.CurTime + timeout.Value;
    }

    public void Acknowledge(NetUserId userId, Type responseType)
    {
        if (!_pendingAcks.TryGetValue(userId, out var acks))
            return;

        _logger.Debug($"Acknowledged user {userId} to send {responseType}.");
        acks.Remove(responseType);
        if (acks.Count == 0)
            _pendingAcks.Remove(userId);
    }

    private void ProcessPending()
    {
        var now = _timing.CurTime;
        var queueDeletion = new List<(NetUserId, Type)>();
        foreach (var (userId, acks) in _pendingAcks)
        {
            foreach (var (evType, timeout) in acks)
            {
                if (now < timeout)
                    continue;

                _logger.Error($"User {userId} didn't acknowledged an event at time. Event Type: {evType}. Kicking...");
                KickPlayer(userId);
                queueDeletion.Add((userId, evType));
                break;
            }
        }

        foreach (var (userId, type) in queueDeletion)
        {
            if (!_pendingAcks.TryGetValue(userId, out var acks))
                continue;

            acks.Remove(type);
            if (acks.Count == 0)
                _pendingAcks.Remove(userId);
        }
    }

    private void KickPlayer(NetUserId userId)
    {
        if (!_player.TryGetSessionById(userId, out var session))
            return;

        session.Channel.Disconnect("Failed to acknowledge server's message in time.");
    }
}
