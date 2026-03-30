using System;
using System.Collections.Generic;
using Content.Shared._Stalker.PersistentCrafting;
using Robust.Shared.Log;
using Robust.Shared.Prototypes;

namespace Content.Server._Stalker.PersistentCrafting;

public sealed class PersistentCraftProfileService
{
    private readonly IPrototypeManager _prototype;
    private readonly PersistentCraftBranchRegistry _branchRegistry;
    private readonly IReadOnlyList<PersistentCraftNodePrototype> _nodeCache;
    private readonly ISawmill _sawmill;

    public PersistentCraftProfileService(
        IPrototypeManager prototype,
        PersistentCraftBranchRegistry branchRegistry,
        IReadOnlyList<PersistentCraftNodePrototype> nodeCache)
    {
        _prototype = prototype;
        _branchRegistry = branchRegistry;
        _nodeCache = nodeCache;
        _sawmill = Logger.GetSawmill("persistent-craft.profile");
    }

    public Dictionary<string, PersistentCraftBranchProfile> CreateDefaultBranchProfiles()
    {
        var result = new Dictionary<string, PersistentCraftBranchProfile>(_branchRegistry.OrderedBranchIds.Count);

        for (var i = 0; i < _branchRegistry.OrderedBranchIds.Count; i++)
        {
            var branch = _branchRegistry.OrderedBranchIds[i];
            result[branch] = new PersistentCraftBranchProfile();
        }

        return result;
    }

    public Dictionary<string, PersistentCraftBranchProfile> BuildBranchProfiles(IEnumerable<PersistentCraftBranchSaveData> branches)
    {
        var result = CreateDefaultBranchProfiles();

        foreach (var branch in branches)
        {
            if (string.IsNullOrWhiteSpace(branch.Branch) || !result.ContainsKey(branch.Branch))
                continue;

            result[branch.Branch] = new PersistentCraftBranchProfile
            {
                TotalEarnedPoints = Math.Max(0, branch.TotalEarnedPoints),
            };
        }

        return result;
    }

    public HashSet<string> SanitizeUnlockedNodes(IEnumerable<string> unlockedNodes, string characterName)
    {
        var sanitized = new HashSet<string>();

        foreach (var nodeId in unlockedNodes)
        {
            if (string.IsNullOrWhiteSpace(nodeId))
                continue;

            if (!_prototype.TryIndex<PersistentCraftNodePrototype>(nodeId, out _))
            {
                _sawmill.Warning($"[PersistentCraft] Missing node prototype '{nodeId}' in profile '{characterName}', removing stale unlock.");
                continue;
            }

            sanitized.Add(nodeId);
        }

        return sanitized;
    }

    public void EnsureAutoTierNodesUnlocked(PersistentCraftProfileComponent profile)
    {
        var changed = true;
        while (changed)
        {
            changed = false;

            for (var i = 0; i < _nodeCache.Count; i++)
            {
                var node = _nodeCache[i];
                if (!IsAutoUnlockedNode(node))
                    continue;

                if (!AreNodePrerequisitesMet(profile, node))
                    continue;

                if (profile.UnlockedNodes.Add(node.ID))
                    changed = true;
            }
        }
    }

    public bool HasNodeUnlockedOrAutoAvailable(PersistentCraftProfileComponent profile, string nodeId)
    {
        return PersistentCraftNodeRules.HasNodeUnlockedOrAutoAvailable(
            nodeId,
            profile.UnlockedNodes.Contains,
            ResolveNodePrototypeOrNull);
    }

    public bool AreNodePrerequisitesMet(PersistentCraftProfileComponent profile, PersistentCraftNodePrototype node)
    {
        return PersistentCraftNodeRules.ArePrerequisitesMet(
            node,
            profile.UnlockedNodes.Contains,
            ResolveNodePrototypeOrNull);
    }

    public int GetAvailableBranchPoints(PersistentCraftProfileComponent profile, string branch)
    {
        var branchProfile = GetOrCreateBranchProfile(profile, branch);
        var totalEarned = Math.Max(0, branchProfile.TotalEarnedPoints);
        var spent = GetSpentBranchPoints(profile, branch);
        return Math.Max(0, totalEarned - spent);
    }

    public int GetTotalEarnedBranchPoints(PersistentCraftProfileComponent profile, string branch)
    {
        return Math.Max(0, GetOrCreateBranchProfile(profile, branch).TotalEarnedPoints);
    }

    public int GetSpentBranchPoints(PersistentCraftProfileComponent profile, string branch)
    {
        var spent = 0;

        for (var i = 0; i < _nodeCache.Count; i++)
        {
            var node = _nodeCache[i];
            if (node.Branch != branch || node.Cost <= 0 || !profile.UnlockedNodes.Contains(node.ID))
                continue;

            spent += node.Cost;
        }

        return spent;
    }

    public List<PersistentCraftBranchState> BuildBranchStates(PersistentCraftProfileComponent profile)
    {
        var result = new List<PersistentCraftBranchState>(_branchRegistry.OrderedBranchIds.Count);

        for (var i = 0; i < _branchRegistry.OrderedBranchIds.Count; i++)
        {
            var branch = _branchRegistry.OrderedBranchIds[i];
            result.Add(new PersistentCraftBranchState(
                branch,
                GetAvailableBranchPoints(profile, branch),
                GetSpentBranchPoints(profile, branch)));
        }

        return result;
    }

    public void NormalizeBranchPoints(PersistentCraftProfileComponent profile)
    {
        for (var i = 0; i < _branchRegistry.OrderedBranchIds.Count; i++)
        {
            var branch = _branchRegistry.OrderedBranchIds[i];
            var branchProfile = GetOrCreateBranchProfile(profile, branch);
            branchProfile.TotalEarnedPoints = Math.Max(0, branchProfile.TotalEarnedPoints);
        }
    }

    public PersistentCraftBranchProfile GetOrCreateBranchProfile(PersistentCraftProfileComponent profile, string branch)
    {
        return GetOrCreateBranchProfile(profile.BranchProgress, branch);
    }

    public PersistentCraftBranchProfile GetOrCreateBranchProfile(
        Dictionary<string, PersistentCraftBranchProfile> branches,
        string branch)
    {
        if (!branches.TryGetValue(branch, out var profile))
        {
            profile = new PersistentCraftBranchProfile();
            branches[branch] = profile;
        }

        return profile;
    }

    private PersistentCraftNodePrototype? ResolveNodePrototypeOrNull(string nodeId)
    {
        return _prototype.TryIndex<PersistentCraftNodePrototype>(nodeId, out var node)
            ? node
            : null;
    }

    private static bool IsAutoUnlockedNode(PersistentCraftNodePrototype node)
    {
        return PersistentCraftingHelper.IsAutoUnlockedNode(node);
    }
}
