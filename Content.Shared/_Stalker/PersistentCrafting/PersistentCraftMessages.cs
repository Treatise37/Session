using Robust.Shared.Serialization;

namespace Content.Shared._Stalker.PersistentCrafting;

[Serializable, NetSerializable]
public sealed class PersistentCraftBranchState
{
    public PersistentCraftBranch Branch;
    public int AvailablePoints;
    public int SpentPoints;

    public PersistentCraftBranchState(
        PersistentCraftBranch branch,
        int availablePoints,
        int spentPoints)
    {
        Branch = branch;
        AvailablePoints = availablePoints;
        SpentPoints = spentPoints;
    }
}

[Serializable, NetSerializable]
public sealed class PersistentCraftState
{
    public bool Loaded;
    public List<PersistentCraftBranchState> BranchStates;
    public List<string> UnlockedNodes;

    public PersistentCraftState(
        bool loaded,
        List<PersistentCraftBranchState> branchStates,
        List<string> unlockedNodes)
    {
        Loaded = loaded;
        BranchStates = branchStates;
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
