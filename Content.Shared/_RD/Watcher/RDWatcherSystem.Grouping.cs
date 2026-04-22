using System.Numerics;
using Robust.Shared.Map;

namespace Content.Shared._RD.Watcher;

public sealed partial class RDWatcherSystem
{
    [Dependency] private readonly SharedTransformSystem _transform = null!;

    private readonly List<TargetEntity> _targets = new(256);
    private readonly HashSet<EntityUid> _visited = new(256);
    private readonly Queue<int> _queue = new(256);
    private readonly List<EntityUid> _group = new(64);
    private readonly List<Entity<RDWatcherComponent>> _overlapping = new(8);

    private void InitializeGrouping()
    {
        SubscribeLocalEvent<RDWatcherTargetComponent, ComponentRemove>(OnTargetRemove);
    }

    private void OnTargetRemove(EntityUid uid, RDWatcherTargetComponent component, ComponentRemove args)
    {
        if (_net.IsClient)
            return;

        if (component.Watcher is not { } watcherUid)
            return;

        if (!TryComp<RDWatcherComponent>(watcherUid, out var watcher))
            return;

        if (!watcher.Entities.Remove(uid))
            return;

        if (watcher.Entities.Count == 0)
        {
            QueueDel(watcherUid);
            return;
        }

        DirtyField(watcherUid, watcher, nameof(RDWatcherComponent.Entities));
    }

    private void UpdateWatchers()
    {
        _targets.Clear();
        _visited.Clear();

        var radiusSq = Inst.Comp.GroupRadius * Inst.Comp.GroupRadius;

        FillTargets();

        var groups = new List<List<EntityUid>>();

        for (var i = 0; i < _targets.Count; i++)
        {
            var start = _targets[i];
            if (_visited.Contains(start.Uid))
                continue;

            var group = new List<EntityUid>();

            var queue = new Queue<int>();
            queue.Enqueue(i);

            _visited.Add(start.Uid);

            while (queue.Count > 0)
            {
                var current = _targets[queue.Dequeue()];
                group.Add(current.Uid);

                for (var j = 0; j < _targets.Count; j++)
                {
                    var other = _targets[j];

                    if (_visited.Contains(other.Uid))
                        continue;

                    if (other.MapId != current.MapId)
                        continue;

                    if (other.GroupId != current.GroupId)
                        continue;

                    if (Vector2.DistanceSquared(other.Position, current.Position) > radiusSq)
                        continue;

                    _visited.Add(other.Uid);

                    queue.Enqueue(j);
                }
            }

            groups.Add(group);
        }

        foreach (var group in groups)
        {
            SyncGroupWithWatchers(group);
        }
    }

    private void FillTargets()
    {
        var query = EntityQueryEnumerator<RDWatcherTargetComponent, TransformComponent>();
        while (query.MoveNext(out var uid, out var targetComponent, out var transform))
        {
            _targets.Add(new TargetEntity(
                    uid,
                    _transform.GetWorldPosition(transform),
                    _transform.GetMapId((uid, transform)),
                    targetComponent.GroupId
                )
            );
        }

    }

    private void SyncGroupWithWatchers(List<EntityUid> group)
    {
        _overlapping.Clear();

        for (var i = 0; i < _watcherCache.Count; i++)
        {
            var watcher = _watcherCache[i];
            foreach (var ent in group)
            {
                if (watcher.Comp.Entities.Contains(ent))
                {
                    _overlapping.Add(watcher);
                    break;
                }
            }
        }

        if (_overlapping.Count == 0)
        {
            _ = CreateWatcher(group);
            return;
        }

        var main = _overlapping[0];

        WatcherSync(main, group);

        for (var i = 1; i < _overlapping.Count; i++)
        {
            var other = _overlapping[i];

            foreach (var ent in other.Comp.Entities)
            {
                main.Comp.Entities.Add(ent);
            }

            other.Comp.Entities.Clear();
            QueueDel(other);
        }
    }

    private readonly record struct TargetEntity(
        EntityUid Uid,
        Vector2 Position,
        MapId MapId,
        string GroupId
    );
}
