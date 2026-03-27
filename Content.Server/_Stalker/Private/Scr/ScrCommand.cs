using Content.Server.Administration;
using Content.Shared.Administration;
using Robust.Server.Player;
using Robust.Shared.Console;

namespace Content.Server._Stalker.Private.Scr;

[AdminCommand(AdminFlags.Moderator)]
public sealed class ScrCommand : IConsoleCommand
{
    [Dependency] private readonly IPlayerManager _player = null!;
    [Dependency] private readonly IEntityManager _entMan = null!;
    private ScrSystem? _scr;

    public string Command => "scr";
    public string Description => "Request a screenshot from a connected client.";
    public string Help => "Usage: scr <username> [clyde|viewport]\n" +
                          "  clyde    – capture via the Clyde renderer (default)\n" +
                          "  viewport – capture the in-game viewport only";

    public void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (args.Length == 0)
        {
            shell.WriteError("Missing argument: provide a username. " + Help);
            return;
        }

        var userName = args[0];

        if (!_player.TryGetSessionByUsername(userName, out var session))
        {
            shell.WriteError($"No connected player found with username \"{userName}\".");
            return;
        }

        var mode = args.Length > 1 ? args[1].ToLowerInvariant() : "clyde";
        bool isClyde;

        switch (mode)
        {
            case "clyde":
                isClyde = true;
                break;
            case "viewport":
                isClyde = false;
                break;
            default:
                shell.WriteError($"Unknown capture mode \"{mode}\". Valid options: clyde, viewport.");
                return;
        }

        _scr ??= _entMan.System<ScrSystem>();
        if (!_scr.RequestScr(session, isClyde, shell.Player))
            shell.WriteError($"Failed to request a screenshot from \"{userName}\". Probably another request for this user already exists.");
    }

    public CompletionResult GetCompletion(IConsoleShell shell, string[] args)
    {
        switch (args.Length)
        {
            case 1:
                return CompletionResult.FromHintOptions(
                    CompletionHelper.SessionNames(),
                    Loc.GetString("shell-argument-username-hint"));

            case 2:
                return CompletionResult.FromOptions(new[]
                {
                    new CompletionOption("clyde",    "Capture via the Clyde renderer (default)"),
                    new CompletionOption("viewport", "Capture the in-game viewport only"),
                });

            default:
                return CompletionResult.Empty;
        }
    }
}
