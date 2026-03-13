using Robust.Shared.GameObjects;
using Robust.Shared.Serialization;

namespace Content.Shared._Stalker_EN.PdaMessenger;

/// <summary>
/// Full UI state for the messenger cartridge, sent via the CartridgeLoader BUI system.
/// Messages are only included for the chat the client is currently viewing (lazy loading).
/// </summary>
[Serializable, NetSerializable]
public sealed class STMessengerUiState : BoundUserInterfaceState
{
    /// <summary>
    /// This PDA's unique messenger ID (displayed in UI header).
    /// </summary>
    public readonly string MessengerId;

    /// <summary>
    /// Public channel chats with per-PDA unread/mute state.
    /// Messages only populated for the currently viewed chat.
    /// </summary>
    public readonly List<STMessengerChat> Channels;

    /// <summary>
    /// DM conversations for this PDA.
    /// Messages only populated for the currently viewed chat.
    /// </summary>
    public readonly List<STMessengerChat> DirectMessages;

    /// <summary>
    /// This PDA's contact list with faction patch and PDA ID metadata.
    /// </summary>
    public readonly List<STMessengerContactInfo> Contacts;

    /// <summary>
    /// When set, the client should auto-navigate to this chat ID.
    /// Used by external systems (e.g. merc board) to deep-link into a DM.
    /// Null means no navigation requested.
    /// </summary>
    public readonly string? NavigateToChatId;

    /// <summary>
    /// When set alongside <see cref="NavigateToChatId"/>, the compose page opens with this text pre-filled.
    /// Used by external systems (e.g. merc board contact with offer reference).
    /// Null means no draft pre-fill.
    /// </summary>
    public readonly string? DraftMessage;

    public STMessengerUiState(
        string messengerId,
        List<STMessengerChat> channels,
        List<STMessengerChat> directMessages,
        List<STMessengerContactInfo> contacts,
        string? navigateToChatId = null,
        string? draftMessage = null)
    {
        MessengerId = messengerId;
        Channels = channels;
        DirectMessages = directMessages;
        Contacts = contacts;
        NavigateToChatId = navigateToChatId;
        DraftMessage = draftMessage;
    }
}
