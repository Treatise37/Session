using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared._Stalker.PersistentCrafting;

[Prototype("persistentCraftBranch"), Serializable, NetSerializable]
public sealed partial class PersistentCraftBranchPrototype : IPrototype
{
    [IdDataField]
    public string ID { get; private set; } = default!;

    [DataField("branch", required: true)]
    public PersistentCraftBranch Branch;

    [DataField("maxLevel")]
    public int MaxLevel = 5;
}
