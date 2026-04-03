using Robust.Shared.Serialization;

namespace Content.Shared._Stalker.Mood;

/// <summary>
/// Runtime aggregated mood contribution.
/// This is not a component, just a compact data container used by mood systems.
/// </summary>
[Serializable, NetSerializable]
public sealed class STMoodEffect
{
    public STMoodEffectType Type { get; set; }
    public float Value { get; set; }
    public string? SourceId { get; set; }

    public STMoodEffect()
    {
    }

    public STMoodEffect(STMoodEffectType type, float value, string? sourceId = null)
    {
        Type = type;
        Value = value;
        SourceId = sourceId;
    }
}