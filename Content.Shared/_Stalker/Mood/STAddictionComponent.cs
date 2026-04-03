using Robust.Shared.GameStates;

namespace Content.Shared._Stalker.Mood;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class STAddictionComponent : Component
{
    /// <summary>
    /// Character has acquired narcotic addiction.
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool DrugAddicted = false;

    /// <summary>
    /// Time in seconds after the last drug dose before withdrawal starts growing.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float DrugReliefTime = 0f;

    /// <summary>
    /// Default relief duration granted by any tracked narcotic.
    /// </summary>
    [DataField]
    public float DrugReliefDuration = 120f;

    /// <summary>
    /// Withdrawal progression from 0 to 100.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float DrugWithdrawalProgress = 0f;

    /// <summary>
    /// How fast withdrawal grows per second after relief ends.
    /// </summary>
    [DataField]
    public float DrugWithdrawalPerSecond = 0.5f;

    /// <summary>
    /// How long the character must survive at maximum withdrawal to fully clear addiction.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float DrugRecoveryTime = 0f;

    /// <summary>
    /// Required time at maximum withdrawal before addiction is cleared.
    /// </summary>
    [DataField]
    public float DrugRecoveryDuration = 60f;

    /// <summary>
    /// Character has acquired nicotine addiction.
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool NicotineAddicted = false;

    /// <summary>
    /// Time in seconds after the last nicotine dose before craving starts growing.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float NicotineReliefTime = 0f;

    /// <summary>
    /// Default relief duration granted by nicotine.
    /// </summary>
    [DataField]
    public float NicotineReliefDuration = 90f;

    /// <summary>
    /// Nicotine craving progression from 0 to 100.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float NicotineWithdrawalProgress = 0f;

    /// <summary>
    /// How fast nicotine craving grows per second after relief ends.
    /// </summary>
    [DataField]
    public float NicotineWithdrawalPerSecond = 0.35f;

    /// <summary>
    /// How long the character must stay at maximum nicotine withdrawal to clear addiction.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float NicotineRecoveryTime = 0f;

    /// <summary>
    /// Required time at maximum nicotine withdrawal before nicotine addiction is cleared.
    /// </summary>
    [DataField]
    public float NicotineRecoveryDuration = 45f;

    /// <summary>
    /// Character has acquired alcohol dependence.
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool AlcoholAddicted = false;

    /// <summary>
    /// Time in seconds after the last alcohol dose before crash starts growing.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float AlcoholReliefTime = 0f;

    /// <summary>
    /// Default relief duration granted by alcohol.
    /// </summary>
    [DataField]
    public float AlcoholReliefDuration = 120f;

    /// <summary>
    /// Alcohol crash progression from 0 to 100.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float AlcoholWithdrawalProgress = 0f;

    /// <summary>
    /// How fast alcohol crash grows per second after relief ends.
    /// </summary>
    [DataField]
    public float AlcoholWithdrawalPerSecond = 0.3f;

    /// <summary>
    /// How long the character must stay at maximum alcohol withdrawal to clear dependence.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float AlcoholRecoveryTime = 0f;

    /// <summary>
    /// Required time at maximum alcohol withdrawal before alcohol dependence is cleared.
    /// </summary>
    [DataField]
    public float AlcoholRecoveryDuration = 60f;
}