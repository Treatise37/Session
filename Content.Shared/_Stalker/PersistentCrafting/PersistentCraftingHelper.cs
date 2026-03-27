using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Robust.Shared.Maths;
using Robust.Shared.Prototypes;

namespace Content.Shared._Stalker.PersistentCrafting;

public static class PersistentCraftingHelper
{
    public static IReadOnlyList<PersistentCraftBranchPrototype> EnumerateBranchDefinitions(IPrototypeManager prototype)
    {
        return prototype.EnumeratePrototypes<PersistentCraftBranchPrototype>()
            .OrderBy(branch => branch.Order)
            .ThenBy(branch => branch.ID)
            .ToList();
    }

    public static IReadOnlyList<string> EnumerateBranches(IPrototypeManager prototype)
    {
        return EnumerateBranchDefinitions(prototype)
            .Select(branch => branch.ID)
            .ToList();
    }

    public static string GetFirstBranch(IPrototypeManager prototype)
    {
        return EnumerateBranchDefinitions(prototype).FirstOrDefault()?.ID ?? string.Empty;
    }

    public static bool TryGetBranchDefinition(
        IPrototypeManager prototype,
        string branchId,
        out PersistentCraftBranchPrototype definition)
    {
        if (!string.IsNullOrWhiteSpace(branchId) &&
            prototype.TryIndex<PersistentCraftBranchPrototype>(branchId, out var found) &&
            found != null)
        {
            definition = found;
            return true;
        }

        definition = default!;
        return false;
    }

    public static int GetBranchIndex(IPrototypeManager prototype, string branchId)
    {
        var branches = EnumerateBranchDefinitions(prototype);
        for (var i = 0; i < branches.Count; i++)
        {
            if (string.Equals(branches[i].ID, branchId, StringComparison.Ordinal))
                return i;
        }

        throw new ArgumentOutOfRangeException(nameof(branchId), branchId, "Unknown persistent craft branch.");
    }

    public static bool TryGetBranchByIndex(IPrototypeManager prototype, int index, out string branchId)
    {
        var branches = EnumerateBranchDefinitions(prototype);
        if ((uint) index < (uint) branches.Count)
        {
            branchId = branches[index].ID;
            return true;
        }

        branchId = string.Empty;
        return false;
    }

    public static string GetBranchName(IPrototypeManager prototype, string branchId)
    {
        return TryGetBranchDefinition(prototype, branchId, out var definition)
            ? definition.Name
            : branchId;
    }

    public static string GetDefaultCategoryId(IPrototypeManager prototype, string branchId)
    {
        return TryGetBranchDefinition(prototype, branchId, out var definition)
            ? definition.DefaultCategory
            : string.Empty;
    }

    public static Color GetBranchAccent(IPrototypeManager prototype, string branchId)
    {
        return TryGetBranchDefinition(prototype, branchId, out var definition)
            ? definition.AccentColor
            : Color.White;
    }

    public static bool UsesTierSubcategories(IPrototypeManager prototype, string branchId)
    {
        return TryGetBranchDefinition(prototype, branchId, out var definition) && definition.UseTierSubcategories;
    }

    public static int CompareBranches(IPrototypeManager prototype, string? left, string? right)
    {
        var leftOrder = GetBranchOrder(prototype, left);
        var rightOrder = GetBranchOrder(prototype, right);
        return leftOrder != rightOrder
            ? leftOrder.CompareTo(rightOrder)
            : string.Compare(left, right, StringComparison.Ordinal);
    }

    private static int GetBranchOrder(IPrototypeManager prototype, string? branchId)
    {
        if (string.IsNullOrWhiteSpace(branchId))
            return int.MaxValue;

        return TryGetBranchDefinition(prototype, branchId, out var definition)
            ? definition.Order
            : int.MaxValue;
    }

    public static string? GetDisplayPrototypeId(PersistentCraftRecipePrototype recipe)
    {
        if (!string.IsNullOrWhiteSpace(recipe.DisplayProto))
            return recipe.DisplayProto;

        return recipe.Results.FirstOrDefault()?.Proto;
    }

    public static bool IsAutoUnlockedNode(PersistentCraftNodePrototype node)
    {
        return node.Cost <= 0;
    }

    public static int GetPointReward(PersistentCraftRecipePrototype recipe)
    {
        return recipe.PointReward > 0 ? recipe.PointReward : 1;
    }

    public static string GetTierDisplayLabel(int tier)
    {
        return tier > 0 ? ToRoman(tier) : tier.ToString();
    }

    private static string ToRoman(int number)
    {
        var map = new (int Value, string Symbol)[]
        {
            (1000, "M"),
            (900, "CM"),
            (500, "D"),
            (400, "CD"),
            (100, "C"),
            (90, "XC"),
            (50, "L"),
            (40, "XL"),
            (10, "X"),
            (9, "IX"),
            (5, "V"),
            (4, "IV"),
            (1, "I"),
        };

        var result = new StringBuilder();
        foreach (var (value, symbol) in map)
        {
            while (number >= value)
            {
                result.Append(symbol);
                number -= value;
            }
        }

        return result.ToString();
    }
}
