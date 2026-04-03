using System.Collections.Generic;
using Robust.Shared.GameStates;

namespace Content.Shared._Stalker.Mood;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class STMoodComponent : Component
{
    [DataField, AutoNetworkedField]
    public float Value = 0f;

    [DataField, AutoNetworkedField]
    public STMoodState State = STMoodState.Okay;

    [DataField, AutoNetworkedField]
    public List<STMoodEffect> ActiveEffects = new();

    [DataField, AutoNetworkedField]
    public float AgonyProgress = 0f;

    /// <summary>
    /// How fast agony builds up while the character remains in Pain.
    /// Rebalanced: higher than before so agony is reachable without crit-only gameplay.
    /// </summary>
    [DataField]
    public float PainToAgonyPerSecond = 1.0f;

    /// <summary>
    /// How fast agony progress decays when the character is no longer in Pain/Agony.
    /// </summary>
    [DataField]
    public float AgonyRecoveryPerSecond = 0.35f;

    /// <summary>
    /// How much accumulated agony progress is required before agony overflow starts applying.
    /// Rebalanced down so prolonged suffering actually matters.
    /// </summary>
    [DataField]
    public float AgonyThreshold = 22f;
}