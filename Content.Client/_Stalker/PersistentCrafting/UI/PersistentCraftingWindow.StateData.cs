using System;
using System.Collections.Generic;
using Content.Client._Stalker.PersistentCrafting;
using Content.Shared._Stalker.PersistentCrafting;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Shared.GameObjects;
using Robust.Shared.Maths;
using Robust.Shared.Prototypes;

namespace Content.Client._Stalker.PersistentCrafting.UI;

public sealed partial class PersistentCraftingWindow
{
    private IReadOnlyList<PersistentCraftRecipePrototype> FindRecipesForNode(PersistentCraftNodePrototype node)
    {
        if (_recipeIndex != null)
        {
            return _recipeIndex.GetByRequiredNode(node.ID);
        }

        if (_prototypeCache != null &&
            _prototypeCache.RecipesByNode.TryGetValue(node.ID, out var cacheRecipes))
        {
            return cacheRecipes;
        }

        var recipes = new List<PersistentCraftRecipePrototype>();
        for (var i = 0; i < _recipes.Count; i++)
        {
            var recipe = _recipes[i];
            if (recipe.RequiredNode == node.ID)
                recipes.Add(recipe);
        }

        return recipes;
    }

    private bool TryGetNodeTexture(PersistentCraftNodePrototype node, out Texture? texture)
    {
        texture = null;

        var displayProto = node.DisplayProto;
        if (string.IsNullOrWhiteSpace(displayProto))
        {
            var recipes = FindRecipesForNode(node);
            if (recipes.Count > 0)
                displayProto = PersistentCraftingHelper.GetDisplayPrototypeId(recipes[0]);
        }

        if (string.IsNullOrWhiteSpace(displayProto))
            return false;

        try
        {
            var spriteSystem = _entityManager.EntitySysManager.GetEntitySystem<SpriteSystem>();
            texture = spriteSystem.GetPrototypeIcon(displayProto).Default;
            return texture != null;
        }
        catch
        {
            texture = null;
            return false;
        }
    }

    private string ResolveRecipeName(PersistentCraftRecipePrototype recipe)
    {
        var displayProto = PersistentCraftingHelper.GetDisplayPrototypeId(recipe);
        if (!string.IsNullOrWhiteSpace(displayProto) &&
            _prototype.TryIndex<EntityPrototype>(displayProto, out var prototype))
        {
            return prototype.Name;
        }

        return Loc.GetString(recipe.Name);
    }

    private string ResolveNodeName(PersistentCraftNodePrototype node)
    {
        if (!string.IsNullOrWhiteSpace(node.Name))
        {
            if (Loc.TryGetString(node.Name, out var localizedNodeName) &&
                !string.IsNullOrWhiteSpace(localizedNodeName))
            {
                return localizedNodeName;
            }

            if (!string.IsNullOrWhiteSpace(node.Name))
                return node.Name;
        }

        if (!string.IsNullOrWhiteSpace(node.DisplayProto) &&
            _prototype.TryIndex<EntityPrototype>(node.DisplayProto, out var prototype))
        {
            if (!string.IsNullOrWhiteSpace(prototype.Name))
            {
                return prototype.Name;
            }

            if (Loc.TryGetString($"ent-{prototype.ID}", out var localizedPrototypeName) &&
                !string.IsNullOrWhiteSpace(localizedPrototypeName))
            {
                return localizedPrototypeName;
            }

            return prototype.ID;
        }

        if (!string.IsNullOrWhiteSpace(node.DisplayProto))
            return node.DisplayProto;

        return node.ID;
    }

    private string ResolveNodeCardCaption(PersistentCraftNodePrototype node)
    {
        var resolved = ResolveNodeName(node).Trim();
        if (!string.IsNullOrWhiteSpace(resolved))
            return resolved;

        if (!string.IsNullOrWhiteSpace(node.DisplayProto))
            return node.DisplayProto;

        return node.ID;
    }

    private bool HasNodeUnlockedOrAutoAvailable(string nodeId)
    {
        if (_state == null)
            return false;

        return PersistentCraftNodeAvailabilityResolver.HasNodeUnlockedOrAutoAvailable(
            _state,
            nodeId,
            ResolveNodePrototypeOrNull);
    }

    private Color GetBranchAccent(string branch)
    {
        return _branchRegistry.TryGetBranchDefinition(branch, out var definition)
            ? definition.AccentColor
            : Color.White;
    }

    private string ResolveBranchTitle(string branchId)
    {
        return _branchRegistry.TryGetBranchDefinition(branchId, out var definition)
            ? ResolveBranchTitle(definition)
            : branchId;
    }

    private IReadOnlyList<PersistentCraftNodePrototype> GetNodesForBranch(string branch)
    {
        if (_prototypeCache != null &&
            _prototypeCache.NodesByBranch.TryGetValue(branch, out var nodes))
        {
            return nodes;
        }

        var filtered = new List<PersistentCraftNodePrototype>();
        for (var i = 0; i < _nodes.Count; i++)
        {
            var node = _nodes[i];
            if (node.Branch == branch)
                filtered.Add(node);
        }

        return filtered;
    }

    private bool TryGetNodePrototype(string nodeId, out PersistentCraftNodePrototype node)
    {
        if (_prototypeCache != null &&
            _prototypeCache.TryGetNode(nodeId, out node))
        {
            return true;
        }

        if (_prototype.TryIndex<PersistentCraftNodePrototype>(nodeId, out var resolvedNode) &&
            resolvedNode != null)
        {
            node = resolvedNode;
            return true;
        }

        node = default!;
        return false;
    }

    private PersistentCraftNodePrototype? ResolveNodePrototypeOrNull(string nodeId)
    {
        return TryGetNodePrototype(nodeId, out var node)
            ? node
            : null;
    }

    private static string ResolveBranchTitle(PersistentCraftBranchPrototype definition)
    {
        try
        {
            return Loc.GetString(definition.Name);
        }
        catch
        {
            return definition.Name;
        }
    }
}
