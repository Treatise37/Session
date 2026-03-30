using System.Collections.Generic;
using System.Linq;
using System.Text;
using Content.Shared.Stacks;
using Content.Shared.Tag;
using Robust.Shared.GameObjects;

namespace Content.Shared._Stalker.PersistentCrafting;

public sealed class PersistentCraftInventorySnapshotBuilder
{
    private readonly IEntityManager _entityManager;
    private readonly TagSystem _tagSystem;
    private readonly PersistentCraftAccessibleInventoryPolicy _policy;
    private readonly PersistentCraftIngredientMatcher _matcher;

    public PersistentCraftInventorySnapshotBuilder(
        IEntityManager entityManager,
        TagSystem tagSystem,
        PersistentCraftAccessibleInventoryPolicy? policy = null)
    {
        _entityManager = entityManager;
        _tagSystem = tagSystem;
        _policy = policy ?? PersistentCraftAccessibleInventoryPolicy.Default;
        _matcher = new PersistentCraftIngredientMatcher(entityManager, tagSystem);
    }

    public PersistentCraftInventorySnapshot Build(
        EntityUid root,
        IReadOnlyList<PersistentCraftIngredient> trackedIngredients)
    {
        if (!_entityManager.EntityExists(root))
            return PersistentCraftInventorySnapshot.Empty;

        var trackedTags = new HashSet<string>();
        var trackedStackTypes = new HashSet<string>();
        var trackedIngredientPrototypes = new HashSet<string>();

        for (var i = 0; i < trackedIngredients.Count; i++)
        {
            var ingredient = trackedIngredients[i];
            switch (ingredient.GetSelectorKind())
            {
                case PersistentCraftIngredientSelectorKind.Proto:
                    trackedIngredientPrototypes.Add(ingredient.Proto!);
                    break;

                case PersistentCraftIngredientSelectorKind.StackType:
                    trackedStackTypes.Add(ingredient.StackType!);
                    break;

                case PersistentCraftIngredientSelectorKind.Tag:
                    trackedTags.Add(ingredient.Tag!);
                    break;
            }
        }

        var sortedTrackedTags = trackedTags.Count > 1
            ? trackedTags.OrderBy(static tag => tag, System.StringComparer.Ordinal).ToArray()
            : trackedTags.ToArray();

        var amountByProto = new Dictionary<string, int>();
        var amountByStackType = new Dictionary<string, int>();
        var amountByTag = new Dictionary<string, int>();
        var signatureBuilder = new StringBuilder();

        var accessibleEntities = PersistentCraftInventoryHelper.CollectAccessibleEntities(_entityManager, root, _policy);
        accessibleEntities.Sort(static (left, right) => left.Id.CompareTo(right.Id));

        for (var entityIndex = 0; entityIndex < accessibleEntities.Count; entityIndex++)
        {
            var entity = accessibleEntities[entityIndex];
            if (!_entityManager.EntityExists(entity))
                continue;

            var amount = _matcher.GetUsableAmount(entity);
            signatureBuilder.Append(entity.Id);
            signatureBuilder.Append(':');
            signatureBuilder.Append(amount);
            signatureBuilder.Append(':');

            string? prototypeId = null;
            if (_entityManager.TryGetComponent(entity, out MetaDataComponent? meta) &&
                meta.EntityPrototype != null)
            {
                prototypeId = meta.EntityPrototype.ID;
                if (trackedIngredientPrototypes.Contains(prototypeId))
                    AddAmount(amountByProto, prototypeId, amount);
            }

            signatureBuilder.Append(prototypeId ?? string.Empty);
            signatureBuilder.Append(':');

            string? stackTypeId = null;
            if (_entityManager.TryGetComponent(entity, out StackComponent? stack))
            {
                stackTypeId = stack.StackTypeId;
                if (trackedStackTypes.Contains(stack.StackTypeId))
                    AddAmount(amountByStackType, stack.StackTypeId, amount);
            }

            signatureBuilder.Append(stackTypeId ?? string.Empty);

            for (var tagIndex = 0; tagIndex < sortedTrackedTags.Length; tagIndex++)
            {
                var tag = sortedTrackedTags[tagIndex];
                if (!_tagSystem.HasTag(entity, tag))
                    continue;

                AddAmount(amountByTag, tag, amount);
                signatureBuilder.Append('#');
                signatureBuilder.Append(tag);
            }

            signatureBuilder.Append(';');
        }

        return new PersistentCraftInventorySnapshot(
            signatureBuilder.ToString(),
            amountByProto,
            amountByStackType,
            amountByTag);
    }

    private static void AddAmount(Dictionary<string, int> dictionary, string key, int amount)
    {
        if (dictionary.TryGetValue(key, out var existing))
            dictionary[key] = existing + amount;
        else
            dictionary[key] = amount;
    }
}
