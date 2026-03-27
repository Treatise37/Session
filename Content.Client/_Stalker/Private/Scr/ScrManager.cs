using System.IO;
using System.Threading.Tasks;
using Content.Client.Viewport;
using Content.Shared._Stalker.Private.Scr;
using Robust.Client.Graphics;
using Robust.Client.Player;
using Robust.Client.State;
using Robust.Shared.Network;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace Content.Client._Stalker.Private.Scr;

public sealed partial class ScrManager
{
    [Dependency] private readonly IClientNetManager _netManager = null!;
    [Dependency] private readonly IPlayerManager _playerManager = null!;
    [Dependency] private readonly IStateManager _stateManager = null!;
    private IClyde _clyde = null!;

    public void Initialize()
    {
        _clyde = IoCManager.Resolve<IClyde>();
        _netManager.RegisterNetMessage<ScrMessage>(OnScrMessage);
    }

    // ReSharper disable once AsyncVoidMethod
    private async void OnScrMessage(ScrMessage message)
    {
        switch (message.IsClyde)
        {
            case true:
                await _UseClyde();
                break;
            case false:
                _UseViewport();
                break;
        }
    }

    private async Task _UseClyde()
    {
        var image = await _clyde.ScreenshotAsync(ScreenshotType.Final);
        var array = await _ToBA(image);
        array = _FillExif(array, _playerManager.LocalUser);
        if (array.Length > 1_500_000)
            return;

        var msg = new ScrMessage
        {
            IsClyde = true,
            Data = array,
        };
        _netManager.ClientSendMessage(msg);
    }

    private void _UseViewport()
    {
        if (_stateManager.CurrentState is not IMainViewportState state)
            return;
        // ReSharper disable once AsyncVoidMethod
        state.Viewport.Viewport.Screenshot(async void (image) =>
        {
            var data = await _ToBA(image);
            data = _FillExif(data, _playerManager.LocalUser);
            var msg = new ScrMessage
            {
                IsClyde = false,
                Data = data,
            };
            _netManager.ClientSendMessage(msg);
        });
    }

    private async Task<byte[]> _ToBA<T>(Image<T> image) where T : unmanaged, IPixel<T>
    {
        using var stream = new MemoryStream();
        await image.SaveAsJpegAsync(stream);
        return stream.ToArray();
    }
}
