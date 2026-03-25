using Robust.Shared.GameStates;

namespace Content.Shared.Weather;

/// <summary>
/// Disables weather entirely for the map entity this component is attached to.
/// Intended for underground maps, interiors and other locations where weather must never run.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class DisableMapWeatherComponent : Component
{
}