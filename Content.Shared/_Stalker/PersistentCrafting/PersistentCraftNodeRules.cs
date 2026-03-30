using System;
using System.Collections.Generic;

namespace Content.Shared._Stalker.PersistentCrafting;

public static class PersistentCraftNodeRules
{
    public static bool HasNodeUnlockedOrAutoAvailable(
        string nodeId,
        Func<string, bool> isUnlocked,
        Func<string, PersistentCraftNodePrototype?> resolveNode)
    {
        return HasNodeUnlockedOrAutoAvailable(nodeId, isUnlocked, resolveNode, new HashSet<string>());
    }

    public static bool ArePrerequisitesMet(
        PersistentCraftNodePrototype node,
        Func<string, bool> isUnlocked,
        Func<string, PersistentCraftNodePrototype?> resolveNode)
    {
        for (var i = 0; i < node.Prerequisites.Count; i++)
        {
            if (!HasNodeUnlockedOrAutoAvailable(node.Prerequisites[i], isUnlocked, resolveNode))
                return false;
        }

        return true;
    }

    private static bool HasNodeUnlockedOrAutoAvailable(
        string nodeId,
        Func<string, bool> isUnlocked,
        Func<string, PersistentCraftNodePrototype?> resolveNode,
        HashSet<string> path)
    {
        var node = resolveNode(nodeId);
        if (node == null)
            return false;

        if (isUnlocked(nodeId))
            return true;

        if (!PersistentCraftingHelper.IsAutoUnlockedNode(node))
            return false;

        if (!path.Add(nodeId))
            return false;

        try
        {
            for (var i = 0; i < node.Prerequisites.Count; i++)
            {
                if (!HasNodeUnlockedOrAutoAvailable(node.Prerequisites[i], isUnlocked, resolveNode, path))
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
