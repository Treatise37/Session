namespace Content.Shared._Stalker.Mood;

/// <summary>
/// Raised before a do-after starts, allowing mood systems to adjust its final delay.
/// </summary>
public sealed class STMoodModifyDoAfterEvent : EntityEventArgs
{
    public TimeSpan Delay;

    public STMoodModifyDoAfterEvent(TimeSpan delay)
    {
        Delay = delay;
    }
}