using System.Collections.Generic;
using Robust.Shared.GameObjects;

namespace Content.Shared._Stalker.PersistentCrafting;

public sealed class PersistentCraftAccessibleInventoryPolicy
{
    public const string DefaultIgnoredContainerId = "toggleable-clothing";
    public static readonly PersistentCraftAccessibleInventoryPolicy Default = new();

    public string IgnoredContainerId { get; }

    public PersistentCraftAccessibleInventoryPolicy(string ignoredContainerId = DefaultIgnoredContainerId)
    {
        IgnoredContainerId = ignoredContainerId;
    }

    public bool ShouldIncludeContainer(string containerId)
    {
        return containerId != IgnoredContainerId;
    }

    public bool ShouldVisitEntity(IEntityManager entityManager, EntityUid entity, HashSet<EntityUid> seen)
    {
        return entityManager.EntityExists(entity) && seen.Add(entity);
    }
}
