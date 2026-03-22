using System;
using System.Linq;

namespace Content.Shared._Stalker.PersistentCrafting;

public static class PersistentCraftingHelper
{
    public const int InitialLevel = 1;
    public const int InitialSubLevel = 1;
    public const int MainTierSubLevel = 0;
    public const int InitialNodeMasteryLevel = 1;
    public const int MaxNodeMasteryLevel = 3;
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

    public static string BuildNodeId(PersistentCraftBranch branch, int tier, int subTier = MainTierSubLevel)
    {
        var prefix = branch switch
        {
            PersistentCraftBranch.Weapon => "PersistentCraftWeapon",
            PersistentCraftBranch.Armor => "PersistentCraftArmor",
            PersistentCraftBranch.Anomaly => "PersistentCraftAnomaly",
            _ => "PersistentCraftWeapon",
        };

        return subTier <= MainTierSubLevel
            ? $"{prefix}T{tier}"
            : $"{prefix}T{tier}S{subTier}";
    }

    public static string BuildMainTierNodeId(PersistentCraftBranch branch, int tier)
    {
        return BuildNodeId(branch, tier, MainTierSubLevel);
    }

    public static bool IsMainTierNode(PersistentCraftNodePrototype node)
    {
        return node.NodeType == PersistentCraftNodeType.MainTier || node.SubTier <= MainTierSubLevel;
    }

    public static int GetNodeRequiredBranchLevel(PersistentCraftNodePrototype node)
    {
        return node.RequiredBranchLevel > 0 ? node.RequiredBranchLevel : node.Tier;
    }

    public static int GetAffectedTier(PersistentCraftNodePrototype node)
    {
        return node.AffectedTier > 0 ? node.AffectedTier : node.Tier;
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

    public static int GetExperienceReward(PersistentCraftRecipePrototype recipe)
    {
        return recipe.ExperienceReward > 0
            ? recipe.ExperienceReward
            : recipe.Tier * 25;
    }

    public static int GetExperienceForNextLevel(int level)
    {
        return Math.Max(100, level * 100);
    }

    public static int GetNodeExperienceReward(PersistentCraftRecipePrototype recipe)
    {
        return Math.Max(10, GetExperienceReward(recipe) / 2);
    }

    public static int GetNodeExperienceForNextLevel(PersistentCraftNodePrototype node, int masteryLevel)
    {
        if (masteryLevel >= MaxNodeMasteryLevel)
            return 0;

        return 30 + node.Tier * 20 + Math.Max(0, masteryLevel - InitialNodeMasteryLevel) * 25;
    }

    public static string FormatLevel(int level, int subLevel = MainTierSubLevel)
    {
        var normalizedLevel = Math.Max(InitialLevel, level);
        if (subLevel <= MainTierSubLevel)
            return normalizedLevel.ToString();

        return $"{normalizedLevel}.{Math.Max(InitialSubLevel, subLevel)}";
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

    public static string GetNodeLevelText(PersistentCraftNodePrototype node)
    {
        if (!string.IsNullOrWhiteSpace(node.DisplayLabel))
            return node.DisplayLabel;

        if (IsMainTierNode(node))
            return GetTierDisplayLabel(node.Tier);

        return FormatLevel(node.Tier, node.SubTier);
    }

    public static string GetRecipeLevelText(PersistentCraftRecipePrototype recipe)
    {
        return FormatLevel(recipe.Tier, recipe.SubTier);
    }
}
