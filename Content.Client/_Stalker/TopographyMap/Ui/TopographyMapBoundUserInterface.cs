using System.Security.Cryptography;
using Content.Shared._Stalker.TopographyMap;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;

namespace Content.Client._Stalker.TopographyMap.Ui;

public sealed class TopographyMapBoundUserInterface : BoundUserInterface
{
    [ViewVariables]
    private TopographyMapWindow? _window;

    public TopographyMapBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey)
    {
    }

    protected override void Open()
    {
        base.Open();

        _window = this.CreateWindow<TopographyMapWindow>();
        _window.OpenCenteredRight();

        _window.OnMapButtonPressed += (_, TexturePath) =>
        {
            _window.UpdateBackground(TexturePath.ToString());
        };
    }

    protected override void UpdateState(BoundUserInterfaceState state)
    {
        base.UpdateState(state);

        if (state is not TopographyMapBoundUiState msg) return;
        if (_window is null) return;

        _window.SetSize = msg.Size;

        _window.UpdateBackground(msg.MapTexturePath);
        _window.UpdateMapButtons(msg.TextureNames,msg.TexturePaths);
    }
}