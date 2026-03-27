using Content.Shared._Stalker.PersistentCrafting;

namespace Content.Server._Stalker.PersistentCrafting;

[RegisterComponent, Access(typeof(PersistentCraftingSystem))]
public sealed partial class PersistentCraftProfileComponent : Component
{
    public Guid UserId;
    public string CharacterName = string.Empty;
    public Dictionary<string, PersistentCraftBranchProfile> BranchProgress = new();
    public HashSet<string> UnlockedNodes = new();
    public bool Loaded;
    public bool PersistenceDisabled;
}
