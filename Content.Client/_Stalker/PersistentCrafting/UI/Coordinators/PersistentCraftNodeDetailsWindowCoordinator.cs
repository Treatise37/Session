using System;
using System.Numerics;
using Robust.Client.Graphics;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.CustomControls;
using Robust.Shared.Maths;

namespace Content.Client._Stalker.PersistentCrafting.UI.Coordinators;

public sealed class PersistentCraftNodeDetailsWindowCoordinator
{
    private readonly IClyde _clyde;
    private readonly float _windowWidth;
    private readonly float _windowHeight;
    private readonly float _windowMinWidth;
    private readonly float _windowMinHeight;
    private readonly float _windowMargin;
    private DefaultWindow? _window;

    public bool IsOpen => _window != null &&
                          !_window.Disposed &&
                          _window.IsOpen;

    public PersistentCraftNodeDetailsWindowCoordinator(
        IClyde clyde,
        float windowWidth,
        float windowHeight,
        float windowMinWidth,
        float windowMinHeight,
        float windowMargin)
    {
        _clyde = clyde;
        _windowWidth = windowWidth;
        _windowHeight = windowHeight;
        _windowMinWidth = windowMinWidth;
        _windowMinHeight = windowMinHeight;
        _windowMargin = windowMargin;
    }

    public void Show(string title, Control content)
    {
        EnsureWindow();
        var window = _window!;

        window.Title = title;
        window.RemoveAllChildren();

        var root = new BoxContainer
        {
            Orientation = BoxContainer.LayoutOrientation.Vertical,
            Margin = new Thickness(10),
            HorizontalExpand = true,
            VerticalExpand = false,
        };
        root.AddChild(content);

        var scroll = new ScrollContainer
        {
            HorizontalExpand = true,
            VerticalExpand = true,
            HScrollEnabled = false,
            VScrollEnabled = true,
        };
        scroll.AddChild(root);

        window.AddChild(scroll);
        if (!window.IsOpen)
            window.Open();

        PositionWindowTopRight(window);
        window.MoveToFront();
    }

    public void Close()
    {
        if (_window == null || _window.Disposed)
            return;

        _window.Close();
        _window = null;
    }

    private void EnsureWindow()
    {
        if (_window != null && !_window.Disposed)
            return;

        _window = new DefaultWindow
        {
            SetSize = new Vector2(_windowWidth, _windowHeight),
            MinSize = new Vector2(_windowMinWidth, _windowMinHeight),
            Resizable = true,
        };
        _window.OnClose += () => _window = null;
    }

    private void PositionWindowTopRight(DefaultWindow window)
    {
        var screen = _clyde.ScreenSize;
        var windowWidth = window.Width > 0 ? window.Width : (int) _windowWidth;
        var x = Math.Max(_windowMargin, screen.X - windowWidth - _windowMargin);
        var y = _windowMargin;
        LayoutContainer.SetPosition(window, new Vector2(x, y));
    }
}
