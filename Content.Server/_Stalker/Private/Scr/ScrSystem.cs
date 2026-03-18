using Content.Server._Stalker.Private.Scr.UI;
using Content.Server.Chat.Managers;
using Content.Server.EUI;
using Content.Shared._Stalker.Private.Scr;
using Robust.Server.Player;
using Robust.Shared.Network;
using Robust.Shared.Player;

namespace Content.Server._Stalker.Private.Scr;

// ReSharper disable once ClassNeverInstantiated.Global
public sealed partial class ScrSystem : EntitySystem
{
    [Dependency] private readonly IServerNetManager _netManager = null!;
    [Dependency] private readonly EuiManager _eui = null!;
    [Dependency] private readonly IPlayerManager _playerManager = null!;
    [Dependency] private readonly ResponseAckSystem _ack = null!;

    private ISawmill _logger = null!;

    // target -> requestBy
    private readonly Dictionary<NetUserId, NetUserId> _requests = new();

    public override void Initialize()
    {
        _netManager.RegisterNetMessage<ScrMessage>(OnScrReply);
        _logger = Logger.GetSawmill("scr");
    }

    private async void OnScrReply(ScrMessage message)
    {
        var targetId = message.MsgChannel.UserId;
        var isValid = true;
        var (userComment, software) = _ReadExifTags(message.Data);

        if (userComment != $"{message.MsgChannel.UserId}" || software != "StalkerSS14")
        {
            isValid = false;
            _logger.Warning($"Player {message.MsgChannel.UserId} sent an invalid EXIF tag. UserComment: {userComment}, Software: {software}");
        }

        if (_requests.TryGetValue(targetId, out var requestById) && _playerManager.TryGetSessionById(requestById, out var requestBy))
        {
            var eui = new ScrEui(message.Data, isValid);
            _eui.OpenEui(eui, requestBy);
            _eui.QueueStateUpdate(eui);
        }

        _requests.Remove(targetId);
        _ack.Acknowledge(targetId, message.GetType());
    }

    public bool RequestScr(ICommonSession session, bool isClyde, ICommonSession? requestBy = null)
    {
        var msg = new ScrMessage
        {
            IsClyde = isClyde,
        };
        _netManager.ServerSendMessage(msg, session.Channel);

        if (requestBy is not null &&
            !_requests.TryAdd(session.UserId, requestBy.UserId))
        {
            return false;
        }

        _ack.ExpectAck(session.UserId, msg.GetType(), TimeSpan.FromSeconds(2));
        return true;
    }
}
