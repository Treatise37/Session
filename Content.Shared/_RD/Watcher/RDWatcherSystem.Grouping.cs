using System.Numerics;
using Robust.Shared.Map;

namespace Content.Shared._RD.Watcher;

public sealed partial class RDWatcherSystem
{
    [Dependency] private readonly SharedTransformSystem _transform = null!;

    private readonly List<TargetEntity> _targets = new(150);

    private void InitializeGrouping()
    {
        SubscribeLocalEvent<RDWatcherTargetComponent, ComponentRemove>(OnTargetRemove);
    }

    private void OnTargetRemove(EntityUid uid, RDWatcherTargetComponent component, ComponentRemove args)
    {
        if (component.Watcher is not { } watcherUid)
            return;

        if (!TryComp<RDWatcherComponent>(watcherUid, out var watcher))
            return;

        watcher.Entities.Remove(uid);

        if (watcher.Entities.Count == 0)
            QueueDel(watcherUid);
    }

    private void UpdateWatchers()
    {
        _targets.Clear();

        var query = EntityQueryEnumerator<RDWatcherTargetComponent, TransformComponent>();
        while (query.MoveNext(out var uid, out _, out var transform))
        {
            _targets.Add(new TargetEntity(uid, _transform.GetWorldPosition(transform), _transform.GetMapId((uid, transform))));
        }

        var visited = new HashSet<EntityUid>();
        foreach (var entity in _targets)
        {
            if (visited.Contains(entity.Uid))
                continue;

            var group = new HashSet<EntityUid> { entity.Uid };
            var queue = new Queue<TargetEntity>();

            queue.Enqueue(entity);
            visited.Add(entity.Uid);

            while (queue.Count > 0)
            {
                var current = queue.Dequeue();

                foreach (var other in _targets)
                {
                    if (visited.Contains(other.Uid))
                        continue;

                    if (other.MapId != current.MapId)
                        continue;

                    if ((other.Position - current.Position).Length() > Inst.Comp.GroupRadius)
                        continue;

                    group.Add(other.Uid);
                    queue.Enqueue(other);
                    visited.Add(other.Uid);
                }
            }

            Entity<RDWatcherComponent>? existingWatcher = null;
            foreach (var watcher in _watcherCache)
            {
                if (!group.Overlaps(watcher.Comp.Entities))
                    continue;

                existingWatcher = watcher;
                break;
            }

            if (existingWatcher is null)
            {
                _ = CreateWatcher(group);
                continue;
            }

            WatcherAdd(existingWatcher.Value, group);
        }

        for (var i = _watcherCache.Count - 1; i >= 0; i--)
        {
            if (_watcherCache[i].Comp.Entities.Count != 0)
                continue;

            QueueDel(_watcherCache[i]);
        }
    }

    private readonly record struct TargetEntity(
        EntityUid Uid,
        Vector2 Position,
        MapId MapId
    );
}
