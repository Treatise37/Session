using Content.Server.EUI;
using Content.Shared._Stalker.Private.Scr;

namespace Content.Server._Stalker.Private.Scr.UI;

public sealed class ScrEui : BaseEui
{
    private readonly byte[] _image;
    private readonly bool _isValid;

    public ScrEui(byte[] image, bool isValid)
    {
        _image = image;
        _isValid = isValid;
    }

    public override ScrEuiState GetNewState()
    {
        return new ScrEuiState(_image, _isValid);
    }
}
