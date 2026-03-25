using System;
using System.Collections.Generic;
using System.Numerics;
using Content.Client.UserInterface.Controls;
using Content.Shared._Stalker.ItemUpgrades;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Localization;

namespace Content.Client._Stalker.ItemUpgrades.UI;

public sealed class STItemUpgradeWindow : FancyWindow
{
    private static STItemUpgradeWindow? _currentWindow;

    private readonly BoxContainer _topItems;
    private readonly BoxContainer _modTree;
    private readonly PanelContainer _modTreePanel;
    private readonly PanelContainer _leftBottomPanel;

    private readonly BoxContainer _rightColumn;
    private readonly PanelContainer _rightTopPanel;
    private readonly PanelContainer _rightBottomPanel;

    private STItemUpgradeItemEntry? _selected;
    private STItemUpgradeEntryView? _selectedUpgrade;
    private bool _confirmingReset;

    public STItemUpgradeWindow()
    {
        if (_currentWindow != null && !_currentWindow.Disposed)
            _currentWindow.Close();

        _currentWindow = this;
        OnClose += HandleClosed;

        Title = "Модификация снаряжения";
        MinSize = new Vector2(1100, 760);
        SetSize = new Vector2(1100, 760);

        var outerPanel = new PanelContainer
        {
            PanelOverride = CreatePanelStyle(),
            HorizontalExpand = true,
            VerticalExpand = true
        };

        var root = new BoxContainer
        {
            Orientation = BoxContainer.LayoutOrientation.Vertical,
            SeparationOverride = 8
        };

        outerPanel.AddChild(root);
        AddChild(outerPanel);

        root.AddChild(new Control
        {
            MinSize = new Vector2(0, 8)
        });

        _topItems = new BoxContainer
        {
            Orientation = BoxContainer.LayoutOrientation.Horizontal,
            SeparationOverride = 10,
            HorizontalExpand = true,
            VerticalExpand = false
        };

        var scroll = new ScrollContainer
        {
            HorizontalExpand = true,
            VerticalExpand = false
        };

        scroll.AddChild(_topItems);

        var topPanel = new PanelContainer
        {
            MinSize = new Vector2(0, 110),
            HorizontalExpand = true,
            PanelOverride = CreatePanelStyle()
        };

        topPanel.AddChild(scroll);
        root.AddChild(topPanel);

        root.AddChild(new Control
        {
            MinSize = new Vector2(0, 1)
        });

        var middle = new BoxContainer
        {
            Orientation = BoxContainer.LayoutOrientation.Horizontal,
            SeparationOverride = 8,
            VerticalExpand = true
        };

        var leftColumn = new BoxContainer
        {
            Orientation = BoxContainer.LayoutOrientation.Vertical,
            SeparationOverride = 8,
            HorizontalExpand = true,
            VerticalExpand = true
        };

        _modTree = new BoxContainer
        {
            Orientation = BoxContainer.LayoutOrientation.Horizontal,
            SeparationOverride = 20,
            HorizontalExpand = true,
            VerticalExpand = true
        };

        _modTreePanel = new PanelContainer
        {
            HorizontalExpand = true,
            VerticalExpand = true,
            PanelOverride = CreatePanelStyle()
        };

        var modTreeWrapper = new BoxContainer
        {
            Orientation = BoxContainer.LayoutOrientation.Vertical,
            HorizontalExpand = true,
            VerticalExpand = true
        };

        modTreeWrapper.AddChild(new Control
        {
            MinSize = new Vector2(0, 18)
        });

        modTreeWrapper.AddChild(_modTree);
        _modTreePanel.AddChild(modTreeWrapper);

        _leftBottomPanel = new PanelContainer
        {
            MinSize = new Vector2(0, 120),
            VerticalExpand = false,
            PanelOverride = CreatePanelStyle()
        };

        leftColumn.AddChild(_modTreePanel);
        leftColumn.AddChild(_leftBottomPanel);

        middle.AddChild(leftColumn);

        _rightColumn = new BoxContainer
        {
            Orientation = BoxContainer.LayoutOrientation.Vertical,
            SeparationOverride = 8,
            MinSize = new Vector2(290, 0),
            MaxSize = new Vector2(290, float.MaxValue),
            VerticalExpand = true
        };

        _rightTopPanel = new PanelContainer
        {
            VerticalExpand = true,
            PanelOverride = CreatePanelStyle()
        };

        _rightBottomPanel = new PanelContainer
        {
            MinSize = new Vector2(0, 170),
            VerticalExpand = false,
            PanelOverride = CreatePanelStyle()
        };

        _rightColumn.AddChild(_rightTopPanel);
        _rightColumn.AddChild(_rightBottomPanel);

        middle.AddChild(_rightColumn);
        root.AddChild(middle);

        root.AddChild(new Control
        {
            MinSize = new Vector2(0, 1)
        });

        ResetRightPanel();
        RefreshBottom();
        RefreshLeftBottomPanel();
    }

