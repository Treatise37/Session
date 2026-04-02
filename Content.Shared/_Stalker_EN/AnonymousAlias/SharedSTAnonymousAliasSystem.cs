namespace Content.Shared._Stalker_EN.AnonymousAlias;

/// <summary>
/// Lightweight runtime anonymous alias generator.
/// Stable for the same true name, so the same masked player keeps the same alias.
/// </summary>
public sealed class SharedSTAnonymousAliasSystem : EntitySystem
{
    private static readonly string[] Adjectives =
    {
        "Scarred",
        "Weathered",
        "Silent",
        "Ragged",
        "Ashen",
        "Grim",
        "Dusty",
        "Wary",
        "Cold",
        "Shrouded",
        "Steel",
        "Hollow",
        "Restless",
        "Bleak",
        "Faded",
        "Wicked"
    };

    private static readonly string[] Nouns =
    {
        "Stalker",
        "Nomad",
        "Drifter",
        "Wanderer",
        "Outcast",
        "Hunter",
        "Stranger",
        "Pilgrim",
        "Shade",
        "Ghost",
        "Rogue",
        "Hermit",
        "Raider",
        "Watcher",
        "Vagabond",
        "Survivor"
    };

    public string GetAlias(string trueName)
    {
        if (string.IsNullOrWhiteSpace(trueName))
            return "Unknown Stalker";

        var hash = trueName.GetHashCode();
        var hashA = hash & 0x7fffffff;
        var hashB = ((hash * 397) ^ 0x5f3759df) & 0x7fffffff;

        var adjective = Adjectives[hashA % Adjectives.Length];
        var noun = Nouns[hashB % Nouns.Length];

        return $"{adjective} {noun}";
    }
}