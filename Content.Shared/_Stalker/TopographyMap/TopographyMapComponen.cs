using System.Numerics;

namespace Content.Shared._Stalker.TopographyMap;

[RegisterComponent]
public sealed partial class TopographyMapComponent : Component
{
    [DataField(required: true)]
    public string MapTexturePath = "";
    [DataField(required: true)]
    public string MapTextureName = "";

    [DataField]
    public LocId OpenMapText = "topography-open-map";

    [DataField]
    public Vector2 Size = new Vector2(790, 790);
    [DataField]
    public List<String> TextureNames = new List<String>();
    [DataField]
    public List<String> TexturePaths = new List<String>();
}