    private StyleBoxFlat CreatePanelStyle()
    {
        return new StyleBoxFlat
        {
            BackgroundColor = Robust.Shared.Maths.Color.FromHex("#1B1B1E"),
            BorderColor = Robust.Shared.Maths.Color.FromHex("#8B6B2B"),
            BorderThickness = new Robust.Shared.Maths.Thickness(1)
        };
    }

    private StyleBoxFlat CreateInnerPanelStyle()
    {
        return new StyleBoxFlat
        {
            BackgroundColor = Robust.Shared.Maths.Color.FromHex("#232329"),
            BorderColor = Robust.Shared.Maths.Color.FromHex("#8B6B2B"),
            BorderThickness = new Robust.Shared.Maths.Thickness(1)
        };
    }

    private StyleBoxFlat CreateButtonStyle()
    {
        return new StyleBoxFlat
        {
            BackgroundColor = Robust.Shared.Maths.Color.FromHex("#2A2A2F"),
            BorderColor = Robust.Shared.Maths.Color.FromHex("#8B6B2B"),
            BorderThickness = new Robust.Shared.Maths.Thickness(1)
        };
    }

    private StyleBoxFlat CreateUpgradeAvailableStyle()
    {
        return new StyleBoxFlat
        {
            BackgroundColor = Robust.Shared.Maths.Color.FromHex("#2A2A2F"),
            BorderColor = Robust.Shared.Maths.Color.FromHex("#C9A94A"),
            BorderThickness = new Robust.Shared.Maths.Thickness(2)
        };
    }

    private StyleBoxFlat CreateUpgradeBlockedStyle()
    {
        return new StyleBoxFlat
        {
            BackgroundColor = Robust.Shared.Maths.Color.FromHex("#1F1F23"),
            BorderColor = Robust.Shared.Maths.Color.FromHex("#444444"),
            BorderThickness = new Robust.Shared.Maths.Thickness(1)
        };
    }

    private StyleBoxFlat CreateUpgradeInstalledStyle()
    {
        return new StyleBoxFlat
        {
            BackgroundColor = Robust.Shared.Maths.Color.FromHex("#2E2E35"),
            BorderColor = Robust.Shared.Maths.Color.FromHex("#6FCF97"),
            BorderThickness = new Robust.Shared.Maths.Thickness(2)
        };
    }

    private void ApplyButtonStyle(Button button)
    {
        button.StyleBoxOverride = CreateButtonStyle();
    }

    private void HandleClosed()
    {
        if (_currentWindow == this)
            _currentWindow = null;
    }

