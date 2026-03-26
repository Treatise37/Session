using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared._Stalker.PersistentCrafting;

[Prototype("persistentCraftRecipe"), Serializable, NetSerializable]
public sealed partial class PersistentCraftRecipePrototype : IPrototype
{
    [ViewVariables]
    [IdDataField]
    public string ID { get; private set; } = default!;

    [DataField("name", required: true)]
    public string Name = string.Empty;

    [DataField("description", required: true)]
    public string Description = string.Empty;

    [DataField("displayProto")]
    public string? DisplayProto;

    [DataField("branch", required: true)]
    public PersistentCraftBranch Branch;

    [DataField("tier", required: true)]
    public int Tier = 1;

    [DataField("requiredNode", required: true)]
    public string RequiredNode = string.Empty;

    [DataField("category")]
    public string? Category;

    [DataField("subCategory")]
    public string? SubCategory;

    [DataField("craftTime")]
    public float CraftTime = 2f;

    [DataField("pointReward")]
    public int PointReward;

    [DataField("ingredients", required: true)]
    public List<PersistentCraftIngredient> Ingredients = new();

    [DataField("results", required: true)]
    public List<PersistentCraftResult> Results = new();
}

[DataDefinition, Serializable, NetSerializable]
public sealed partial class PersistentCraftIngredient
{
    [DataField("proto")]
    public string? Proto;

    [DataField("tag")]
    public string? Tag;

    [DataField("amount")]
    public int Amount = 1;
}

[DataDefinition, Serializable, NetSerializable]
public sealed partial class PersistentCraftResult
{
    [DataField("proto", required: true)]
    public string Proto = string.Empty;

    [DataField("amount")]
    public int Amount = 1;
}
