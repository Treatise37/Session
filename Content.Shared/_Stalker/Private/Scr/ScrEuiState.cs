using Content.Shared.Eui;
using Robust.Shared.Serialization;

namespace Content.Shared._Stalker.Private.Scr;

[Serializable, NetSerializable]
public sealed class ScrEuiState : EuiStateBase
{
    public byte[]? Image;
    public bool IsValid;

    public ScrEuiState(byte[] image,  bool isValid)
    {
        Image = image;
        IsValid = isValid;
    }
}