    public void UpdateState(STItemUpgradeBoundUserInterfaceState state)
    {
        _topItems.RemoveAllChildren();
        _modTree.RemoveAllChildren();
        ResetRightPanel();

        if (state.Items == null || state.Items.Count == 0)
        {
            _selected = null;
            _selectedUpgrade = null;
            _confirmingReset = false;

            _topItems.AddChild(new Label
            {
                Text = "НЕТ ПРЕДМЕТОВ",
                HorizontalAlignment = HAlignment.Center,
                VerticalAlignment = VAlignment.Center
            });

            RefreshBottom();
            RefreshLeftBottomPanel();
            return;
        }

        STItemUpgradeItemEntry? refreshedSelected = null;
        if (_selected != null)
        {
            foreach (var item in state.Items)
            {
                if (item.Entity == _selected.Entity)
                {
                    refreshedSelected = item;
                    break;
                }
            }
        }

        _selected = refreshedSelected;

        if (_selected != null && _selectedUpgrade != null)
        {
            STItemUpgradeEntryView? refreshedUpgrade = null;
            foreach (var upgrade in _selected.Upgrades)
            {
                if (upgrade.Id == _selectedUpgrade.Id)
                {
                    refreshedUpgrade = upgrade;
                    break;
                }
            }

            _selectedUpgrade = refreshedUpgrade;
        }
        else
        {
            _selectedUpgrade = null;
        }

        foreach (var item in state.Items)
        {
            var card = CreateCard(item);
            _topItems.AddChild(card);
        }

        if (_selected != null)
            BuildModTree();

        if (_selectedUpgrade != null)
            RefreshRightPanel();

        RefreshBottom();
        RefreshLeftBottomPanel();
    }

    private Control CreateCard(STItemUpgradeItemEntry item)
    {
        var card = new Button
        {
            MinSize = new Vector2(160, 90),
            MaxSize = new Vector2(160, 90)
        };
        ApplyButtonStyle(card);

        var cardBox = new BoxContainer
        {
            Orientation = BoxContainer.LayoutOrientation.Vertical,
            SeparationOverride = 4
        };

        var spritePanel = new PanelContainer
        {
            MinSize = new Vector2(0, 50),
            PanelOverride = CreateInnerPanelStyle()
        };

        var sprite = CreateEntitySprite(item.Entity, new Vector2(48, 48));
        sprite.HorizontalAlignment = HAlignment.Center;
        sprite.VerticalAlignment = VAlignment.Center;
        spritePanel.AddChild(sprite);

        var namePanel = new PanelContainer
        {
            MinSize = new Vector2(0, 30),
            PanelOverride = CreateInnerPanelStyle()
        };

        namePanel.AddChild(new Label
        {
            Text = ShortenName(item.Name),
            HorizontalAlignment = HAlignment.Center,
            VerticalAlignment = VAlignment.Center
        });

        cardBox.AddChild(spritePanel);
        cardBox.AddChild(namePanel);
        card.AddChild(cardBox);

        card.OnPressed += _ =>
        {
            _selected = item;
            _selectedUpgrade = null;
            _confirmingReset = false;
            BuildModTree();
            RefreshBottom();
            RefreshLeftBottomPanel();
            ResetRightPanel();
        };

        return card;
    }

