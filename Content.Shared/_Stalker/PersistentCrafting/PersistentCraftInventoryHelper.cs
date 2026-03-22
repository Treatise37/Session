using Content.Shared.Stacks;
using Content.Shared.Tag;
using Robust.Shared.Containers;
using Robust.Shared.GameObjects;
using Robust.Shared.Prototypes;

namespace Content.Shared._Stalker.PersistentCrafting;

public static class PersistentCraftInventoryHelper
{
    private const string IgnoredContainerId = "toggleable-clothing";

    public static List<EntityUid> CollectAccessibleEntities(IEntityManager entityManager, EntityUid root)
    {
        var result = new List<EntityUid>();
        var seen = new HashSet<EntityUid>();
        CollectAccessibleEntitiesRecursive(entityManager, root, result, seen);
        return result;
    }

    public static int CountIngredientAmount(
        IEntityManager entityManager,
        IPrototypeManager prototypeManager,
        TagSystem tagSystem,
        EntityUid root,
        PersistentCraftIngredient ingredient)
    {
        var total = 0;
        var seen = new HashSet<EntityUid>();
        CountIngredientAmountRecursive(entityManager, prototypeManager, tagSystem, root, ingredient, ref total, seen);
        return total;
    }

    public static bool MatchesIngredient(
        IEntityManager entityManager,
        IPrototypeManager prototypeManager,
        TagSystem tagSystem,
        EntityUid entity,
        PersistentCraftIngredient ingredient)
    {
        if (!string.IsNullOrWhiteSpace(ingredient.Proto) &&
            entityManager.TryGetComponent(entity, out MetaDataComponent? meta) &&
            meta.EntityPrototype?.ID == ingredient.Proto)
        {
            return true;
        }

        if (!string.IsNullOrWhiteSpace(ingredient.Proto) &&
            entityManager.TryGetComponent(entity, out StackComponent? stack) &&
            prototypeManager.TryIndex<EntityPrototype>(ingredient.Proto, out var ingredientProto) &&
            ingredientProto.TryGetComponent<StackComponent>(out var ingredientStack, entityManager.ComponentFactory) &&
            stack.StackTypeId == ingredientStack.StackTypeId)
        {
            return true;
        }

        return !string.IsNullOrWhiteSpace(ingredient.Tag) && tagSystem.HasTag(entity, ingredient.Tag);
    }

    public static int GetUsableAmount(IEntityManager entityManager, EntityUid entity)
    {
        return entityManager.TryGetComponent(entity, out StackComponent? stack) ? stack.Count : 1;
    }

    private static void CollectAccessibleEntitiesRecursive(
        IEntityManager entityManager,
        EntityUid entity,
        List<EntityUid> result,
        HashSet<EntityUid> seen,
        ContainerManagerComponent? manager = null)
    {
        if (manager == null && !entityManager.TryGetComponent(entity, out manager))
            return;

        foreach (var (containerId, container) in manager.Containers)
        {
            if (containerId == IgnoredContainerId)
                continue;

            foreach (var contained in container.ContainedEntities)
            {
                if (!entityManager.EntityExists(contained) || !seen.Add(contained))
                    continue;

                result.Add(contained);
                CollectAccessibleEntitiesRecursive(entityManager, contained, result, seen);
            }
        }
    }

    private static void CountIngredientAmountRecursive(
        IEntityManager entityManager,
        IPrototypeManager prototypeManager,
        TagSystem tagSystem,
        EntityUid entity,
        PersistentCraftIngredient ingredient,
        ref int total,
        HashSet<EntityUid> seen,
        ContainerManagerComponent? manager = null)
    {
        if (manager == null && !entityManager.TryGetComponent(entity, out manager))
            return;

        foreach (var (containerId, container) in manager.Containers)
        {
            if (containerId == IgnoredContainerId)
                continue;

            foreach (var contained in container.ContainedEntities)
            {
                if (!entityManager.EntityExists(contained) || !seen.Add(contained))
                    continue;

                if (MatchesIngredient(entityManager, prototypeManager, tagSystem, contained, ingredient))
                    total += GetUsableAmount(entityManager, contained);

                CountIngredientAmountRecursive(
                    entityManager,
                    prototypeManager,
                    tagSystem,
                    contained,
                    ingredient,
                    ref total,
                    seen);
            }
        }
    }
}
