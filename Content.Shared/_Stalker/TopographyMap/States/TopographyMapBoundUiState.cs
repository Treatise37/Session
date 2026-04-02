using System.Numerics;
using Robust.Shared.Serialization;

namespace Content.Shared._Stalker.TopographyMap;


[Serializable, NetSerializable]
public sealed class TopographyMapBoundUiState : BoundUserInterfaceState
{
    public Vector2 Size = new Vector2(790, 790);

    public string MapTexturePath = "";

    public List<String> TextureNames = new List<String>();
    public List<String> TexturePaths = new List<String>();
}
