namespace Content.Shared._Stalker.Mood;

/// <summary>
/// Shared thresholds for converting mood value into a visible state.
/// </summary>
public static class STMoodThresholds
{
    public const float MinValue = -100f;
    public const float MaxValue = 100f;

    public const float GreatMin = 50f;
    public const float GoodMin = 20f;
    public const float OkayMin = 0f;
    public const float DiscomfortMin = -20f;
    public const float BadMin = -50f;
    public const float PainMin = -75f;

    public static STMoodState GetState(float value)
    {
        if (value >= GreatMin)
            return STMoodState.Great;

        if (value >= GoodMin)
            return STMoodState.Good;

        if (value >= OkayMin)
            return STMoodState.Okay;

        if (value >= DiscomfortMin)
            return STMoodState.Discomfort;

        if (value >= BadMin)
            return STMoodState.Bad;

        if (value >= PainMin)
            return STMoodState.Pain;

        return STMoodState.Agony;
    }

    public static float Clamp(float value)
    {
        if (value < MinValue)
            return MinValue;

        if (value > MaxValue)
            return MaxValue;

        return value;
    }
}