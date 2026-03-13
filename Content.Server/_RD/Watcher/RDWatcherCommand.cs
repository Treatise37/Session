using Content.Server.Administration;
using Content.Shared._RD.Watcher;
using Content.Shared.Administration;
using Robust.Shared.Console;
using Robust.Shared.Toolshed;

namespace Content.Server._RD.Watcher;

[ToolshedCommand(Name = "rd_watcher"), AdminCommand(AdminFlags.Mapping)]
public sealed class RDWatcherCommand : ToolshedCommand
{
    [Dependency] private readonly IConsoleHost _console = null!;

    [CommandImplementation("vv")]
    public void ViewVariables([CommandInvocationContext] IInvocationContext ctx)
    {
        var watcherSystem = EntityManager.System<RDWatcherSystem>();
        _console.RemoteExecuteCommand(ctx.Session, $"vv {watcherSystem.ViewVariablesUid}");
    }

    [CommandImplementation("list")]
    public void List([CommandInvocationContext] IInvocationContext ctx)
    {
        var query = EntityManager.AllEntityQueryEnumerator<RDWatcherComponent, MetaDataComponent>();
        while (query.MoveNext(out var uid, out var watcher, out _))
        {
            ctx.WriteLine($"Watcher: {EntityManager.ToPrettyString(uid)}");
            ctx.WriteLine($"Entities ({watcher.Entities.Count}):");

            foreach (var entity in watcher.Entities)
            {
                ctx.WriteLine($"  - {EntityManager.ToPrettyString(entity)}");
            }

            ctx.WriteLine("");
        }
    }
}