    private void RefreshLeftBottomPanel()
    {
        _leftBottomPanel.RemoveAllChildren();

        var root = new BoxContainer
        {
            Orientation = BoxContainer.LayoutOrientation.Horizontal,
            SeparationOverride = 0,
            HorizontalExpand = true,
            VerticalExpand = true
        };

        var leftColumn = new BoxContainer
        {
            Orientation = BoxContainer.LayoutOrientation.Vertical,
            SeparationOverride = 8,
            HorizontalExpand = true,
            VerticalExpand = true
        };

        var centerColumn = new BoxContainer
        {
            Orientation = BoxContainer.LayoutOrientation.Vertical,
            SeparationOverride = 8,
            HorizontalExpand = true,
            VerticalExpand = true
        };

        var rightColumn = new BoxContainer
        {
            Orientation = BoxContainer.LayoutOrientation.Vertical,
            SeparationOverride = 8,
            HorizontalExpand = true,
            VerticalExpand = true
        };

        if (_selected == null)
        {
            leftColumn.AddChild(new Label
            {
                Text = "Выберите предмет",
                HorizontalAlignment = HAlignment.Left,
                VerticalAlignment = VAlignment.Center
            });

            root.AddChild(leftColumn);
            root.AddChild(centerColumn);
            root.AddChild(rightColumn);

            _leftBottomPanel.AddChild(root);
            return;
        }

        if (!_confirmingReset)
        {
            var resetButton = new Button
            {
                Text = "Сбросить модификации",
                HorizontalAlignment = HAlignment.Left
            };
            ApplyButtonStyle(resetButton);

            resetButton.OnPressed += _ =>
            {
                _confirmingReset = true;
                RefreshLeftBottomPanel();
            };

            leftColumn.AddChild(resetButton);
        }
        else
        {
            var confirmRow = new BoxContainer
            {
                Orientation = BoxContainer.LayoutOrientation.Horizontal,
                SeparationOverride = 8
            };

            confirmRow.AddChild(new Label
            {
                Text = "Вы уверены?",
                HorizontalAlignment = HAlignment.Left,
                VerticalAlignment = VAlignment.Center
            });

            var yesButton = new Button
            {
                Text = "Да"
            };
            ApplyButtonStyle(yesButton);

            yesButton.OnPressed += _ =>
            {
                if (_selected == null)
                    return;

                _confirmingReset = false;

                var systems = IoCManager.Resolve<IEntitySystemManager>();
                var upgradeSystem = systems.GetEntitySystem<Content.Client._Stalker.ItemUpgrades.STItemUpgradeSystem>();
                upgradeSystem.RequestResetUpgrades(_selected.Entity);
            };

            var cancelButton = new Button
            {
                Text = "Отмена"
            };
            ApplyButtonStyle(cancelButton);

            cancelButton.OnPressed += _ =>
            {
                _confirmingReset = false;
                RefreshLeftBottomPanel();
            };

            confirmRow.AddChild(yesButton);
            confirmRow.AddChild(cancelButton);
            leftColumn.AddChild(confirmRow);
        }

        var repairButtonText = _selected.RepairSteelRequired > 0
            ? "Починить"
            : "Починка не требуется";

        var repairButton = new Button
        {
            Text = repairButtonText,
            HorizontalAlignment = HAlignment.Center
        };
        ApplyButtonStyle(repairButton);

        repairButton.OnPressed += _ =>
        {
            if (_selected == null)
                return;

            if (_selected.RepairSteelRequired <= 0)
                return;

            var systems = IoCManager.Resolve<IEntitySystemManager>();
            var upgradeSystem = systems.GetEntitySystem<Content.Client._Stalker.ItemUpgrades.STItemUpgradeSystem>();
            upgradeSystem.RequestRepairItem(_selected.Entity);
        };

        centerColumn.AddChild(repairButton);

        if (_selected.RepairSteelRequired > 0)
        {
            centerColumn.AddChild(new Label
            {
                Text = $"Нужно материалов: Сталь x{_selected.RepairSteelRequired}",
                HorizontalAlignment = HAlignment.Center
            });

            foreach (var tool in _selected.RepairTools)
            {
                centerColumn.AddChild(new Label
                {
                    Text = $"Нужен инструмент: {tool.Name}",
                    HorizontalAlignment = HAlignment.Center
                });
            }
        }
        else
        {
            centerColumn.AddChild(new Label
            {
                Text = "Предмет полностью исправен",
                HorizontalAlignment = HAlignment.Center
            });
        }

        root.AddChild(leftColumn);
        root.AddChild(centerColumn);
        root.AddChild(rightColumn);

        _leftBottomPanel.AddChild(root);
    }

    private void RefreshBottom()
    {
        _rightBottomPanel.RemoveAllChildren();

        if (_selected == null)
        {
            _rightBottomPanel.AddChild(new Label
            {
                Text = "Ничего не выбрано",
                HorizontalAlignment = HAlignment.Center,
                VerticalAlignment = VAlignment.Center
            });
            return;
        }

        var box = new BoxContainer
        {
            Orientation = BoxContainer.LayoutOrientation.Vertical,
            SeparationOverride = 6,
            HorizontalAlignment = HAlignment.Left,
            HorizontalExpand = true
        };

        var sprite = CreateEntitySprite(_selected.Entity, new Vector2(64, 64));
        sprite.HorizontalAlignment = HAlignment.Center;
        sprite.VerticalAlignment = VAlignment.Center;
        box.AddChild(sprite);

        box.AddChild(new Label
        {
            Text = "Выбранный предмет:",
            HorizontalAlignment = HAlignment.Left
        });

        box.AddChild(new Label
        {
            Text = _selected.Name,
            HorizontalAlignment = HAlignment.Left
        });

        if (!string.IsNullOrWhiteSpace(_selected.DurabilityState))
        {
            box.AddChild(new Label
            {
                Text = $"Состояние: {_selected.DurabilityState}",
                HorizontalAlignment = HAlignment.Left
            });
        }

        string buttonText;
        if (_selectedUpgrade == null)
            buttonText = "Выберите модификацию";
        else if (IsInstalledSelectedUpgrade())
            buttonText = "Модификация уже установлена";
        else if (!CanInstallSelectedUpgrade())
            buttonText = "Модификация недоступна";
        else
            buttonText = "Установить модификацию";

        var button = new Button
        {
            Text = buttonText,
            HorizontalAlignment = HAlignment.Left
        };
        ApplyButtonStyle(button);

        button.OnPressed += _ =>
        {
            if (!CanInstallSelectedUpgrade())
                return;

            TryInstallUpgrade();
        };

        box.AddChild(button);
        _rightBottomPanel.AddChild(box);
    }

