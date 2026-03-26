using System.Linq;

namespace Content.Shared._Stalker.PersistentCrafting;

public static class PersistentCraftingHelper
{
    private static readonly PersistentCraftBranch[] Branches =
    {
        PersistentCraftBranch.Weapon,
        PersistentCraftBranch.Armor,
        PersistentCraftBranch.Anomaly,
    };

    public static IReadOnlyList<PersistentCraftBranch> EnumerateBranches()
    {
        return Branches;
    }

    public static string GetBranchLocKey(PersistentCraftBranch branch)
    {
        return branch switch
        {
            PersistentCraftBranch.Weapon => "persistent-craft-branch-weapon",
            PersistentCraftBranch.Armor => "persistent-craft-branch-armor",
            PersistentCraftBranch.Anomaly => "persistent-craft-branch-anomaly",
            _ => "persistent-craft-branch-weapon",
        };
    }

    public static string? GetDisplayPrototypeId(PersistentCraftRecipePrototype recipe)
    {
        if (!string.IsNullOrWhiteSpace(recipe.DisplayProto))
            return recipe.DisplayProto;

        return recipe.Results.FirstOrDefault()?.Proto;
    }

    public static int GetPointReward(PersistentCraftRecipePrototype recipe)
    {
        if (recipe.PointReward > 0)
            return recipe.PointReward;

        return 1;
    }

    public static string GetTierDisplayLabel(int tier)
    {
        return tier switch
        {
            1 => "I",
            2 => "II",
            3 => "III",
            4 => "IV",
            5 => "V",
            _ => tier.ToString(),
        };
    }

}
