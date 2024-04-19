/*************************************************************************/
/*              This file is substantially derived from:                 */
/*                           GODOT ENGINE                                */
/*                      https://godotengine.org                          */
/*************************************************************************/
/* Copyright (c) 2007-2021 Juan Linietsky, Ariel Manzur.                 */
/* Copyright (c) 2014-2021 Godot Engine contributors (cf. AUTHORS.md).   */

/* Permission is hereby granted, free of charge, to any person obtaining */
/* a copy of this software and associated documentation files (the       */
/* "Software"), to deal in the Software without restriction, including   */
/* without limitation the rights to use, copy, modify, merge, publish,   */
/* distribute, sublicense, and/or sell copies of the Software, and to    */
/* permit persons to whom the Software is furnished to do so, subject to */
/* the following conditions:                                             */

/* The above copyright notice and this permission notice shall be        */
/* included in all copies or substantial portions of the Software.       */

/* THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,       */
/* EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF    */
/* MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT.*/
/* IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY  */
/* CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT,  */
/* TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE     */
/* SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.                */
/*************************************************************************/

using System;
using System.Collections.Generic;
using Godot;
using Godot.Collections;

/// <summary>
///   A reimplementation of WindowDialog for a much more customized style and functionality. Suitable for general use
///   or as a base class for any custom window dialog derived types.
/// </summary>
/// <remarks>
///   <para>
///     This uses Tool attribute to make this class be run in the Godot editor for live feedback as this class
///     handles UI visuals extensively through code. Not necessary but very helpful when editing scenes involving
///     any custom dialogs.
///     NOTE: should always be commented in master branch to avoid Godot breaking exported properties. Uncomment this
///     only locally if needed.
///   </para>
/// </remarks>
/// TODO: see https://github.com/Revolutionary-Games/Thrive/issues/2751
/// [Tool]
public partial class CustomWindow : TopLevelContainer
{
    /// <summary>
    ///   Paths to window reordering nodes in ancestors.
    /// </summary>
    /// <remarks>
    ///   <para>
    ///     This overrides automatic search.
    ///   </para>
    ///   <para>
    ///     NOTE: Changes take effect when this node enters a tree.
    ///   </para>
    /// </remarks>
    [Export]
    public Array<NodePath>? WindowReorderingPaths;

    /// <summary>
    ///   Tries to find first window reordering node in ancestors to connect to up to the specified depth.
    /// </summary>
    /// <remarks>
    ///   <para>
    ///     Ignored when <see cref="WindowReorderingPaths"/> are not empty.
    ///   </para>
    ///   <para>
    ///     NOTE: Changes take effect when this node enters a tree.
    ///   </para>
    /// </remarks>
    [Export]
    public int AutomaticWindowReorderingDepth = 10;

    /// <summary>
    ///   If true, window reordering nodes also connect this window to their ancestors.
    /// </summary>
    /// <remarks>
    ///   <para>
    ///     NOTE: Changes take effect when this node enters a tree.
    ///   </para>
    /// </remarks>
    [Export]
    public bool AllowWindowReorderingRecursion = true;

    private static readonly Lazy<StyleBox> CloseButtonFocus = new(CreateCloseButtonFocusStyle);

    private readonly List<AddWindowReorderingSupportToSiblings> windowReorderingNodes = new();

    private string windowTitle = string.Empty;
    private string translatedWindowTitle = string.Empty;

    private bool closeHovered;
    private bool closeFocused;

    /// <summary>
    ///   Stored value of what <see cref="AllowWindowReorderingRecursion"/> was when it was applied. Used to guard
    ///   against changes to that value after its been used which would lead to inconsistent logic without this.
    /// </summary>
    private bool usedAllowWindowReorderingRecursion = true;

    private Vector2 dragOffset;
    private Vector2 dragOffsetFar;

#pragma warning disable CA2213
    private TextureButton? closeButton;
    private Texture2D closeButtonTexture = null!;