    private void BuildModTree()
    {
        _modTree.RemoveAllChildren();

        if (_selected == null)
            return;

        var branches = new Dictionary<string, List<STItemUpgradeEntryView>>();
        var branchOrder = new List<string>();

        foreach (var upgrade in _selected.Upgrades)
        {
            var branchId = string.IsNullOrWhiteSpace(upgrade.BranchId) ? "default" : upgrade.BranchId!;

            if (!branches.TryGetValue(branchId, out var list))
            {
                list = new List<STItemUpgradeEntryView>();
                branches[branchId] = list;
                branchOrder.Add(branchId);
            }

            list.Add(upgrade);
        }

        foreach (var branchId in branchOrder)
        {
            var column = new BoxContainer
            {
                Orientation = BoxContainer.LayoutOrientation.Vertical,
                SeparationOverride = 10,
                HorizontalExpand = true,
                VerticalExpand = true
            };

            var branch = branches[branchId];

            foreach (var node in branch)
            {
                var installed = _selected.InstalledUpgrades.Contains(node.Id);
                var available = IsUpgradeAvailable(node, _selected);

                var title = ShortenNodeTitle(node.Name, 32);

                if (installed)
                    title = $"[УСТ] {title}";
                else if (!available)
                    title = $"[БЛОК] {title}";

                var panel = new Button
                {
                    MinSize = new Vector2(0, 72),
                    HorizontalExpand = true
                };

                if (installed)
                    panel.StyleBoxOverride = CreateUpgradeInstalledStyle();
                else if (available)
                    panel.StyleBoxOverride = CreateUpgradeAvailableStyle();
                else
                    panel.StyleBoxOverride = CreateUpgradeBlockedStyle();

                var box = new BoxContainer
                {
                    Orientation = BoxContainer.LayoutOrientation.Vertical,
                    SeparationOverride = 2,
                    HorizontalExpand = true
                };

                box.AddChild(new Label
                {
                    Text = title,
                    HorizontalAlignment = HAlignment.Center
                });

                box.AddChild(new Label
                {
                    Text = GetBranchDisplayName(node),
                    HorizontalAlignment = HAlignment.Center
                });

                panel.AddChild(box);

                panel.OnPressed += _ =>
                {
                    _selectedUpgrade = node;
                    BuildModTree();
                    RefreshRightPanel();
                    RefreshBottom();
                };

                column.AddChild(panel);
            }

            _modTree.AddChild(column);
        }
    }

    private void ResetRightPanel()
    {
        _rightTopPanel.RemoveAllChildren();

        _rightTopPanel.AddChild(new Label
        {
            Text = "Правая панель описания",
            HorizontalAlignment = HAlignment.Center,
            VerticalAlignment = VAlignment.Center
        });
    }

