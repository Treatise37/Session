using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared._Stalker.PersistentCrafting;

[Prototype("persistentCraftNode"), Serializable, NetSerializable]
public sealed partial class PersistentCraftNodePrototype : IPrototype
{
    [ViewVariables]
    [IdDataField]
    public string ID { get; private set; } = default!;

    [DataField("name")]
    public string Name = string.Empty;

    [DataField("description")]
    public string Description = string.Empty;

    [DataField("branch", required: true)]
    public PersistentCraftBranch Branch;

    [DataField("nodeType")]
    public PersistentCraftNodeType NodeType = PersistentCraftNodeType.RecipeUnlock;

    [DataField("tier", required: true)]
    public int Tier = 1;

    [DataField("subTier")]
    public int SubTier;

    [DataField("cost")]
    public int Cost = 1;

    [DataField("requiredBranchLevel")]
    public int RequiredBranchLevel;

    [DataField("affectedTier")]
    public int AffectedTier;

    [DataField("materialCostReductionPercent")]
    public int MaterialCostReductionPercent;

    [DataField("craftTimeReductionPercent")]
    public int CraftTimeReductionPercent;

    [DataField("displayLabel")]
    public string? DisplayLabel;

    [DataField("displayProto")]
    public string? DisplayProto;

    [DataField("treeColumn")]
    public int TreeColumn = -1;

    [DataField("treeRow")]
    public int TreeRow = -1;

    [DataField("prerequisites")]
    public List<string> Prerequisites = new();
}