    private StyleBox customPanel = null!;
    private StyleBox titleBarPanel = null!;
    private StyleBox closeButtonHighlight = null!;

    private Font? titleFont;
#pragma warning restore CA2213
    private Color titleColor;
    private Color closeButtonColor;

    private DragType dragType = DragType.None;

    private int titleFontSize;

    private int titleBarHeight;
    private int titleHeight;
    private int scaleBorderSize;
    private int customMargin;
    private bool showCloseButton = true;
    private bool decorate = true;

    /// <summary>
    ///   This is emitted by any means to hide this dialog (when not accepting) but NOT the hiding itself, for that use
    ///   <see cref="ClosedEventHandler"/> signal OR <see cref="OnHidden"/>.
    /// </summary>
    [Signal]
    public delegate void CanceledEventHandler();

    [Signal]
    public delegate void DraggedEventHandler(TopLevelContainer window);

    [Flags]
    private enum DragType
    {
        None = 0,
        Move = 1,
        ResizeTop = 1 << 1,
        ResizeRight = 1 << 2,
        ResizeBottom = 1 << 3,
        ResizeLeft = 1 << 4,
    }

    /// <summary>
    ///   The text displayed in the window's title bar.
    /// </summary>
    [Export]
    public string WindowTitle
    {
        get => windowTitle;
        set
        {
            if (windowTitle == value)
                return;

            windowTitle = value;
            translatedWindowTitle = Localization.Translate(value);

            UpdateMinimumSize();
            QueueRedraw();
        }
    }

    /// <summary>
    ///   If true, the user can resize the window.
    /// </summary>
    [Export]
    public bool Resizable { get; set; }

    /// <summary>
    ///   If true, the user can move the window around the viewport by dragging the titlebar.
    /// </summary>
    [Export]
    public bool Movable { get; set; } = true;

    /// <summary>
    ///   If true, the window's position is clamped inside the screen so it doesn't go out of bounds.
    /// </summary>
    [Export]
    public bool BoundToScreenArea { get; set; } = true;

    [Export]
    public bool ShowCloseButton
    {
        get => showCloseButton;
        set
        {
            if (showCloseButton == value)
                return;

            showCloseButton = value;
            SetupCloseButton();
        }
    }

    /// <summary>
    ///   Sets whether the window frame should be visible.
    /// </summary>
    /// <remarks>
    ///   <para>
    ///     Note: Doesn't handle close button. That's still controlled by <see cref="ShowCloseButton"/>.
    ///   </para>
    /// </remarks>
    [Export]
    public bool Decorate
    {
        get => decorate;
        set
        {
            if (decorate == value)
                return;

            decorate = value;

            // TODO: doesn't this need to adjust titleBarHeight value here as that's only set on tree entry?
            QueueRedraw();
        }
    }

    public override void _EnterTree()
    {
        customPanel = GetThemeStylebox("custom_panel", "Window");
        titleBarPanel = GetThemeStylebox("custom_titlebar", "Window");
        titleBarHeight = decorate ? GetThemeConstant("custom_titlebar_height", "Window") : 0;
        titleFont = GetThemeFont("title_font", "Window");
        titleFontSize = GetThemeFontSize("title_font_size", "Window");
        titleHeight = GetThemeConstant("custom_title_height", "Window");
        titleColor = GetThemeColor("custom_title_color", "Window");
        closeButtonColor = GetThemeColor("custom_close_color", "Window");
        closeButtonHighlight = GetThemeStylebox("custom_close_highlight", "Window");
        closeButtonTexture = GetThemeIcon("custom_close", "Window");
        scaleBorderSize = GetThemeConstant("custom_scaleBorder_size", "Window");
        customMargin = decorate ? GetThemeConstant("custom_margin", "Dialogs") : 0;

        // Make the close button style be fully created when this is initialized
        if (showCloseButton)
            _ = CloseButtonFocus.Value;

        ConnectToWindowReorderingNodes();

        base._EnterTree();

        Localization.Instance.OnTranslationsChanged += OnTranslationsChanged;
    }

