using System;
using System.Collections.Generic;
using System.Numerics;
using Content.Client._Stalker.PersistentCrafting;
using Content.Client._Stalker.PersistentCrafting.UI.Indexes;
using Content.Client.Message;
using Content.Shared._Stalker.PersistentCrafting;
using Robust.Client.Graphics;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Maths;
using Robust.Shared.Utility;

namespace Content.Client._Stalker.PersistentCrafting.UI;

public sealed partial class PersistentCraftingWindow
{
    private BoxContainer CreateSubNodeTree(
        string branch,
        IReadOnlyList<PersistentCraftNodePrototype> subNodes,
        string? selectedNodeId)
    {
        var accent = GetBranchAccent(branch);
        var layoutData = PersistentCraftNodeTreeLayoutBuilder.Build(subNodes);
        var positions = layoutData.Positions;

        var layout = new LayoutContainer
        {
            MinSize = new Vector2(
                TierTreePadding * 2 + (layoutData.MaxColumn + 1) * TierTreeNodeWidth + layoutData.MaxColumn * TierTreeHorizontalGap,
                TierTreePadding * 2 + (layoutData.MaxRow + 1) * TierTreeNodeHeight + layoutData.MaxRow * TierTreeVerticalGap + 20),
            HorizontalAlignment = HAlignment.Center,
        };

        foreach (var node in subNodes)
        {
            var childPosition = GetNodeCanvasPosition(positions[node.ID]);
            var childCenter = GetNodeCenter(childPosition);

            foreach (var prerequisiteId in node.Prerequisites)
            {
                if (!layoutData.ContainsNode(prerequisiteId))
                    continue;

                if (!positions.TryGetValue(prerequisiteId, out var parentGridPosition))
                    continue;

                var parentPosition = GetNodeCanvasPosition(parentGridPosition);
                var parentCenter = GetNodeCenter(parentPosition);
                AddConnector(layout, parentCenter, childCenter, accent);
            }
        }

        foreach (var node in subNodes)
        {
            var control = CreateSubNodeEntry(branch, node, selectedNodeId == node.ID);
            control.MinSize = new Vector2(TierTreeNodeWidth, TierTreeNodeHeight);
            control.MaxSize = new Vector2(TierTreeNodeWidth, TierTreeNodeHeight);
            LayoutContainer.SetPosition(control, GetNodeCanvasPosition(positions[node.ID]));
            layout.AddChild(control);
        }

        var wrapper = new BoxContainer
        {
            Orientation = BoxContainer.LayoutOrientation.Vertical,
            HorizontalExpand = true,
            HorizontalAlignment = HAlignment.Center,
        };
        wrapper.AddChild(layout);
        return wrapper;
    }

    private static Vector2 GetNodeCanvasPosition(Vector2i gridPosition)
    {
        return new Vector2(
            TierTreePadding + gridPosition.X * (TierTreeNodeWidth + TierTreeHorizontalGap),
            TierTreePadding + gridPosition.Y * (TierTreeNodeHeight + TierTreeVerticalGap));
    }

    private static Vector2 GetNodeCenter(Vector2 canvasPosition)
    {
        return new Vector2(
            canvasPosition.X + TierTreeNodeWidth / 2f,
            canvasPosition.Y + TierTreeNodeHeight / 2f);
    }

    private static void AddConnector(LayoutContainer layout, Vector2 parentCenter, Vector2 childCenter, Color accent)
    {
        var connectorColor = accent.WithAlpha(0.35f);
        var parentBottom = parentCenter.Y + TierTreeNodeHeight / 2f;
        var childTop = childCenter.Y - TierTreeNodeHeight / 2f;
        var midY = parentBottom + (childTop - parentBottom) / 2f;

        AddLine(layout, parentCenter.X, parentBottom, parentCenter.X, midY, connectorColor);
        AddLine(layout, Math.Min(parentCenter.X, childCenter.X), midY, Math.Max(parentCenter.X, childCenter.X), midY, connectorColor);
        AddLine(layout, childCenter.X, midY, childCenter.X, childTop, connectorColor);
    }

