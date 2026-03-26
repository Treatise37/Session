using Content.Shared._Stalker.PersistentCrafting;

namespace Content.Server._Stalker.PersistentCrafting;

public sealed class PersistentCraftBranchProfile
{
    public int AvailablePoints;
    public int SpentPoints;
    public int Level = PersistentCraftingHelper.InitialLevel;
    public int SubLevel = PersistentCraftingHelper.DefaultSubLevel;
    public int Experience;
    public Dictionary<int, PersistentCraftTierProfile> TierProgress = new();
}

public sealed class PersistentCraftTierProfile
{
    public int ProgressLevel = PersistentCraftingHelper.InitialTierProgressLevel;
    public int Experience;
}