    public override void _ExitTree()
    {
        base._ExitTree();

        Localization.Instance.OnTranslationsChanged -= OnTranslationsChanged;
        DisconnectFromWindowReorderingNodes();
    }

    public override void _Notification(int what)
    {
        base._Notification(what);

        switch ((long)what)
        {
            case NotificationReady:
            {
                SetupCloseButton();
                UpdateChildRects();
                break;
            }

            case NotificationResized:
            {
                UpdateChildRects();
                break;
            }

            case NotificationMouseExit:
            {
                // Reset the mouse cursor when leaving the resizable window border.
                if (Resizable && dragType == DragType.None)
                {
                    if (MouseDefaultCursorShape != CursorShape.Arrow)
                        MouseDefaultCursorShape = CursorShape.Arrow;
                }

                break;
            }
        }
    }

    public override void _Draw()
    {
        if (!Decorate)
            return;

        // Draw background panels
        DrawStyleBox(customPanel,
            new Rect2(new Vector2(0, -titleBarHeight), new Vector2(Size.X, Size.Y + titleBarHeight)));

        DrawStyleBox(titleBarPanel,
            new Rect2(new Vector2(3, -titleBarHeight + 3), new Vector2(Size.X - 6, titleBarHeight - 3)));

        // Draw title in the title bar
        var fontHeight = titleFont!.GetHeight(titleFontSize) - titleFont.GetDescent(titleFontSize) * 2;

        var titlePosition = new Vector2(0, (-titleHeight + fontHeight) * 0.5f);

        DrawString(titleFont, titlePosition, translatedWindowTitle, HorizontalAlignment.Center, Size.X,
            titleFontSize, titleColor);

        // Draw close button (if this window has a close button)
        if (closeButton != null)
        {
            var closeButtonRect = closeButton!.GetRect();

            // We render this in a custom way because rendering it in a child node causes a bug where render order
            // breaks in some cases: https://github.com/Revolutionary-Games/Thrive/issues/4365
            DrawTextureRect(closeButtonTexture, closeButtonRect, false, closeButtonColor);

            // Draw close button highlight
            if (closeHovered)
            {
                DrawStyleBox(closeButtonHighlight, closeButtonRect);
            }
            else if (closeFocused)
            {
                DrawStyleBox(CloseButtonFocus.Value, closeButtonRect);
            }
        }
    }

    public override void _GuiInput(InputEvent @event)
    {
        // Handle title bar dragging
        if (@event is InputEventMouseButton { ButtonIndex: MouseButton.Left } mouseButton)
        {
            if (mouseButton.Pressed && Movable && !closeHovered)
            {
                // Begin a possible dragging operation
                dragType = DragHitTest(new Vector2(mouseButton.Position.X, mouseButton.Position.Y));

                if (dragType != DragType.None)
                    dragOffset = GetGlobalMousePosition() - Position;

                dragOffsetFar = Position + Size - GetGlobalMousePosition();

                EmitSignal(SignalName.Dragged, this);
                GetViewport().SetInputAsHandled();
            }
            else if (dragType != DragType.None && !mouseButton.Pressed)
            {
                // End a dragging operation
                dragType = DragType.None;
                GetViewport().SetInputAsHandled();
            }
        }

        if (@event is InputEventMouseMotion mouseMotion)
        {
            if (dragType == DragType.None)
            {
                HandlePreviewDrag(mouseMotion);
            }
            else
            {
                HandleActiveDrag();
            }
        }
    }

