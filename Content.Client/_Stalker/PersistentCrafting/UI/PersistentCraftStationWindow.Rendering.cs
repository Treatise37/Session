using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Content.Client._Stalker.PersistentCrafting.UI.Controls;
using Content.Client.Message;
using Content.Shared._Stalker.PersistentCrafting;
using Robust.Client.Graphics;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Maths;
using Robust.Shared.Utility;

namespace Content.Client._Stalker.PersistentCrafting.UI;

public sealed partial class PersistentCraftStationWindow
{
    private BoxContainer CreateTierFilterRow(
        string branch,
        IReadOnlyList<PersistentCraftRecipePrototype> recipes,
        int selectedTier,
        Color accent)
    {
        var row = new BoxContainer
        {
            Orientation = BoxContainer.LayoutOrientation.Horizontal,
            HorizontalExpand = true,
        };

        row.AddChild(CreateTierFilterButton(branch, 0, Loc.GetString("persistent-craft-filter-all"), selectedTier == 0, accent));

        foreach (var tier in recipes.Select(recipe => recipe.Tier).Distinct().OrderBy(tier => tier))
        {
            row.AddChild(new Control { MinSize = new Vector2(6, 1) });
            row.AddChild(CreateTierFilterButton(
                branch,
                tier,
                PersistentCraftingHelper.GetTierDisplayLabel(tier),
                selectedTier == tier,
                accent));
        }

        return row;
    }

    private Button CreateTierFilterButton(
        string branch,
        int tier,
        string text,
        bool selected,
        Color accent)
    {
        var button = new Button
        {
            Text = text,
            MinSize = new Vector2(42, 28),
            ModulateSelfOverride = selected ? PersistentCraftUiTheme.TextPrimary : PersistentCraftUiTheme.TextSecondary,
            StyleBoxOverride = new StyleBoxFlat
            {
                BackgroundColor = selected ? accent.WithAlpha(0.12f) : PersistentCraftUiTheme.SurfacePanelSoft,
                BorderColor = selected ? accent.WithAlpha(0.72f) : CardMutedBorder,
                BorderThickness = new Thickness(1),
                ContentMarginLeftOverride = 10,
                ContentMarginRightOverride = 10,
                ContentMarginTopOverride = 5,
                ContentMarginBottomOverride = 5,
            }
        };

        button.OnPressed += _ => SelectTierFilter(branch, tier);
        return button;
    }

    private Control CreateBranchHeader(
        string branch,
        PersistentCraftBranchState branchState,
        int unlockedRecipes,
        int totalRecipes)
    {
        var accent = GetAccent(branch);
        var unlockProgress = totalRecipes == 0
            ? 0f
            : unlockedRecipes / (float) totalRecipes;
        var control = new PersistentCraftBranchSummaryBlock();
        control.SetData(
            ResolveBranchTitle(branch),
            $"{Loc.GetString("persistent-craft-branch-points-label")}: {branchState.AvailablePoints} | {Loc.GetString("persistent-craft-spent-points-label")}: {branchState.SpentPoints} | " +
            $"{Loc.GetString("persistent-craft-recipes-short")}: {unlockedRecipes}/{totalRecipes}",
            Loc.GetString("persistent-craft-unlocked-summary", ("unlocked", unlockedRecipes)),
            unlockProgress,
            accent,
            null);
        return control;
    }

    private Control CreateFilterToolbar(string branch, string searchText, bool craftableOnly)
    {
        var accent = GetAccent(branch);
        var toolbar = new PersistentCraftRecipeToolbar();
        toolbar.SetData(
            Loc.GetString("persistent-craft-search-placeholder"),
            searchText,
            Loc.GetString("persistent-craft-filter-craftable"),
            craftableOnly,
            accent);
        toolbar.SearchInput.OnTextChanged += args => UpdateSearch(branch, args.Text);
        toolbar.CraftableToggleButton.OnPressed += _ => ToggleCraftableOnly(branch);
        return toolbar;
    }

    private Control CreateRecipeListSummary(int selectedTier, int visibleRecipes, int craftableCount, bool craftableOnly, Color accent)
    {
        var tierText = selectedTier > 0
            ? $"{Loc.GetString("persistent-craft-level-label")} {PersistentCraftingHelper.GetTierDisplayLabel(selectedTier)}"
            : Loc.GetString("persistent-craft-filter-all");
        var craftableText = $"{Loc.GetString("persistent-craft-craftable-short")}: {craftableCount}";
        var filterText = craftableOnly ? $" | {Loc.GetString("persistent-craft-filter-craftable")}" : string.Empty;

        var summary = new PersistentCraftRecipeListSummary();
        summary.SetData(
            $"{tierText} | {Loc.GetString("persistent-craft-recipes-short")}: {visibleRecipes} | {craftableText}{filterText}",
            accent);
        return summary;
    }

