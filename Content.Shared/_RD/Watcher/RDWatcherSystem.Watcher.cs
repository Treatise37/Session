using System.Linq;
using System.Numerics;
using Robust.Shared.GameStates;
using Robust.Shared.Map;
using Robust.Shared.Utility;

namespace Content.Shared._RD.Watcher;

public sealed partial class RDWatcherSystem
{
    [Dependency] private readonly SharedPvsOverrideSystem _pvsOverride = null!;

    private readonly List<EntityUid> _tmpEntities = new(128);

    private void UpdateWatcherPositions()
    {
        for (var i = 0; i < _watcherCache.Count; i++)
        {
            UpdateWatcherPosition(_watcherCache[i]);
        }
    }

    private void UpdateWatcherPosition(Entity<RDWatcherComponent> watcher)
    {
        var entities = watcher.Comp.Entities;
        if (entities.Count == 0)
            return;

        var sum = Vector2.Zero;
        var count = 0;
        MapId? mapId = null;

        foreach (var ent in entities)
        {
            if (!ent.Valid || !Exists(ent) || !HasComp<TransformComponent>(ent))
                continue;

            var pos = _transform.GetWorldPosition(ent);

            sum += pos;
            mapId ??= _transform.GetMapId(ent);
            count++;
        }

        if (count == 0 || mapId is null)
            return;

        watcher.Comp.Position = sum / count;
        watcher.Comp.MapId = mapId.Value;

        Dirty(watcher);
    }

    private Entity<RDWatcherComponent> CreateWatcher(List<EntityUid> entities)
    {
        return CreateWatcher(entities.ToHashSet());
    }

    private Entity<RDWatcherComponent> CreateWatcher(HashSet<EntityUid> entities)
    {
        var uid = Spawn(null, MapCoordinates.Nullspace);
        var comp = EnsureComp<RDWatcherComponent>(uid);
        var watcher = (uid, comp);

        if (entities.Count == 0)
            return watcher;

        comp.Entities = entities;
        comp.GroupId = Comp<RDWatcherTargetComponent>(entities.First()).GroupId;
        DirtyFields(uid, comp, null, nameof(RDWatcherComponent.Entities), nameof(RDWatcherComponent.GroupId));

        UpdateWatcherPosition(watcher);

        // Just for client debug (Do PVS only for showed overlay entities later)
        _pvsOverride.AddGlobalOverride(uid);

        return watcher;
    }

    #region Sync

    private void WatcherSync(Entity<RDWatcherComponent> watcher, IEnumerable<EntityUid> group)
    {
        var entities = watcher.Comp.Entities;

        _tmpEntities.Clear();
        _tmpEntities.AddRange(entities);

        foreach (var uid in _tmpEntities)
        {
            WatcherRemove(watcher, uid);
        }

        foreach (var uid in group)
        {
            WatcherAdd(watcher, uid);
        }
    }

    #endregion

    #region Add

    private void WatcherAdd(Entity<RDWatcherComponent> watcher, EntityUid targetUid)
    {
        if (!watcher.Comp.Entities.Add(targetUid))
            return;

        var watcherTarget = EnsureComp<RDWatcherTargetComponent>(targetUid);
        watcherTarget.Watcher = watcher;

        DirtyField(targetUid, watcherTarget, nameof(RDWatcherTargetComponent.Watcher));
    }

    #endregion

    #region Remove

    private void WatcherRemove(Entity<RDWatcherComponent> watcher, EntityUid targetUid)
    {
        if (!watcher.Comp.Entities.Remove(targetUid))
            return;

        if (!TryComp<RDWatcherTargetComponent>(targetUid, out var target))
            return;

        target.Watcher = null;
        DirtyField(targetUid, target, nameof(RDWatcherTargetComponent.Watcher));
    }

    #endregion
}
