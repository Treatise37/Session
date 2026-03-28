using System;
using System.Collections.Generic;
using Content.Shared._Stalker.PersistentCrafting;

namespace Content.Client._Stalker.PersistentCrafting;

public static class PersistentCraftNodeAvailabilityResolver
{
    public static bool HasNodeUnlockedOrAutoAvailable(
        PersistentCraftState state,
        string nodeId,
        Func<string, PersistentCraftNodePrototype?> resolveNode)
    {
        return HasNodeUnlockedOrAutoAvailable(state, nodeId, resolveNode, new HashSet<string>());
    }

    public static bool ArePrerequisitesMet(
        PersistentCraftState state,
        PersistentCraftNodePrototype node,
        Func<string, PersistentCraftNodePrototype?> resolveNode)
    {
        for (var i = 0; i < node.Prerequisites.Count; i++)
        {
            if (!HasNodeUnlockedOrAutoAvailable(state, node.Prerequisites[i], resolveNode))
                return false;
        }

        return true;
    }

    private static bool HasNodeUnlockedOrAutoAvailable(
        PersistentCraftState state,
        string nodeId,
        Func<string, PersistentCraftNodePrototype?> resolveNode,
        HashSet<string> path)
    {
        var node = resolveNode(nodeId);
        if (node == null)
            return false;

        if (state.UnlockedNodes.Contains(nodeId))
            return true;

        if (node.Cost > 0)
            return false;

        if (!path.Add(nodeId))
            return false;

        try
        {
            for (var i = 0; i < node.Prerequisites.Count; i++)
            {
                if (!HasNodeUnlockedOrAutoAvailable(state, node.Prerequisites[i], resolveNode, path))
                    return false;
            }

            return true;
        }
        finally
        {
            path.Remove(nodeId);
        }
    }
}