    private void RefreshRightPanel()
    {
        _rightTopPanel.RemoveAllChildren();

        if (_selectedUpgrade == null)
        {
            ResetRightPanel();
            return;
        }

        var box = new BoxContainer
        {
            Orientation = BoxContainer.LayoutOrientation.Vertical,
            SeparationOverride = 6
        };

        box.AddChild(new Label
        {
            Text = _selectedUpgrade.Name,
            HorizontalAlignment = HAlignment.Left
        });

        if (!string.IsNullOrWhiteSpace(_selectedUpgrade.BranchId))
        {
            box.AddChild(new Label
            {
                Text = $"Ветка: {GetBranchDisplayName(_selectedUpgrade)}",
                HorizontalAlignment = HAlignment.Left
            });
        }

        if (_selectedUpgrade.RequiredUpgrades.Count > 0)
        {
            box.AddChild(new Label
            {
                Text = "Требуется:",
                HorizontalAlignment = HAlignment.Left
            });

            foreach (var req in _selectedUpgrade.RequiredUpgrades)
            {
                box.AddChild(new Label
                {
                    Text = "- " + GetUpgradeDisplayName(req),
                    HorizontalAlignment = HAlignment.Left
                });
            }
        }

        if (_selectedUpgrade.RequiredMaterials.Count > 0)
        {
            box.AddChild(new Label
            {
                Text = "Материалы:",
                HorizontalAlignment = HAlignment.Left
            });

            foreach (var material in _selectedUpgrade.RequiredMaterials)
            {
                box.AddChild(new Label
                {
                    Text = $"- {material.Name} x{material.Amount}",
                    HorizontalAlignment = HAlignment.Left
                });
            }
        }

        if (_selectedUpgrade.RequiredTools.Count > 0)
        {
            box.AddChild(new Label
            {
                Text = "Инструменты:",
                HorizontalAlignment = HAlignment.Left
            });

            foreach (var tool in _selectedUpgrade.RequiredTools)
            {
                box.AddChild(new Label
                {
                    Text = $"- {tool.Name}",
                    HorizontalAlignment = HAlignment.Left
                });
            }
        }

        if (_selectedUpgrade.Gun != null)
        {
            var gun = _selectedUpgrade.Gun;

            box.AddChild(new Label
            {
                Text = "Оружие:",
                HorizontalAlignment = HAlignment.Left
            });

            if (Math.Abs(gun.FireRateMultiplier - 1f) > 0.01f)
            {
                var percent = (gun.FireRateMultiplier - 1f) * 100f;
                box.AddChild(new Label
                {
                    Text = $"{FormatPercent(percent)} скорострельности",
                    HorizontalAlignment = HAlignment.Left
                });
            }

            if (Math.Abs(gun.MinAngleMultiplier - 1f) > 0.01f)
            {
                var percent = (gun.MinAngleMultiplier - 1f) * 100f;
                box.AddChild(new Label
                {
                    Text = $"{FormatPercent(percent)} минимального разброса",
                    HorizontalAlignment = HAlignment.Left
                });
            }

            if (Math.Abs(gun.MaxAngleMultiplier - 1f) > 0.01f)
            {
                var percent = (gun.MaxAngleMultiplier - 1f) * 100f;
                box.AddChild(new Label
                {
                    Text = $"{FormatPercent(percent)} максимального разброса",
                    HorizontalAlignment = HAlignment.Left
                });
            }

            if (Math.Abs(gun.AngleIncreaseMultiplier - 1f) > 0.01f)
            {
                var percent = (gun.AngleIncreaseMultiplier - 1f) * 100f;
                box.AddChild(new Label
                {
                    Text = $"{FormatPercent(percent)} роста разброса",
                    HorizontalAlignment = HAlignment.Left
                });
            }

            if (Math.Abs(gun.AngleDecayMultiplier - 1f) > 0.01f)
            {
                var percent = (gun.AngleDecayMultiplier - 1f) * 100f;
                box.AddChild(new Label
                {
                    Text = $"{FormatPercent(percent)} сброса разброса",
                    HorizontalAlignment = HAlignment.Left
                });
            }
        }

        if (_selectedUpgrade.Armor != null)
        {
            box.AddChild(new Label
            {
                Text = "Броня:",
                HorizontalAlignment = HAlignment.Left
            });

            foreach (var coef in _selectedUpgrade.Armor.Coefficients)
            {
                box.AddChild(new Label
                {
                    Text = $"{ResolveDamageTypeName(coef.DamageType)} x{coef.Multiplier}",
                    HorizontalAlignment = HAlignment.Left
                });
            }

            foreach (var flat in _selectedUpgrade.Armor.FlatReductions)
            {
                box.AddChild(new Label
                {
                    Text = $"{ResolveDamageTypeName(flat.DamageType)} +{flat.Add}",
                    HorizontalAlignment = HAlignment.Left
                });
            }
        }

        if (IsInstalledSelectedUpgrade())
        {
            box.AddChild(new Label
            {
                Text = "Статус: уже установлена",
                HorizontalAlignment = HAlignment.Left
            });
        }
        else if (!CanInstallSelectedUpgrade())
        {
            box.AddChild(new Label
            {
                Text = "Статус: недоступна",
                HorizontalAlignment = HAlignment.Left
            });
        }
        else
        {
            box.AddChild(new Label
            {
                Text = "Статус: доступна",
                HorizontalAlignment = HAlignment.Left
            });
        }

        _rightTopPanel.AddChild(box);
    }

