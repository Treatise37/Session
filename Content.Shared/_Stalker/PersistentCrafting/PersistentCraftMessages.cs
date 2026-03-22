using Robust.Shared.Serialization;

namespace Content.Shared._Stalker.PersistentCrafting;

[Serializable, NetSerializable]
public sealed class PersistentCraftBranchState
{
    public PersistentCraftBranch Branch;
    public int AvailablePoints;
    public int SpentPoints;
    public int Level;
    public int SubLevel;
    public int Experience;
    public int ExperienceForNextLevel;

    public PersistentCraftBranchState(
        PersistentCraftBranch branch,
        int availablePoints,
        int spentPoints,
        int level,
        int subLevel,
        int experience,
        int experienceForNextLevel)
    {
        Branch = branch;
        AvailablePoints = availablePoints;
        SpentPoints = spentPoints;
        Level = level;
        SubLevel = subLevel;
        Experience = experience;
        ExperienceForNextLevel = experienceForNextLevel;
    }
}

[Serializable, NetSerializable]
public sealed class PersistentCraftNodeState
{
    public string NodeId;
    public int MasteryLevel;
    public int Experience;
    public int ExperienceForNextLevel;

    public PersistentCraftNodeState(
        string nodeId,
        int masteryLevel,
        int experience,
        int experienceForNextLevel)
    {
        NodeId = nodeId;
        MasteryLevel = masteryLevel;
        Experience = experience;
        ExperienceForNextLevel = experienceForNextLevel;
    }
}

[Serializable, NetSerializable]
public sealed class PersistentCraftState
{
    public bool Loaded;
    public List<PersistentCraftBranchState> BranchStates;
    public List<PersistentCraftNodeState> NodeStates;
    public List<string> UnlockedNodes;

    public PersistentCraftState(
        bool loaded,
        List<PersistentCraftBranchState> branchStates,
        List<PersistentCraftNodeState> nodeStates,
        List<string> unlockedNodes)
    {
        Loaded = loaded;
        BranchStates = branchStates;
        NodeStates = nodeStates;
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
