using Content.Shared._Stalker.PersistentCrafting;

namespace Content.Server._Stalker.PersistentCrafting;

public sealed class PersistentCraftBranchProfile
{
    public int AvailablePoints;
    public int SpentPoints;
    public int Level = PersistentCraftingHelper.InitialLevel;
    public int SubLevel = PersistentCraftingHelper.MainTierSubLevel;
    public int Experience;
    public Dictionary<string, PersistentCraftNodeProfile> NodeProgress = new();
}

public sealed class PersistentCraftNodeProfile
{
    public int MasteryLevel = PersistentCraftingHelper.InitialNodeMasteryLevel;
    public int Experience;
}