    private Control CreateEmptyRecipeListMessage(string searchText, bool craftableOnly)
    {
        var text = craftableOnly
            ? string.IsNullOrWhiteSpace(searchText)
                ? Loc.GetString("persistent-craft-empty-craftable")
                : Loc.GetString("persistent-craft-search-empty-craftable")
            : string.IsNullOrWhiteSpace(searchText)
                ? Loc.GetString("persistent-craft-station-no-recipes")
                : Loc.GetString("persistent-craft-search-empty");

        return new Label
        {
            Text = text,
            FontColorOverride = MutedText,
        };
    }

    private PanelContainer CreateEmptyDetailPanel(string searchText, bool craftableOnly)
    {
        var panel = new PanelContainer
        {
            HorizontalExpand = true,
            VerticalExpand = true,
            PanelOverride = new StyleBoxFlat
            {
                BackgroundColor = WindowBackground,
                BorderColor = CardMutedBorder,
                BorderThickness = new Thickness(1),
                ContentMarginLeftOverride = 18,
                ContentMarginRightOverride = 18,
                ContentMarginTopOverride = 18,
                ContentMarginBottomOverride = 18,
            }
        };

        var text = craftableOnly
            ? string.IsNullOrWhiteSpace(searchText)
                ? Loc.GetString("persistent-craft-empty-craftable-detail")
                : Loc.GetString("persistent-craft-search-empty-craftable-detail")
            : string.IsNullOrWhiteSpace(searchText)
                ? Loc.GetString("persistent-craft-empty-detail")
                : Loc.GetString("persistent-craft-search-empty-detail");

        panel.AddChild(new Label
        {
            Text = text,
            FontColorOverride = MutedText,
        });

        return panel;
    }

    private BoxContainer CreateRecipeList(
        string branch,
        IReadOnlyList<PersistentCraftRecipePrototype> recipes,
        PersistentCraftRecipePrototype selected,
        Color accent)
    {
        var list = new BoxContainer
        {
            Orientation = BoxContainer.LayoutOrientation.Vertical,
            HorizontalExpand = true,
        };

        foreach (var tierGroup in recipes.GroupBy(recipe => recipe.Tier).OrderBy(group => group.Key))
        {
            list.AddChild(CreateTierHeader(tierGroup.Key, accent));
            list.AddChild(new Control { MinSize = new Vector2(1, 6) });

            foreach (var categoryGroup in tierGroup
                         .GroupBy(GetRecipeCategoryId)
                         .OrderBy(group => GetCategoryOrder(group.Key)))
            {
                var categoryKey = BuildCategoryGroupKey(branch, tierGroup.Key, categoryGroup.Key);
                EnsureCategoryCollapsedByDefault(categoryKey);
                var categoryCollapsed = _viewModel.CollapsedCategoryKeys.Contains(categoryKey);
                list.AddChild(CreateCategoryHeader(GetCategoryName(categoryGroup.Key), accent, categoryGroup.Count(), categoryCollapsed, categoryKey));
                list.AddChild(new Control { MinSize = new Vector2(1, 4) });

                if (categoryCollapsed)
                    continue;

                foreach (var subCategoryGroup in categoryGroup
                             .GroupBy(GetRecipeSubCategoryId)
                             .OrderBy(group => GetSubCategoryOrder(group.Key)))
                {
                    var subCategoryId = subCategoryGroup.Key;
                    if (!string.IsNullOrWhiteSpace(subCategoryId))
                    {
                        var subCategoryKey = BuildSubCategoryGroupKey(branch, tierGroup.Key, categoryGroup.Key, subCategoryId);
                        EnsureSubCategoryCollapsedByDefault(subCategoryKey);
                        var subCategoryCollapsed = _viewModel.CollapsedSubCategoryKeys.Contains(subCategoryKey);
                        list.AddChild(CreateSubCategoryHeader(GetSubCategoryName(subCategoryId), subCategoryGroup.Count(), subCategoryCollapsed, subCategoryKey));
                        list.AddChild(new Control { MinSize = new Vector2(1, 4) });

                        if (subCategoryCollapsed)
                            continue;
                    }

                    foreach (var recipe in subCategoryGroup.OrderBy(item => item.Tier).ThenBy(item => item.ID))
                    {
                        list.AddChild(CreateRecipeListEntry(branch, recipe, selected.ID == recipe.ID));
                    }
                }
            }
        }

        return list;
    }

