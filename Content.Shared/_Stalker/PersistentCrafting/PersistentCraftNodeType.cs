using Robust.Shared.Serialization;

namespace Content.Shared._Stalker.PersistentCrafting;

[Serializable, NetSerializable]
public enum PersistentCraftNodeType : byte
{
    RecipeUnlock = 0,
}