    private static string GetBranchDisplayName(STItemUpgradeEntryView entry)
    {
        if (!string.IsNullOrWhiteSpace(entry.BranchName))
            return entry.BranchName!;

        return entry.BranchId ?? "default";
    }

    private static string ResolveDamageTypeName(string damageType)
    {
        return Loc.GetString($"damage-type-{damageType.ToLowerInvariant()}");
    }

    private Control CreateEntitySprite(NetEntity entity, Vector2 size)
    {
        var entManager = IoCManager.Resolve<IEntityManager>();
        var uid = entManager.GetEntity(entity);

        var spriteView = new SpriteView
        {
            MinSize = size,
            SetSize = size,
            Scale = Vector2.One
        };

        if (uid != EntityUid.Invalid)
            spriteView.SetEntity(uid);

        return spriteView;
    }

    private bool IsInstalledSelectedUpgrade()
    {
        if (_selected == null || _selectedUpgrade == null)
            return false;

        return _selected.InstalledUpgrades.Contains(_selectedUpgrade.Id);
    }

    private bool CanInstallSelectedUpgrade()
    {
        if (_selected == null || _selectedUpgrade == null)
            return false;

        if (_selected.InstalledUpgrades.Contains(_selectedUpgrade.Id))
            return false;

        return IsUpgradeAvailable(_selectedUpgrade, _selected);
    }

    private void TryInstallUpgrade()
    {
        if (_selected == null || _selectedUpgrade == null)
            return;

        var systems = IoCManager.Resolve<IEntitySystemManager>();
        var upgradeSystem = systems.GetEntitySystem<Content.Client._Stalker.ItemUpgrades.STItemUpgradeSystem>();

        upgradeSystem.RequestInstallUpgrade(_selected.Entity, _selectedUpgrade.Id);
    }

    private string GetUpgradeDisplayName(string upgradeId)
    {
        if (_selected == null)
            return upgradeId;

        foreach (var upgrade in _selected.Upgrades)
        {
            if (upgrade.Id == upgradeId)
                return upgrade.Name;
        }

        return upgradeId;
    }

    private static bool IsUpgradeAvailable(STItemUpgradeEntryView entry, STItemUpgradeItemEntry item)
    {
        if (item.InstalledUpgrades.Contains(entry.Id))
            return true;

        if (!string.IsNullOrWhiteSpace(item.SelectedBranch) &&
            !string.Equals(item.SelectedBranch, entry.BranchId, StringComparison.Ordinal))
        {
            return false;
        }

        foreach (var required in entry.RequiredUpgrades)
        {
            if (!item.InstalledUpgrades.Contains(required))
                return false;
        }

        return true;
    }

    private static string ShortenName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            return string.Empty;

        const int maxLength = 16;

        if (name.Length <= maxLength)
            return name;

        return name.Substring(0, maxLength - 3) + "...";
    }

    private static string ShortenNodeTitle(string name, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(name))
            return string.Empty;

        if (name.Length <= maxLength)
            return name;

        return name.Substring(0, maxLength - 3) + "...";
    }

    private static string FormatPercent(float value)
    {
        var rounded = MathF.Round(value, 1);

        if (rounded > 0)
            return $"+{rounded}%";

        return $"{rounded}%";
    }
}