using Robust.Shared.Serialization;

namespace Content.Shared._Stalker.PdaMessenger;

/// <summary>
/// Event sent from server to all clients when a message is sent to the General PDA channel.
/// Used for displaying toast notifications in the bottom-left corner.
/// </summary>
[Serializable, NetSerializable]
public sealed class PdaGeneralMessageEvent : EntityEventArgs
{
    public readonly string Title;
    public readonly string Content;
    /// <summary>Sender display name (defaults to Title if not provided).</summary>
    public readonly string Sender;

    /// <summary>
    /// Band icon name (e.g. "stalker", "freedom", "Dolg", "band").
    /// Used to determine which faction PNG texture to display.
    /// </summary>
    public readonly string? BandIcon;

    /// <summary>
    /// Character portrait texture path for the sender's selected portrait.
    /// If set, takes priority over BandIcon for notification display.
    /// </summary>
    public readonly string? PortraitId;

    /// <summary>
    /// Whether the sender is disguised (e.g., Clear Sky disguised as Loners).
    /// Used to display correct faction icon when PNG icons are disabled.
    /// </summary>
    public readonly bool IsDisguised;

    public PdaGeneralMessageEvent(string title, string content, string? bandIcon = null, string? portraitId = null, bool isDisguised = false, string? sender = null)
    {
        Title = title;
        Content = content;
        Sender = sender ?? title;
        BandIcon = bandIcon;
        PortraitId = portraitId;
        IsDisguised = isDisguised;
    }
}
