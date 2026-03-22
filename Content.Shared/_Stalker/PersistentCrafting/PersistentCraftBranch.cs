using Robust.Shared.Serialization;

namespace Content.Shared._Stalker.PersistentCrafting;

[Serializable, NetSerializable]
public enum PersistentCraftBranch : byte
{
    Weapon,
    Armor,
    Anomaly,
}
