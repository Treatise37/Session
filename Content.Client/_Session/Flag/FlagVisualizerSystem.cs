using Content.Shared._Session.Flag;
using Robust.Client.GameObjects;

namespace Content.Client._Session.Flag;

public sealed class WarZoneFlagVisualizerSystem : VisualizerSystem<WarZoneFlagComponent>
{
    protected override void OnAppearanceChange(EntityUid uid, WarZoneFlagComponent component, ref AppearanceChangeEvent args)
    {
        if (args.Sprite == null)
            return;

        if (AppearanceSystem.TryGetData(uid, WarZoneFlagVisuals.Level, out int level, args.Component))
            args.Sprite.LayerSetState(0, $"flag{level}");
    }
}
