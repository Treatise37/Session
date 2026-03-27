using Content.Shared.Stacks;
using Content.Shared.Tag;
using Robust.Shared.Containers;
using Robust.Shared.GameObjects;
using Robust.Shared.Prototypes;

namespace Content.Shared._Stalker.PersistentCrafting;

public static class PersistentCraftInventoryHelper
{
    private static readonly PersistentCraftAccessibleInventoryPolicy DefaultPolicy = PersistentCraftAccessibleInventoryPolicy.Default;

    public static List<EntityUid> CollectAccessibleEntities(IEntityManager entityManager, EntityUid root)
    {
        var result = new List<EntityUid>();
        var seen = new HashSet<EntityUid>();
        CollectAccessibleEntitiesRecursive(entityManager, root, result, seen, DefaultPolicy);
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
        var matcher = new PersistentCraftIngredientMatcher(entityManager, prototypeManager, tagSystem);
        CountIngredientAmountRecursive(entityManager, matcher, root, ingredient, ref total, seen, DefaultPolicy);
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
        return entityManager.TryGetComponent(entity, out StackComponent? stack)
            ? stack.Count
            : 1;
    }

    private static void CollectAccessibleEntitiesRecursive(
        IEntityManager entityManager,
        EntityUid entity,
        List<EntityUid> result,
        HashSet<EntityUid> seen,
        PersistentCraftAccessibleInventoryPolicy policy,
        ContainerManagerComponent? manager = null)
    {
        if (manager == null && !entityManager.TryGetComponent(entity, out manager))
            return;

        foreach (var (containerId, container) in manager.Containers)
        {
            if (!policy.ShouldIncludeContainer(containerId))
                continue;

            foreach (var contained in container.ContainedEntities)
            {
                if (!policy.ShouldVisitEntity(entityManager, contained, seen))
                    continue;

                result.Add(contained);
                CollectAccessibleEntitiesRecursive(entityManager, contained, result, seen, policy);
            }
        }
    }

    private static void CountIngredientAmountRecursive(
        IEntityManager entityManager,
        PersistentCraftIngredientMatcher matcher,
        EntityUid entity,
        PersistentCraftIngredient ingredient,
        ref int total,
        HashSet<EntityUid> seen,
        PersistentCraftAccessibleInventoryPolicy policy,
        ContainerManagerComponent? manager = null)
    {
        if (manager == null && !entityManager.TryGetComponent(entity, out manager))
            return;

        foreach (var (containerId, container) in manager.Containers)
        {
            if (!policy.ShouldIncludeContainer(containerId))
                continue;

            foreach (var contained in container.ContainedEntities)
            {
                if (!policy.ShouldVisitEntity(entityManager, contained, seen))
                    continue;

                if (matcher.Matches(contained, ingredient))
                    total += matcher.GetUsableAmount(contained);

                CountIngredientAmountRecursive(
                    entityManager,
                    matcher,
                    contained,
                    ingredient,
                    ref total,
                    seen,
                    policy);
            }
        }
    }
}
