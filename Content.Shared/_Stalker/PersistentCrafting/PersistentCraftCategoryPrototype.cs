using Robust.Shared.Prototypes;

namespace Content.Shared._Stalker.PersistentCrafting;

[Prototype("persistentCraftCategory")]
public sealed class PersistentCraftCategoryPrototype : IPrototype
{
    [IdDataField]
    public string ID { get; private set; } = default!;

    [DataField("name", required: true)]
    public string Name = string.Empty;

    [DataField("order")]
    public int Order = 99;
}

[Prototype("persistentCraftSubCategory")]
public sealed class PersistentCraftSubCategoryPrototype : IPrototype
{
    [IdDataField]
    public string ID { get; private set; } = default!;

    [DataField("name", required: true)]
    public string Name = string.Empty;

    [DataField("order")]
    public int Order = 99;
}