    /// <summary>
    ///   Overrides the minimum size to account for default elements (e.g. title, close button, margin) rect size
    ///   and for the other custom added contents on the window.
    /// </summary>
    public override Vector2 _GetMinimumSize()
    {
        var buttonWidth = closeButton?.GetCombinedMinimumSize().X;
        var titleWidth = titleFont?.GetStringSize(translatedWindowTitle).X;
        var buttonArea = buttonWidth + buttonWidth / 2;

        var contentSize = Vector2.Zero;

        for (int i = 0; i < GetChildCount(); ++i)
        {
            var child = GetChildOrNull<Control>(i);

            if (child == null || child == closeButton || child.TopLevel)
                continue;

            var childMinSize = child.GetCombinedMinimumSize();

            contentSize = new Vector2(Mathf.Max(childMinSize.X, contentSize.X),
                Mathf.Max(childMinSize.Y, contentSize.Y));
        }

        // Re-decide whether the largest rect is the default elements' or the contents'
        return new Vector2(Mathf.Max(2 * buttonArea.GetValueOrDefault() + titleWidth.GetValueOrDefault(),
            contentSize.X + customMargin * 2), contentSize.Y + customMargin * 2);
    }

    /// <summary>
    ///   This is overriden so mouse position could take the titlebar into account due to it being drawn
    ///   outside of the normal Control's rect bounds.
    /// </summary>
    public override bool _HasPoint(Vector2 point)
    {
        // Enlarge upwards for title bar
        var position = Vector2.Zero;
        var size = Size;
        position.Y -= titleBarHeight;
        size.Y += titleBarHeight;
        var rect = new Rect2(position, size);

        // Inflate by the resizable border thickness
        if (Resizable)
        {
            rect = new Rect2(new Vector2(rect.Position.X - scaleBorderSize, rect.Position.Y - scaleBorderSize),
                new Vector2(rect.Size.X + scaleBorderSize * 2, rect.Size.Y + scaleBorderSize * 2));
        }

        return rect.HasPoint(point);
    }

    protected override void OnOpen()
    {
        base.OnOpen();
        UpdateChildRects();
    }

    protected override void OnHidden()
    {
        base.OnHidden();
        UpdateChildRects();
        closeHovered = false;
        closeFocused = false;
    }

    protected override Rect2 GetFullRect()
    {
        var rect = base.GetFullRect();
        rect.Position = new Vector2(0, titleBarHeight);
        rect.Size = new Vector2(rect.Size.X, rect.Size.Y - titleBarHeight);
        return rect;
    }

    protected override void ApplyRectSettings()
    {
        // For debugging warnings coming from window uneven anchors:
        // GD.Print("Applying rect to: " + GetPath());

        base.ApplyRectSettings();

        var screenSize = GetViewportRect().Size;

        if (BoundToScreenArea)
        {
            // Clamp position to ensure window stays inside the screen
            // titleBarHeight may be larger than the space left after the window fills the entire screen so that last
            // Max is needed
            Position = new Vector2(Mathf.Clamp(Position.X, 0, screenSize.X - Size.X),
                Mathf.Clamp(Position.Y, titleBarHeight, Math.Max(titleBarHeight, screenSize.Y - Size.Y)));
        }

        if (Resizable)
        {
            // Size can't be bigger than the viewport
            Size = new Vector2(Mathf.Min(Size.X, screenSize.X),
                Mathf.Min(Size.Y, screenSize.Y - titleBarHeight));
        }
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            if (WindowReorderingPaths != null)
            {
                foreach (var path in WindowReorderingPaths)
                    path.Dispose();
            }
        }

