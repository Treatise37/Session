using Robust.Shared.Containers;
using Robust.Shared.GameObjects;

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
}
