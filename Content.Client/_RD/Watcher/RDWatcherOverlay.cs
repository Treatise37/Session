using System.Numerics;
using System.Text;
using Content.Shared._RD.Watcher;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Client.ResourceManagement;
using Robust.Shared.Enums;

namespace Content.Client._RD.Watcher;

public sealed class RDWatcherDebugOverlay : Overlay
{
    [Dependency] private readonly IEntityManager _entityManager = null!;
    [Dependency] private readonly IResourceCache _cache = null!;

    private readonly TransformSystem _transform;

    public override OverlaySpace Space => OverlaySpace.ScreenSpace;
    private readonly Font _font;

    public RDWatcherDebugOverlay()
    {
        IoCManager.InjectDependencies(this);

        _transform = _entityManager.System<TransformSystem>();

        _font = new VectorFont(_cache.GetResource<FontResource>("/Fonts/NotoSans/NotoSans-Regular.ttf"), 8);
    }

    protected override void Draw(in OverlayDrawArgs args)
    {
        var query = _entityManager.EntityQueryEnumerator<RDWatcherComponent>();
        while (query.MoveNext(out var uid, out var watcher))
        {
            if (watcher.Entities.Count == 0)
                continue;

            var worldPos = watcher.Position;
            if (!args.WorldAABB.Contains(worldPos))
                continue;

            var screenPos = args.ViewportControl?.WorldToScreen(worldPos) ?? Vector2.Zero;

            var text = BuildText(uid, watcher);
            args.ScreenHandle.DrawString(_font, screenPos, text, Color.Yellow);
            args.ScreenHandle.DrawCircle(screenPos, 4.5f, Color.Cyan);

            foreach (var targetUid in watcher.Entities)
            {
                var targetPosition = _transform.GetWorldPosition(targetUid);
                if (!args.WorldAABB.Contains(targetPosition))
                    continue;

                var targetScreenPos = args.ViewportControl?.WorldToScreen(targetPosition) ?? Vector2.Zero;
                args.ScreenHandle.DrawLine(screenPos, targetScreenPos, Color.Cyan);
            }
        }
    }

    private string BuildText(EntityUid watcherUid, RDWatcherComponent watcher)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"Watcher: {watcherUid} [{watcher.Entities.Count}] ({watcher.Position};{watcher.MapId})");

        foreach (var uid in watcher.Entities)
        {
            sb.AppendLine($" - {_entityManager.ToPrettyString(uid)}");
        }

        return sb.ToString();
    }
}
