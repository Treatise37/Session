using System;

namespace Content.Shared._Stalker.PersistentCrafting;

public static class PersistentCraftRecipeRules
{
    public static float GetEffectiveCraftTime(PersistentCraftRecipePrototype recipe)
    {
        return MathF.Max(0.25f, recipe.CraftTime);
    }

    public static int GetEffectiveIngredientAmount(
        PersistentCraftRecipePrototype recipe,
        PersistentCraftIngredient ingredient)
    {
        _ = recipe;
        return Math.Max(1, ingredient.Amount);
    }
}
