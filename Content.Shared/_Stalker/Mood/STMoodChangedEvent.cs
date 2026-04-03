using Robust.Shared.Serialization;

namespace Content.Shared._Stalker.Mood;

[Serializable, NetSerializable]
public sealed class STMoodChangedEvent : EntityEventArgs
{
    public float OldValue { get; }
    public float NewValue { get; }

    public STMoodState OldState { get; }
    public STMoodState NewState { get; }

    public STMoodChangedEvent(float oldValue, float newValue, STMoodState oldState, STMoodState newState)
    {
        OldValue = oldValue;
        NewValue = newValue;
        OldState = oldState;
        NewState = newState;
    }
}