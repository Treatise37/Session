using System.Numerics;
using Robust.Shared.GameStates;
using Robust.Shared.Map;

namespace Content.Shared._RD.Watcher;

public sealed partial class RDWatcherSystem
{
    [Dependency] private readonly SharedPvsOverrideSystem _pvsOverride = null!;

    private void UpdateWatcherPositions()
    {
        foreach (var watcher in _watcherCache)
        {
            UpdateWatcherPosition(watcher);
        }
    }

    private void UpdateWatcherPosition(Entity<RDWatcherComponent> watcher)
    {
        if (watcher.Comp.Entities.Count == 0)
            return;

        var sum = Vector2.Zero;
        var count = 0;

        MapId? mapId = null;
        foreach (var ent in watcher.Comp.Entities)
        {
            if (!ent.Valid)
                continue;

            sum += _transform.GetWorldPosition(ent);
            mapId ??= _transform.GetMapId(ent);
            count++;
        }

        if (count == 0 || mapId is null)
            return;

        watcher.Comp.Position = sum / count;
        watcher.Comp.MapId = mapId.Value;

        Dirty(watcher);
    }

    private Entity<RDWatcherComponent> CreateWatcher(HashSet<EntityUid> entities)
    {
        var instance = Spawn(null, MapCoordinates.Nullspace);
        var component = EnsureComp<RDWatcherComponent>(instance);

        component.Entities = entities;
        DirtyField(instance, component, nameof(RDWatcherComponent.Entities));

        var watcher = (instance, component);
        UpdateWatcherPosition(watcher);

        _pvsOverride.AddGlobalOverride(instance);

        return watcher;
    }

    private void WatcherAdd(Entity<RDWatcherComponent> watcher, HashSet<EntityUid> group)
    {
        foreach (var uid in group)
        {
            WatcherAdd(watcher, uid);
        }
    }

    private void WatcherAdd(Entity<RDWatcherComponent> entity, EntityUid targetUid)
    {
        entity.Comp.Entities.Add(targetUid);
        DirtyField(entity, entity.Comp, nameof(RDWatcherComponent.Entities));

        var watcherTarget = EnsureComp<RDWatcherTargetComponent>(targetUid);
        watcherTarget.Watcher = entity;
        DirtyField(targetUid, watcherTarget, nameof(RDWatcherTargetComponent.Watcher));
    }
}
