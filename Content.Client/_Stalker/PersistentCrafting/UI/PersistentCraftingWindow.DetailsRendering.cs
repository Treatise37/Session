using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;
using Content.Client._Stalker.PersistentCrafting.UI.Controls;
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
    private PanelContainer CreateDetailsPanel(
        PersistentCraftBranchState branchState,
        PersistentCraftNodePrototype node)
    {
        var state = _state ?? throw new InvalidOperationException("Persistent craft state is not initialized.");
        var unlocked = HasNodeUnlockedOrAutoAvailable(node.ID);
        var prerequisitesMet = ArePrerequisitesMet(node);
        var canUnlock = state.Loaded &&
                        !unlocked &&
                        prerequisitesMet &&
                        branchState.AvailablePoints >= node.Cost;
        var accent = GetBranchAccent(node.Branch);

        var panel = new PanelContainer
        {
            HorizontalExpand = true,
            VerticalExpand = false,
            PanelOverride = new StyleBoxFlat
            {
                BackgroundColor = PanelBackground,
                BorderColor = accent.WithAlpha(0.5f),
                BorderThickness = new Thickness(1),
                ContentMarginLeftOverride = 18,
                ContentMarginRightOverride = 18,
                ContentMarginTopOverride = 18,
                ContentMarginBottomOverride = 18,
            }
        };

        var body = new BoxContainer
        {
            Orientation = BoxContainer.LayoutOrientation.Vertical,
            HorizontalExpand = true,
            VerticalExpand = true,
        };

        var headerRow = new BoxContainer
        {
            Orientation = BoxContainer.LayoutOrientation.Horizontal,
            HorizontalExpand = true,
        };

        var headerLeft = new BoxContainer
        {
            Orientation = BoxContainer.LayoutOrientation.Vertical,
            MinSize = new Vector2(136, 0),
        };

        headerLeft.AddChild(CreateNodeIcon(node, accent, new Vector2(120, 120)));
        headerLeft.AddChild(new Control { MinSize = new Vector2(1, 8) });
        headerLeft.AddChild(new Label
        {
            Text = Loc.GetString("persistent-craft-node-branch-points", ("points", branchState.AvailablePoints)),
            FontColorOverride = MutedTextColor,
            HorizontalAlignment = HAlignment.Center,
        });
        headerLeft.AddChild(new Control { MinSize = new Vector2(1, 8) });

        var unlockButton = new Button
        {
            Text = GetActionText(unlocked),
            Disabled = !canUnlock,
            MinSize = new Vector2(0, 42),
            HorizontalExpand = true,
        };
        unlockButton.OnPressed += _ =>
        {
            _detailsDirty = true;
            _onUnlock?.Invoke(node.ID);
        };
        headerLeft.AddChild(unlockButton);

        headerRow.AddChild(headerLeft);
        headerRow.AddChild(new Control { MinSize = new Vector2(18, 1) });

        var headerText = new BoxContainer
        {
            Orientation = BoxContainer.LayoutOrientation.Vertical,
            HorizontalExpand = true,
        };

        headerText.AddChild(new Label
        {
            Text = ResolveNodeName(node),
            FontColorOverride = HeaderTextColor,
            ClipText = true,
        });
        headerText.AddChild(new Control { MinSize = new Vector2(1, 8) });

        var meta = new RichTextLabel
        {
            HorizontalExpand = true,
        };
        meta.SetMarkup(
            $"[color={MutedTextColor.ToHex()}]{Loc.GetString("persistent-craft-selected-branch", ("branch", ResolveBranchTitle(node.Branch)))}\n" +
            $"{Loc.GetString("persistent-craft-spent-points-label")}: {branchState.SpentPoints}\n" +
            $"{Loc.GetString("persistent-craft-node-cost", ("cost", node.Cost))} | " +
            $"{Loc.GetString(GetDetailStatusKey(unlocked, prerequisitesMet, canUnlock))}[/color]");
        headerText.AddChild(meta);
        headerRow.AddChild(headerText);

        body.AddChild(headerRow);
        body.AddChild(new Control { MinSize = new Vector2(1, 10) });

        body.AddChild(CreateDetailSection(
            Loc.GetString("persistent-craft-rewards-label"),
            BuildRewardMarkup(node)));
        body.AddChild(new Control { MinSize = new Vector2(1, 10) });
        body.AddChild(CreateDetailSection(
            Loc.GetString("persistent-craft-requirements-label"),
            BuildRequirementMarkup(node)));
        panel.AddChild(body);
        return panel;
    }

    private void ShowNodeDetailsWindow(PersistentCraftBranchState branchState, PersistentCraftNodePrototype node)
    {
        _detailsCoordinator.Show(
            ResolveNodeName(node),
            CreateDetailsPanel(branchState, node));
        MarkDetailsShown(branchState, node);
    }

    private void CloseNodeDetailsWindow()
    {
        _detailsCoordinator.Close();
        ResetDetailsCache();
    }

    private Control CreateDetailSection(string title, string contentMarkup)
    {
        var section = new PersistentCraftTextSection();
        section.SetData(title, contentMarkup, CardBorder, 12);
        return section;
    }

    private PanelContainer CreateNodeIcon(PersistentCraftNodePrototype node, Color accent, Vector2 size)
    {
        var panel = new PanelContainer
        {
            MinSize = size,
            VerticalExpand = false,
            HorizontalAlignment = HAlignment.Center,
            VerticalAlignment = VAlignment.Top,
            PanelOverride = new StyleBoxFlat
            {
                BackgroundColor = IconBackground,
                BorderColor = accent.WithAlpha(0.60f),
                BorderThickness = new Thickness(1),
                ContentMarginLeftOverride = 6,
                ContentMarginRightOverride = 6,
                ContentMarginTopOverride = 6,
                ContentMarginBottomOverride = 6,
            }
        };

        if (TryGetNodeTexture(node, out var texture))
        {
            var scale = size.X >= NodeIconLargeThreshold ? NodeIconScaleLarge : NodeIconScaleSmall;
            panel.AddChild(new TextureRect
            {
                Texture = texture,
                TextureScale = new Vector2(scale, scale),
                Stretch = TextureRect.StretchMode.KeepAspectCentered,
                HorizontalAlignment = HAlignment.Center,
                VerticalAlignment = VAlignment.Center,
            });
        }
        else
        {
            panel.AddChild(new Label
            {
                Text = ResolveNodeName(node),
                FontColorOverride = HeaderTextColor,
                HorizontalAlignment = HAlignment.Center,
                VerticalAlignment = VAlignment.Center,
                ClipText = true,
            });
        }

        return panel;
    }

    private string BuildRewardMarkup(PersistentCraftNodePrototype node)
    {
        var recipes = FindRecipesForNode(node);
        if (recipes.Count == 0)
            return $"[color={DescriptionTextColor.ToHex()}]{Loc.GetString("persistent-craft-none")}[/color]";

        var builder = new StringBuilder();
        for (var i = 0; i < recipes.Count; i++)
        {
            if (i > 0)
                builder.Append('\n');

            builder.Append($"[color={DescriptionTextColor.ToHex()}]- {FormattedMessage.EscapeText(ResolveRecipeName(recipes[i]))}[/color]");
        }

        return builder.ToString();
    }

    private string BuildRequirementMarkup(PersistentCraftNodePrototype node)
    {
        var lines = new List<string>();

        foreach (var prerequisiteId in node.Prerequisites)
        {
            if (!TryGetNodePrototype(prerequisiteId, out var prerequisite))
            {
                lines.Add($"[color={DescriptionTextColor.ToHex()}]- {FormattedMessage.EscapeText(prerequisiteId)}[/color]");
                continue;
            }

            lines.Add($"[color={DescriptionTextColor.ToHex()}]- {FormattedMessage.EscapeText(ResolveNodeName(prerequisite))}[/color]");
        }

        if (lines.Count == 0)
            return $"[color={DescriptionTextColor.ToHex()}]{Loc.GetString("persistent-craft-none")}[/color]";

        return string.Join("\n", lines);
    }

    private string GetDetailStatusKey(
        bool unlocked,
        bool prerequisitesMet,
        bool canUnlock)
    {
        if (_state?.Loaded != true)
            return "persistent-craft-node-status-loading";

        if (unlocked)
            return "persistent-craft-node-status-unlocked";

        if (canUnlock)
            return "persistent-craft-node-status-available";

        return prerequisitesMet
            ? "persistent-craft-node-status-not-enough-points"
            : "persistent-craft-node-status-locked";
    }

    private string GetActionText(bool unlocked)
    {
        if (_state?.Loaded != true)
            return Loc.GetString("persistent-craft-node-status-loading");

        if (unlocked)
            return Loc.GetString("persistent-craft-node-status-unlocked");

        return Loc.GetString("persistent-craft-node-action-unlock");
    }
}
