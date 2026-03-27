using System.Collections.Generic;

namespace Content.Client._Stalker.PersistentCrafting.UI.ViewModels;

public sealed class PersistentCraftSkillTreeViewModel
{
    public Dictionary<string, string> SelectedNodeByBranch { get; } = new();
    public bool SelectPreferredBranchOnNextUpdate { get; set; } = true;
}
