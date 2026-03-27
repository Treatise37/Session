using System;
using System.Threading.Tasks;
using Content.Server.Administration;
using Content.Server.Database;
using Content.Shared.Administration;
using Robust.Shared.Console;

namespace Content.Server._Stalker.PersistentCrafting;

[AdminCommand(AdminFlags.Host)]
public sealed class PersistentCraftResetOfflineCommand : IConsoleCommand
{
    [Dependency] private readonly IServerDbManager _db = default!;

    public string Command => "st_pcraft_reset_offline";
    public string Description => "Resets persistent crafting progress for a character by userId and characterName.";
    public string Help => "st_pcraft_reset_offline <userId-guid> <characterName>";

    // Keep payload aligned with current save schema (v2) and zero progression.
    private const string EmptyProfileJson = "{\"Version\":2,\"Branches\":[],\"UnlockedNodes\":[]}";

    public void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        _ = ExecuteAsync(shell, args);
    }

    private async Task ExecuteAsync(IConsoleShell shell, string[] args)
    {
        try
        {
            if (args.Length < 2)
            {
                shell.WriteError($"Usage: {Help}");
                return;
            }

            if (!Guid.TryParse(args[0], out var userId))
            {
                shell.WriteError("Invalid userId guid.");
                return;
            }

            var characterName = string.Join(' ', args, 1, args.Length - 1).Trim();
            if (string.IsNullOrWhiteSpace(characterName))
            {
                shell.WriteError("Character name cannot be empty.");
                return;
            }

            await _db.SetStalkerPersistentCraftProfileAsync(userId, characterName, EmptyProfileJson);
            shell.WriteLine($"Persistent craft profile reset for '{characterName}' ({userId}).");
        }
        catch (Exception ex)
        {
            shell.WriteError($"Persistent craft offline reset failed: {ex.Message}");
        }
    }

    public CompletionResult GetCompletion(IConsoleShell shell, string[] args)
    {
        return CompletionResult.Empty;
    }
}
