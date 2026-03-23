using Robust.Shared.Serialization;

namespace Content.Shared._Stalker.PersistentCrafting;

[Serializable, NetSerializable]
public sealed class PersistentCraftBranchState
{
    public PersistentCraftBranch Branch;
    public int MaxLevel;
    public int AvailablePoints;
    public int SpentPoints;
    public int Level;
    public int SubLevel;
    public int Experience;
    public int ExperienceForNextLevel;

    public PersistentCraftBranchState(
        PersistentCraftBranch branch,
        int maxLevel,
        int availablePoints,
        int spentPoints,
        int level,
        int subLevel,
        int experience,
        int experienceForNextLevel)
    {
        Branch = branch;
        MaxLevel = maxLevel;
        AvailablePoints = availablePoints;
        SpentPoints = spentPoints;
        Level = level;
        SubLevel = subLevel;
        Experience = experience;
        ExperienceForNextLevel = experienceForNextLevel;
    }
}

[Serializable, NetSerializable]
public sealed class PersistentCraftTierState
{
    public PersistentCraftBranch Branch;
    public int Tier;
    public int ProgressLevel;
    public int MaxProgressLevel;
    public int AvailablePoints;
    public int SpentPoints;
    public int Experience;
    public int ExperienceForNextLevel;

    public PersistentCraftTierState(
        PersistentCraftBranch branch,
        int tier,
        int progressLevel,
        int maxProgressLevel,
        int availablePoints,
        int spentPoints,
        int experience,
        int experienceForNextLevel)
    {
        Branch = branch;
        Tier = tier;
        ProgressLevel = progressLevel;
        MaxProgressLevel = maxProgressLevel;
        AvailablePoints = availablePoints;
        SpentPoints = spentPoints;
        Experience = experience;
        ExperienceForNextLevel = experienceForNextLevel;
    }
}

[Serializable, NetSerializable]
public sealed class PersistentCraftState
{
    public bool Loaded;
    public List<PersistentCraftBranchState> BranchStates;
    public List<PersistentCraftTierState> TierStates;
    public List<string> UnlockedNodes;

    public PersistentCraftState(
        bool loaded,
        List<PersistentCraftBranchState> branchStates,
        List<PersistentCraftTierState> tierStates,
        List<string> unlockedNodes)
    {
        Loaded = loaded;
        BranchStates = branchStates;
        TierStates = tierStates;
        UnlockedNodes = unlockedNodes;
    }
}

[Serializable, NetSerializable]
public sealed class RequestPersistentCraftStateEvent : EntityEventArgs
{
}

[Serializable, NetSerializable]
public sealed class PersistentCraftStateEvent : EntityEventArgs
{
    public PersistentCraftState State { get; }

    public PersistentCraftStateEvent(PersistentCraftState state)
    {
        State = state;
    }
}

[Serializable, NetSerializable]
public sealed class RequestPersistentCraftUnlockEvent : EntityEventArgs
{
    public string NodeId { get; }

    public RequestPersistentCraftUnlockEvent(string nodeId)
    {
        NodeId = nodeId;
    }
}