    private Control CreateTierHeader(int tier, Color accent)
    {
        var header = new PersistentCraftTierHeader();
        header.SetData(
            $"{Loc.GetString("persistent-craft-level-label")} {PersistentCraftingHelper.GetTierDisplayLabel(tier)}",
            accent);
        return header;
    }

    private Control CreateCategoryHeader(string text, Color accent, int count, bool collapsed, string categoryKey)
    {
        var header = new PersistentCraftCollapsibleHeader();
        header.SetData(
            text,
            count,
            collapsed,
            accent,
            PersistentCraftUiTheme.SurfacePanelAlt,
            accent.WithAlpha(0.28f),
            new Thickness(0, 0, 0, 6),
            5);
        header.Button.OnPressed += _ => ToggleCategoryCollapse(categoryKey);
        return header;
    }

    private Control CreateSubCategoryHeader(string text, int count, bool collapsed, string subCategoryKey)
    {
        var header = new PersistentCraftCollapsibleHeader();
        header.SetData(
            text,
            count,
            collapsed,
            MutedText,
            PersistentCraftUiTheme.SurfacePanelSoft,
            CardMutedBorder,
            new Thickness(0),
            4);
        header.Button.OnPressed += _ => ToggleSubCategoryCollapse(subCategoryKey);
        return header;
    }

    private Control CreateRecipeListEntry(string branch, PersistentCraftRecipePrototype recipe, bool selected)
    {
        var accent = GetAccent(recipe.Branch);
        var card = new PersistentCraftRecipeCard();
        card.Margin = new Thickness(0, 0, 0, 6);
        card.SetData(
            ResolveRecipeName(recipe),
            BuildRecipeSecondaryLine(recipe),
            CanViewRecipe(recipe) ? Color.White : MutedText,
            MutedText);
        card.Button.OnPressed += _ => SelectRecipe(recipe.Branch, recipe.ID);
        card.StatusContainer.AddChild(CreateRecipeStatusBadge(recipe));
        card.IconHost.AddChild(CreateRecipeIconContent(recipe, new Vector2(48, 48)));

        ApplyRecipeEntryVisuals(card.Button, card.IconHost, recipe, selected);
        _recipeEntryControlsByBranch[branch][recipe.ID] = new RecipeEntryControls(card.Button, card.IconHost);

        return card;
    }