        base.Dispose(disposing);
    }

    private static StyleBox CreateCloseButtonFocusStyle()
    {
        // Note that thrive_theme.tres has the normal hovered style for this, this kind of style can't be specified
        // in the theme, so instead this needs to be defined through the code here. So it's important to keep
        // these two styles close enough if the button styling is overhauled.
        return new StyleBoxFlat
        {
            BgColor = new Color(0.05f, 0.05f, 0.05f, 0.5f),
            BorderColor = new Color(0.8f, 0.8f, 0.8f, 0.9f),
            CornerRadiusTopLeft = 2,
            CornerRadiusTopRight = 2,
            CornerRadiusBottomRight = 2,
            CornerRadiusBottomLeft = 2,
            ExpandMarginLeft = 3,
            ExpandMarginRight = 3,
            ExpandMarginTop = 3,
            ExpandMarginBottom = 3,
        };
    }

    private void ConnectToWindowReorderingNodes()
    {
        usedAllowWindowReorderingRecursion = AllowWindowReorderingRecursion;

        var windowReorderingAncestors = AddWindowReorderingSupportToSiblings.GetWindowReorderingAncestors(
            this, AutomaticWindowReorderingDepth, WindowReorderingPaths);

        foreach (var (reorderingNode, nodeSibling) in windowReorderingAncestors)
        {
            reorderingNode.ConnectWindow(this, nodeSibling, usedAllowWindowReorderingRecursion);
            windowReorderingNodes.Add(reorderingNode);
        }
    }

    private void DisconnectFromWindowReorderingNodes()
    {
        foreach (var node in windowReorderingNodes)
            node.DisconnectWindow(this, usedAllowWindowReorderingRecursion);

        windowReorderingNodes.Clear();
    }

    /// <summary>
    ///   Evaluates what kind of drag type is being done on the window based on the current mouse position.
    /// </summary>
    private DragType DragHitTest(Vector2 position)
    {
        var result = DragType.None;

        if (Resizable)
        {
            if (position.Y < -titleBarHeight + scaleBorderSize)
            {
                result = DragType.ResizeTop;
            }
            else if (position.Y >= Size.Y - scaleBorderSize)
            {
                result = DragType.ResizeBottom;
            }

            if (position.X < scaleBorderSize)
            {
                result |= DragType.ResizeLeft;
            }
            else if (position.X >= Size.X - scaleBorderSize)
            {
                result |= DragType.ResizeRight;
            }
        }

        if (result == DragType.None && position.Y < 0)
            result = DragType.Move;

        return result;
    }

    /// <summary>
    ///   Updates the cursor icon while moving along the borders.
    /// </summary>
    private void HandlePreviewDrag(InputEventMouseMotion mouseMotion)
    {
        var cursor = CursorShape.Arrow;

        if (Resizable)
        {
            var previewDragType = DragHitTest(new Vector2(mouseMotion.Position.X, mouseMotion.Position.Y));

            switch (previewDragType)
            {
                case DragType.ResizeTop:
                case DragType.ResizeBottom:
                    cursor = CursorShape.Vsize;
                    break;
                case DragType.ResizeLeft:
                case DragType.ResizeRight:
                    cursor = CursorShape.Hsize;
                    break;
                case DragType.ResizeTop | DragType.ResizeLeft:
                case DragType.ResizeBottom | DragType.ResizeRight:
                    cursor = CursorShape.Fdiagsize;
                    break;
                case DragType.ResizeTop | DragType.ResizeRight:
                case DragType.ResizeBottom | DragType.ResizeLeft:
                    cursor = CursorShape.Bdiagsize;
                    break;
            }
        }

        if (GetCursorShape() != cursor)
            MouseDefaultCursorShape = cursor;
    }

    /// <summary>
    ///   Updates the window position and size while in a dragging operation.
    /// </summary>
    private void HandleActiveDrag()
    {
        var globalMousePos = GetGlobalMousePosition();

        var minSize = GetCombinedMinimumSize();

        var newPosition = Position;
        var newSize = Size;

        if (dragType == DragType.Move)
        {
            newPosition = globalMousePos - dragOffset;
        }
        else
        {
            // Handle border dragging
            var screenSize = GetViewportRect().Size;

            if (dragType.HasFlag(DragType.ResizeTop))
            {
                var bottom = Position.Y + Size.Y;
                var maxY = bottom - minSize.Y;

                newPosition.Y = Mathf.Clamp(globalMousePos.Y - dragOffset.Y, titleBarHeight, maxY);
                newSize.Y = bottom - newPosition.Y;
            }
            else if (dragType.HasFlag(DragType.ResizeBottom))
            {
                newSize.Y = Mathf.Min(globalMousePos.Y - newPosition.Y + dragOffsetFar.Y, screenSize.Y - newPosition.Y);
            }

            if (dragType.HasFlag(DragType.ResizeLeft))
            {
                var right = Position.X + Size.X;
                var maxX = right - minSize.X;

                newPosition.X = Mathf.Clamp(globalMousePos.X - dragOffset.X, 0, maxX);
                newSize.X = right - newPosition.X;
            }
            else if (dragType.HasFlag(DragType.ResizeRight))
            {
                newSize.X = Mathf.Min(globalMousePos.X - newPosition.X + dragOffsetFar.X, screenSize.X - newPosition.X);
            }
        }

        Position = newPosition;
        Size = newSize;

        ApplyRectSettings();
    }

    private void SetupCloseButton()
    {
        if (!ShowCloseButton)
        {
            if (closeButton != null)
            {
                closeButton.DetachAndQueueFree();
                closeButton = null;
            }

            return;
        }

        if (closeButton != null)
            return;

        closeButton = new TextureButton
        {
            StretchMode = TextureButton.StretchModeEnum.KeepAspectCentered,
            CustomMinimumSize = new Vector2(14, 14),
            MouseFilter = MouseFilterEnum.Stop,
            MouseForcePassScrollEvents = false,
        };

        closeButton.SetAnchorsPreset(LayoutPreset.TopRight);

        closeButton.Position = new Vector2(-GetThemeConstant("custom_close_h_ofs", "Window"),
            -GetThemeConstant("custom_close_v_ofs", "Window"));

        closeButton.Connect(Control.SignalName.MouseEntered, new Callable(this, nameof(OnCloseButtonMouseEnter)));
        closeButton.Connect(Control.SignalName.MouseExited, new Callable(this, nameof(OnCloseButtonMouseExit)));
        closeButton.Connect(BaseButton.SignalName.Pressed, new Callable(this, nameof(OnCloseButtonPressed)));
        closeButton.Connect(Control.SignalName.FocusEntered, new Callable(this, nameof(OnCloseButtonFocused)));
        closeButton.Connect(Control.SignalName.FocusExited, new Callable(this, nameof(OnCloseButtonFocusLost)));

        AddChild(closeButton);
    }

    private void UpdateChildRects()
    {
        var childPos = new Vector2(customMargin, customMargin);
        var childSize = new Vector2(Size.X - customMargin * 2, Size.Y - customMargin * 2);

        for (int i = 0; i < GetChildCount(); ++i)
        {
            var child = GetChildOrNull<Control>(i);

            if (child == null || child == closeButton || child.TopLevel)
                continue;

            // Leaving the anchors alone here causes a warning in Godot 4
            child.AnchorLeft = 0;
            child.AnchorTop = 0;

            child.AnchorBottom = 0;
            child.AnchorRight = 0;

            child.Position = childPos;
            child.Size = childSize;
        }
    }

    private void OnCloseButtonMouseEnter()
    {
        closeHovered = true;
        QueueRedraw();
    }

    private void OnCloseButtonMouseExit()
    {
        closeHovered = false;
        QueueRedraw();
    }

    private void OnCloseButtonFocused()
    {
        closeFocused = true;
        QueueRedraw();
    }

    private void OnCloseButtonFocusLost()
    {
        closeFocused = false;
        QueueRedraw();
    }

    private void OnCloseButtonPressed()
    {
        GUICommon.Instance.PlayButtonPressSound();
        EmitSignal(SignalName.Canceled);
        Close();
    }

    private void OnTranslationsChanged()
    {
        translatedWindowTitle = Localization.Translate(windowTitle);
    }
}
