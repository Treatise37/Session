using System.Collections.Generic;
using System.Numerics;

namespace Content.Client._Stalker.PersistentCrafting.UI.ViewModels;

public sealed class PersistentCraftStationViewModel
{
    public Dictionary<string, string> SelectedRecipes { get; } = new();
    public Dictionary<string, int> SelectedTierFilters { get; } = new();
    public Dictionary<string, string> SearchTextByBranch { get; } = new();
    public Dictionary<string, bool> CraftableOnlyByBranch { get; } = new();
    public Dictionary<string, Vector2> ListScrollByBranch { get; } = new();
    public Dictionary<string, Vector2> DetailScrollByBranch { get; } = new();
    public HashSet<string> CollapsedCategoryKeys { get; } = new();
    public HashSet<string> CollapsedSubCategoryKeys { get; } = new();
    public HashSet<string> InitializedCategoryKeys { get; } = new();
    public HashSet<string> InitializedSubCategoryKeys { get; } = new();
    public bool SelectPreferredBranchOnNextUpdate { get; set; } = true;
    public string LastVisibleBranch { get; set; } = string.Empty;
}
