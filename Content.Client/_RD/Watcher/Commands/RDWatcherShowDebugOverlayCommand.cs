using Robust.Client.Graphics;
using Robust.Shared.Console;

namespace Content.Client._RD.Watcher.Commands;

public sealed class RDWatcherShowDebugOverlayCommand : LocalizedCommands
{
    [Dependency] private readonly IOverlayManager _overlay = null!;

    public override string Command => "rd_watcher_show_debug_overlay";

    public override void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (_overlay.HasOverlay<RDWatcherDebugOverlay>())
        {
            _overlay.RemoveOverlay<RDWatcherDebugOverlay>();
            return;
        }

        _overlay.AddOverlay(new RDWatcherDebugOverlay());
    }
}