    private PanelContainer CreateRecipeDetailsPanel(
        PersistentCraftRecipePrototype recipe,
        PersistentCraftBranchState branchState)
    {
        var loaded = _state?.Loaded == true;
        var requirementMet = _state != null && HasRequirement(_state, recipe);
        var hasMaterials = GetHasLocalMaterials(recipe);
        var canCraft = loaded && requirementMet && hasMaterials;
        var accent = GetAccent(recipe.Branch);

        var panel = new PanelContainer
        {
            HorizontalExpand = true,
            VerticalExpand = true,
            PanelOverride = new StyleBoxFlat
            {
                BackgroundColor = PersistentCraftUiTheme.SurfacePanel,
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

        body.AddChild(CreateRecipeDetailHeader(recipe, branchState, canCraft, loaded, requirementMet, hasMaterials, accent));
        body.AddChild(new Control { MinSize = new Vector2(1, 12) });

        body.AddChild(CreateSection(
            Loc.GetString("persistent-craft-description-label"),
            $"[color={DescriptionText.ToHex()}]{FormattedMessage.EscapeText(ResolveRecipeDescription(recipe))}[/color]",
            10));
        body.AddChild(new Control { MinSize = new Vector2(1, 10) });

        body.AddChild(CreateSection(
            Loc.GetString("persistent-craft-recipe-ingredients"),
            BuildIngredientMarkup(recipe),
            10));
        body.AddChild(new Control { VerticalExpand = true });
        panel.AddChild(body);

        return panel;
    }

    private Control CreateSection(string title, string markupText, int padding)
    {
        var section = new PersistentCraftTextSection();
        section.SetData(title, markupText, CardMutedBorder, padding);
        return section;
    }

    private Control CreateRecipeDetailHeader(
        PersistentCraftRecipePrototype recipe,
        PersistentCraftBranchState branchState,
        bool canCraft,
        bool loaded,
        bool requirementMet,
        bool hasMaterials,
        Color accent)
    {
        _ = loaded;
        _ = requirementMet;
        _ = hasMaterials;

        var header = new PersistentCraftRecipeDetailHeader();
        header.SetData(
            ResolveRecipeName(recipe),
            Color.White,
            Loc.GetString("persistent-craft-recipe-action"),
            !canCraft);
        header.ActionButton.OnPressed += _ => OnCraftPressed?.Invoke(recipe.ID);
        header.InfoText.SetMessage(FormattedMessage.FromMarkupPermissive(BuildHeaderInfoMarkup(recipe)));
        header.IconHost.PanelOverride = new StyleBoxFlat
        {
            BackgroundColor = IconBackground,
            BorderColor = accent,
            BorderThickness = new Thickness(1),
            ContentMarginLeftOverride = 6,
            ContentMarginRightOverride = 6,
            ContentMarginTopOverride = 6,
            ContentMarginBottomOverride = 6,
        };
        header.IconHost.AddChild(CreateRecipeIconContent(recipe, new Vector2(116, 116)));

        header.MetaContainer.AddChild(CreateMetaBadge(
            $"{Loc.GetString("persistent-craft-branch-points-label")}: {branchState.AvailablePoints} | {Loc.GetString("persistent-craft-spent-points-label")}: {branchState.SpentPoints}",
            PersistentCraftUiTheme.SurfacePanelAlt,
            PersistentCraftUiTheme.TextPrimary));
        header.MetaContainer.AddChild(new Control { MinSize = new Vector2(1, 6) });
        header.MetaContainer.AddChild(CreateMetaBadge(
            $"{Loc.GetString("persistent-craft-level-label")} {PersistentCraftingHelper.GetTierDisplayLabel(recipe.Tier)}",
            PersistentCraftUiTheme.SurfacePanelAlt,
            accent));
        header.MetaContainer.AddChild(new Control { MinSize = new Vector2(1, 6) });
        header.MetaContainer.AddChild(CreateMetaBadge(
            GetRecipeCategoryPath(recipe),
            PersistentCraftUiTheme.SurfacePanelAlt,
            PersistentCraftUiTheme.TextMuted));
        header.MetaContainer.AddChild(new Control { MinSize = new Vector2(1, 6) });
        header.MetaContainer.AddChild(CreateMetaBadge(
            $"{Loc.GetString("persistent-craft-recipe-points")}: +{PersistentCraftingHelper.GetPointReward(recipe)}",
            PersistentCraftUiTheme.SurfacePanelAlt,
            accent));

        return header;
    }

    private PanelContainer CreateRecipeIcon(PersistentCraftRecipePrototype recipe, Color accent, Vector2 size)
    {
        var panel = new PanelContainer
        {
            MinSize = size,
            RectClipContent = true,
            PanelOverride = new StyleBoxFlat
            {
                BackgroundColor = IconBackground,
                BorderColor = accent,
                BorderThickness = new Thickness(1),
                ContentMarginLeftOverride = 6,
                ContentMarginRightOverride = 6,
                ContentMarginTopOverride = 6,
                ContentMarginBottomOverride = 6,
            },
        };

        panel.AddChild(CreateRecipeIconContent(recipe, size));

        return panel;
    }

    private Control CreateRecipeIconContent(PersistentCraftRecipePrototype recipe, Vector2 size)
    {
        if (TryGetRecipeTexture(recipe, out var texture))
        {
            return new TextureRect
            {
                Texture = texture,
                TextureScale = size.X >= 90 ? new Vector2(2.1f, 2.1f) : new Vector2(1.15f, 1.15f),
                Stretch = TextureRect.StretchMode.KeepAspectCentered,
                HorizontalAlignment = HAlignment.Center,
                VerticalAlignment = VAlignment.Center,
            };
        }

        return new Label
        {
            Text = PersistentCraftingHelper.GetTierDisplayLabel(recipe.Tier),
            HorizontalAlignment = HAlignment.Center,
            VerticalAlignment = VAlignment.Center,
        };
    }

    private PanelContainer CreateMetaBadge(string text, Color background, Color foreground)
    {
        var badge = new PanelContainer
        {
            PanelOverride = new StyleBoxFlat
            {
                BackgroundColor = background,
                BorderColor = PersistentCraftUiTheme.BorderSoft,
                BorderThickness = new Thickness(1),
                ContentMarginLeftOverride = 10,
                ContentMarginRightOverride = 10,
                ContentMarginTopOverride = 5,
                ContentMarginBottomOverride = 5,
            }
        };

        badge.AddChild(new Label
        {
            Text = text,
            FontColorOverride = foreground,
            ClipText = true,
        });

        return badge;
    }
}
