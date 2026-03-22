using Robust.Shared.Serialization;

namespace Content.Shared._Stalker.PersistentCrafting;

[Serializable, NetSerializable]
public enum PersistentCraftNodeType : byte
{
    MainTier = 0,
    RecipeUnlock,
    MaterialEfficiency,
    CraftSpeed,
}
