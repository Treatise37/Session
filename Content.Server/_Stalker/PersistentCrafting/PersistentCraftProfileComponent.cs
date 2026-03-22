using Content.Shared._Stalker.PersistentCrafting;

namespace Content.Server._Stalker.PersistentCrafting;

[RegisterComponent, Access(typeof(PersistentCraftingSystem))]
public sealed partial class PersistentCraftProfileComponent : Component
{
    public Guid UserId;
    public string CharacterName = string.Empty;
    public Dictionary<PersistentCraftBranch, PersistentCraftBranchProfile> BranchProgress = new()
    {
        [PersistentCraftBranch.Weapon] = new(),
        [PersistentCraftBranch.Armor] = new(),
        [PersistentCraftBranch.Anomaly] = new(),
    };
    public HashSet<string> UnlockedNodes = new();
    public bool Loaded;
}
