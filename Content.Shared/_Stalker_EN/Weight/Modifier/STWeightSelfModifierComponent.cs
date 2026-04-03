using Content.Shared._Stalker.Modifier;
using Robust.Shared.GameStates;

namespace Content.Shared._Stalker_EN.Weight.Modifier;

/// <summary>
/// Component for weight self modifier status effects.
/// Attached to status effect entities to store the weight modifier value.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class STWeightSelfModifierComponent : BaseFloatModifierComponent
{
}