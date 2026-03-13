using Robust.Shared.GameStates;

namespace Content.Shared._Stalker.Weight;

/// <summary>
/// When present on an entity with <see cref="STWeightComponent"/>, reduces the weight of items stored inside it.
/// </summary>
[RegisterComponent]
public sealed partial class STWeightInsideReductionComponent : Component
{
    /// <summary>
    /// Fraction of the inside weight to remove.
    /// For example 0.2 means items inside count for 80% of their normal weight.
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public float ReductionFraction = 0f;
}
