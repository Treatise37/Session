using Content.Client.Eui;
using Content.Shared._Stalker.Private.Scr;
using Content.Shared.Eui;

namespace Content.Client._Stalker.Private.Scr.UI;

public sealed class ScrEui : BaseEui
{
    private readonly ScrUi _window;

    public ScrEui()
    {
        _window = new ScrUi();
    }

    public override void Opened()
    {
        _window.OpenCentered();
    }

    public override void Closed()
    {
        _window.Close();
    }

    public override void HandleState(EuiStateBase state)
    {
        if (state is not ScrEuiState scrState)
            return;

        if (scrState.Image is null)
            return;

        _window.SetImage(scrState.Image);
        _window.SetIsValid(scrState.IsValid);
    }
}