    private static void AddLine(LayoutContainer layout, float startX, float startY, float endX, float endY, Color color)
    {
        var isVertical = Math.Abs(startX - endX) < 0.01f;
        var minX = isVertical
            ? startX - TierTreeLineThickness / 2f
            : Math.Min(startX, endX);
        var minY = isVertical
            ? Math.Min(startY, endY)
            : startY - TierTreeLineThickness / 2f;
        var width = isVertical
            ? TierTreeLineThickness
            : Math.Max(Math.Abs(endX - startX), TierTreeLineThickness);
        var height = isVertical
            ? Math.Max(Math.Abs(endY - startY), TierTreeLineThickness)
            : TierTreeLineThickness;

        var line = new PanelContainer
        {
            MinSize = new Vector2(width, height),
            PanelOverride = new StyleBoxFlat
            {
                BackgroundColor = color,
            }
        };

        LayoutContainer.SetPosition(line, new Vector2(minX, minY));
        layout.AddChild(line);
    }

    private ContainerButton CreateSubNodeEntry(string branch, PersistentCraftNodePrototype node, bool selected)
    {
        var state = _state ?? throw new InvalidOperationException("Persistent craft state is not initialized.");
        var branchState = _branchCoordinator.GetBranchState(branch);
        var unlocked = HasNodeUnlockedOrAutoAvailable(node.ID);
        var prerequisitesMet = ArePrerequisitesMet(node);
        var canUnlock = state.Loaded && !unlocked && prerequisitesMet && branchState.AvailablePoints >= node.Cost;
        var accent = GetBranchAccent(branch);

        var button = new ContainerButton
        {
            MinSize = new Vector2(TierTreeNodeWidth, TierTreeNodeHeight),
            MaxSize = new Vector2(TierTreeNodeWidth, TierTreeNodeHeight),
            HorizontalExpand = false,
            VerticalExpand = false,
            StyleBoxOverride = new StyleBoxFlat
            {
                BackgroundColor = unlocked ? CardUnlockedBackground : canUnlock ? CardAvailableBackground : CardLockedBackground,
                BorderColor = selected ? SelectedBorder : unlocked ? UnlockedBorder : canUnlock ? accent.WithAlpha(0.6f) : CardBorder,
                BorderThickness = new Thickness(selected ? 2 : 1),
                ContentMarginLeftOverride = 12,
                ContentMarginRightOverride = 12,
                ContentMarginTopOverride = 12,
                ContentMarginBottomOverride = 12,
            }
        };
        button.OnPressed += _ => SelectNode(branch, node.ID);

        var body = new BoxContainer
        {
            Orientation = BoxContainer.LayoutOrientation.Vertical,
            HorizontalExpand = true,
            VerticalExpand = true,
        };

        body.AddChild(CreateNodeIcon(node, selected ? SelectedBorder : accent, new Vector2(64, 64)));
        body.AddChild(new Control { MinSize = new Vector2(1, 8) });

        var namePlate = new PanelContainer
        {
            HorizontalExpand = true,
            MinSize = new Vector2(0, 26),
            PanelOverride = new StyleBoxFlat
            {
                BackgroundColor = IconBackground.WithAlpha(0.45f),
                BorderColor = CardBorder.WithAlpha(0.55f),
                BorderThickness = new Thickness(1),
                ContentMarginLeftOverride = 4,
                ContentMarginRightOverride = 4,
                ContentMarginTopOverride = 2,
                ContentMarginBottomOverride = 2,
            }
        };

        var nameLabel = new RichTextLabel
        {
            HorizontalExpand = true,
            VerticalAlignment = VAlignment.Center,
        };
        nameLabel.SetMarkup($"[color={HeaderTextColor.ToHex()}]{FormattedMessage.EscapeText(ResolveNodeCardCaption(node))}[/color]");
        namePlate.AddChild(nameLabel);
        body.AddChild(namePlate);
        body.AddChild(new Control { VerticalExpand = true });

        button.AddChild(body);
        return button;
    }

    private bool ArePrerequisitesMet(PersistentCraftNodePrototype node)
    {
        var state = _state ?? throw new InvalidOperationException("Persistent craft state is not initialized.");
        return PersistentCraftNodeAvailabilityResolver.ArePrerequisitesMet(state, node, ResolveNodePrototypeOrNull);
    }
}
