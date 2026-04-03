using System;

namespace Content.Shared._Stalker.Mood;

public abstract class SharedSTMoodSystem : EntitySystem
{
    public float ClampMood(float value)
    {
        return Math.Clamp(value, -100f, 100f);
    }

    public STMoodState GetMoodState(float value)
    {
        if (value <= -85f)
            return STMoodState.Agony;

        if (value <= -55f)
            return STMoodState.Pain;

        if (value <= -30f)
            return STMoodState.Bad;

        if (value <= -10f)
            return STMoodState.Discomfort;

        if (value >= 35f)
            return STMoodState.Great;

        if (value >= 15f)
            return STMoodState.Good;

        return STMoodState.Okay;
    }
}