using System.Collections.Generic;
using System.Text;
using Content.Shared.Stacks;
using Content.Shared.Tag;
using Robust.Shared.Containers;
using Robust.Shared.GameObjects;
using Robust.Shared.Prototypes;

namespace Content.Shared._Stalker.PersistentCrafting;

public sealed class PersistentCraftInventorySnapshotBuilder
{
    private readonly IEntityManager _entityManager;
    private readonly TagSystem _tagSystem;
    private readonly PersistentCraftAccessibleInventoryPolicy _policy;
    private readonly PersistentCraftIngredientMatcher _matcher;

    public PersistentCraftInventorySnapshotBuilder(
        IEntityManager entityManager,
        IPrototypeManager prototypeManager,
        TagSystem tagSystem,
        PersistentCraftAccessibleInventoryPolicy? policy = null)
    {
        _entityManager = entityManager;
        _tagSystem = tagSystem;
        _policy = policy ?? PersistentCraftAccessibleInventoryPolicy.Default;
        _matcher = new PersistentCraftIngredientMatcher(entityManager, prototypeManager, tagSystem);
    }

    public PersistentCraftInventorySnapshot Build(
        EntityUid root,
        IReadOnlyList<PersistentCraftIngredient> trackedIngredients)
    {
        if (!_entityManager.EntityExists(root))
            return PersistentCraftInventorySnapshot.Empty;

        var trackedTags = new HashSet<string>();
        var trackedIngredientPrototypes = new HashSet<string>();

        for (var i = 0; i < trackedIngredients.Count; i++)
        {
            var ingredient = trackedIngredients[i];
            if (!string.IsNullOrWhiteSpace(ingredient.Proto))
                trackedIngredientPrototypes.Add(ingredient.Proto);

            if (!string.IsNullOrWhiteSpace(ingredient.Tag))
                trackedTags.Add(ingredient.Tag);
        }

        var ingredientStackTypeByProto = ResolveIngredientStackTypes(trackedIngredientPrototypes);
        var amountByProto = new Dictionary<string, int>();
        var nonStackAmountByProto = new Dictionary<string, int>();
        var amountByStackType = new Dictionary<string, int>();
        var amountByTag = new Dictionary<string, int>();
        var signatureBuilder = new StringBuilder();

        var accessibleEntities = CollectAccessibleEntities(root);
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
            signatureBuilder.Append(';');

            var hasStack = false;
            if (_entityManager.TryGetComponent(entity, out StackComponent? stack))
            {
                hasStack = true;
                AddAmount(amountByStackType, stack.StackTypeId, amount);
            }

            if (_entityManager.TryGetComponent(entity, out MetaDataComponent? meta) &&
                meta.EntityPrototype != null)
            {
                var prototypeId = meta.EntityPrototype.ID;
                AddAmount(amountByProto, prototypeId, amount);

                if (!hasStack)
                    AddAmount(nonStackAmountByProto, prototypeId, amount);
            }

            foreach (var tag in trackedTags)
            {
                if (_tagSystem.HasTag(entity, tag))
                    AddAmount(amountByTag, tag, amount);
            }
        }

        return new PersistentCraftInventorySnapshot(
            signatureBuilder.ToString(),
            amountByProto,
            nonStackAmountByProto,
            amountByStackType,
            amountByTag,
            ingredientStackTypeByProto);
    }

    private List<EntityUid> CollectAccessibleEntities(EntityUid root)
    {
        var result = new List<EntityUid>();
        var seen = new HashSet<EntityUid>();
        CollectAccessibleEntitiesRecursive(root, result, seen);
        return result;
    }

    private void CollectAccessibleEntitiesRecursive(
        EntityUid entity,
        List<EntityUid> result,
        HashSet<EntityUid> seen,
        ContainerManagerComponent? manager = null)
    {
        if (manager == null && !_entityManager.TryGetComponent(entity, out manager))
            return;

        foreach (var (containerId, container) in manager.Containers)
        {
            if (!_policy.ShouldIncludeContainer(containerId))
                continue;

            foreach (var contained in container.ContainedEntities)
            {
                if (!_policy.ShouldVisitEntity(_entityManager, contained, seen))
                    continue;

                result.Add(contained);
                CollectAccessibleEntitiesRecursive(contained, result, seen);
            }
        }
    }

    private Dictionary<string, string?> ResolveIngredientStackTypes(HashSet<string> trackedIngredientPrototypes)
    {
        var result = new Dictionary<string, string?>(trackedIngredientPrototypes.Count);

        foreach (var prototypeId in trackedIngredientPrototypes)
        {
            result[prototypeId] = _matcher.TryGetIngredientStackType(prototypeId, out var stackType)
                ? stackType
                : null;
        }

        return result;
    }

    private static void AddAmount(Dictionary<string, int> dictionary, string key, int amount)
    {
        if (dictionary.TryGetValue(key, out var existing))
            dictionary[key] = existing + amount;
        else
            dictionary[key] = amount;
    }
}
