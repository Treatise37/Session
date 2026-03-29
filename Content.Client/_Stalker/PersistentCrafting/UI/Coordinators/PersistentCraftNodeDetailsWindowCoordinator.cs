using System;
using System.Numerics;
using Robust.Client.Graphics;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Maths;

namespace Content.Client._Stalker.PersistentCrafting.UI.Coordinators;

public sealed class PersistentCraftNodeDetailsWindowCoordinator
{
    private readonly IClyde _clyde;
    private readonly IUserInterfaceManager _uiManager;
    private readonly float _windowWidth;
    private readonly float _windowHeight;
    private readonly float _windowMinWidth;
    private readonly float _windowMinHeight;
    private readonly float _windowMargin;
    private Popup? _popup;

    public bool IsOpen => _popup != null &&
                          !_popup.Disposed &&
                          _popup.Visible;

    public PersistentCraftNodeDetailsWindowCoordinator(
        IClyde clyde,
        IUserInterfaceManager uiManager,
        float windowWidth,
        float windowHeight,
        float windowMinWidth,
        float windowMinHeight,
        float windowMargin)
    {
        _clyde = clyde;
        _uiManager = uiManager;
        _windowWidth = windowWidth;
        _windowHeight = windowHeight;
        _windowMinWidth = windowMinWidth;
        _windowMinHeight = windowMinHeight;
        _windowMargin = windowMargin;
    }

    public void Show(string title, Control content)
    {
        EnsurePopup();
        var popup = _popup!;
        popup.RemoveAllChildren();

        var root = new BoxContainer
        {
            Orientation = BoxContainer.LayoutOrientation.Vertical,
            Margin = new Thickness(8),
            HorizontalExpand = true,
            VerticalExpand = false,
        };
        root.AddChild(new Label
        {
            Text = title,
            HorizontalExpand = true,
        });
        root.AddChild(new Control { MinSize = new Vector2(1, 6) });
        root.AddChild(content);

        var scroll = new ScrollContainer
        {
            HorizontalExpand = true,
            VerticalExpand = true,
            HScrollEnabled = false,
            VScrollEnabled = true,
        };
        scroll.AddChild(root);
        popup.AddChild(scroll);

        var box = BuildPopupBox();
        popup.Open(box, new Vector2(box.Left, box.Top), new Vector2(box.Left, box.Bottom));
    }

    public void Close()
    {
        if (_popup == null || _popup.Disposed)
            return;

        _popup.Close();
    }

    private void EnsurePopup()
    {
        if (_popup != null && !_popup.Disposed)
            return;

        _popup = new Popup
        {
            CloseOnClick = true,
            CloseOnEscape = true,
            MinSize = new Vector2(_windowMinWidth, _windowMinHeight),
            HorizontalExpand = false,
            VerticalExpand = false,
        };

        _uiManager.ModalRoot.AddChild(_popup);
    }

    private UIBox2 BuildPopupBox()
    {
        var screen = _clyde.ScreenSize;
        var width = MathF.Max(_windowMinWidth, _windowWidth);
        var height = MathF.Max(_windowMinHeight, _windowHeight);
        var x = MathF.Max(_windowMargin, screen.X - width - _windowMargin);
        var y = _windowMargin;
        return UIBox2.FromDimensions(new Vector2(x, y), new Vector2(width, height));
    }
}
