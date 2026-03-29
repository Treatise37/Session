using System.Collections.Generic;
using Content.Shared.Tag;
using Robust.Shared.GameObjects;

namespace Content.Shared._Stalker.PersistentCrafting;

public sealed class PersistentCraftInventorySnapshot
{
    private readonly Dictionary<string, int> _amountByProto;
    private readonly Dictionary<string, int> _amountByStackType;
    private readonly Dictionary<string, int> _amountByTag;

    public static readonly PersistentCraftInventorySnapshot Empty = new(
        string.Empty,
        new Dictionary<string, int>(),
        new Dictionary<string, int>(),
        new Dictionary<string, int>());

    public string Signature { get; }

    internal PersistentCraftInventorySnapshot(
        string signature,
        Dictionary<string, int> amountByProto,
        Dictionary<string, int> amountByStackType,
        Dictionary<string, int> amountByTag)
    {
        Signature = signature;
        _amountByProto = amountByProto;
        _amountByStackType = amountByStackType;
        _amountByTag = amountByTag;
    }

    public static PersistentCraftInventorySnapshot Build(
        IEntityManager entityManager,
        TagSystem tagSystem,
        EntityUid root,
        IReadOnlyList<PersistentCraftIngredient> trackedIngredients)
    {
        var builder = new PersistentCraftInventorySnapshotBuilder(entityManager, tagSystem);
        return builder.Build(root, trackedIngredients);
    }

    public int GetAmount(PersistentCraftIngredient ingredient)
    {
        return ingredient.GetSelectorKind() switch
        {
            PersistentCraftIngredientSelectorKind.Proto => GetAmountByKey(_amountByProto, ingredient.Proto!),
            PersistentCraftIngredientSelectorKind.StackType => GetAmountByKey(_amountByStackType, ingredient.StackType!),
            PersistentCraftIngredientSelectorKind.Tag => GetAmountByKey(_amountByTag, ingredient.Tag!),
            _ => 0,
        };
    }

    private static int GetAmountByKey(Dictionary<string, int> dictionary, string key)
    {
        return dictionary.TryGetValue(key, out var amount) ? amount : 0;
    }
}
