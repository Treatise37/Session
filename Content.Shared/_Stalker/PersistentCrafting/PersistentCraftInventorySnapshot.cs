using System.Collections.Generic;
using Content.Shared.Tag;
using Robust.Shared.GameObjects;
using Robust.Shared.Prototypes;

namespace Content.Shared._Stalker.PersistentCrafting;

public sealed class PersistentCraftInventorySnapshot
{
    private readonly Dictionary<string, int> _amountByProto;
    private readonly Dictionary<string, int> _nonStackAmountByProto;
    private readonly Dictionary<string, int> _amountByStackType;
    private readonly Dictionary<string, int> _amountByTag;
    private readonly Dictionary<string, string?> _ingredientStackTypeByProto;

    public static readonly PersistentCraftInventorySnapshot Empty = new(
        string.Empty,
        new Dictionary<string, int>(),
        new Dictionary<string, int>(),
        new Dictionary<string, int>(),
        new Dictionary<string, int>(),
        new Dictionary<string, string?>());

    public string Signature { get; }

    internal PersistentCraftInventorySnapshot(
        string signature,
        Dictionary<string, int> amountByProto,
        Dictionary<string, int> nonStackAmountByProto,
        Dictionary<string, int> amountByStackType,
        Dictionary<string, int> amountByTag,
        Dictionary<string, string?> ingredientStackTypeByProto)
    {
        Signature = signature;
        _amountByProto = amountByProto;
        _nonStackAmountByProto = nonStackAmountByProto;
        _amountByStackType = amountByStackType;
        _amountByTag = amountByTag;
        _ingredientStackTypeByProto = ingredientStackTypeByProto;
    }

    public static PersistentCraftInventorySnapshot Build(
        IEntityManager entityManager,
        IPrototypeManager prototypeManager,
        TagSystem tagSystem,
        EntityUid root,
        IReadOnlyList<PersistentCraftIngredient> trackedIngredients)
    {
        var builder = new PersistentCraftInventorySnapshotBuilder(entityManager, prototypeManager, tagSystem);
        return builder.Build(root, trackedIngredients);
    }

    public int GetAmount(PersistentCraftIngredient ingredient)
    {
        if (!string.IsNullOrWhiteSpace(ingredient.Proto))
        {
            if (_ingredientStackTypeByProto.TryGetValue(ingredient.Proto, out var stackType) &&
                !string.IsNullOrWhiteSpace(stackType))
            {
                var stackAmount = GetAmountByKey(_amountByStackType, stackType);
                var nonStackExactAmount = GetAmountByKey(_nonStackAmountByProto, ingredient.Proto);
                return stackAmount + nonStackExactAmount;
            }

            return GetAmountByKey(_amountByProto, ingredient.Proto);
        }

        if (!string.IsNullOrWhiteSpace(ingredient.Tag))
            return GetAmountByKey(_amountByTag, ingredient.Tag);

        return 0;
    }

    private static int GetAmountByKey(Dictionary<string, int> dictionary, string key)
    {
        return dictionary.TryGetValue(key, out var amount) ? amount : 0;
    }
}
