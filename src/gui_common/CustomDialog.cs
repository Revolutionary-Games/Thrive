using System;
using Godot;

/// <summary>
///   A reimplementation of WindowDialog for a much more customized style and functionality. Suitable for general use
///   or as a base class for any custom window dialog derived types.
/// </summary>
/// <remarks>
///   <para>
///     This uses Tool attribute to make this class be run in the Godot editor for real-time feedback as this class
///     handles UI visuals extensively through code. Not necessary but very helpful when editing scenes involving
///     any custom dialogs.
///   </para>
/// </remarks>
[Tool]
public class CustomDialog : Popup, ICustomPopup
{
    private string windowTitle;

    private bool closeHovered;

    private Vector2 dragOffset;
    private Vector2 dragOffsetFar;

    private TextureButton closeButton;

    private DragType dragType = DragType.None;

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
            windowTitle = value;
            MinimumSizeChanged();
            Update();
        }
    }

    /// <summary>
    ///   If true, the user can resize the window.
    /// </summary>
    [Export]
    public bool Resizable { get; set; }

    /// <summary>
    ///   If true, the window's position is clamped inside the screen so it doesn't go out of bounds.
    /// </summary>
    [Export]
    public bool BoundToScreenArea { get; set; } = true;

    [Export]
    public bool ExclusiveAllowCloseOnEscape { get; private set; } = true;

    public new bool Visible
    {
        get => base.Visible;
        set
        {
            if (value)
            {
                base.Visible = true;
            }
            else
            {
                // Execute possible closing animation
                ClosePopup();
            }
        }
    }

    public override void _EnterTree()
    {
        // To make popup rect readjustment react to window resizing
        if (!GetTree().Root.IsConnected("size_changed", this, nameof(OnViewportResized)))
            GetTree().Root.Connect("size_changed", this, nameof(OnViewportResized));

        SetupCloseButton();
        UpdateChildRects();
    }

    public override void _Notification(int what)
    {
        switch (what)
        {
            case NotificationResized:
            {
                UpdateChildRects();
                break;
            }

            case NotificationVisibilityChanged:
            {
                if (Visible)
                {
                    OnShown();
                }
                else
                {
                    OnHidden();
                }

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
        var panel = GetStylebox("custom_panel", "WindowDialog");
        var titleBarPanel = GetStylebox("custom_titlebar", "WindowDialog");
        var titleBarHeight = GetConstant("custom_titlebar_height", "WindowDialog");

        // Draw background panels
        DrawStyleBox(panel, new Rect2(
            new Vector2(0, -titleBarHeight), new Vector2(RectSize.x, RectSize.y + titleBarHeight)));

        DrawStyleBox(titleBarPanel, new Rect2(
            new Vector2(3, -titleBarHeight + 3), new Vector2(RectSize.x - 6, titleBarHeight - 3)));

        // Draw title in the title bar
        var titleFont = GetFont("custom_title_font", "WindowDialog");
        var titleHeight = GetConstant("custom_title_height", "WindowDialog");
        var titleColor = GetColor("custom_title_color", "WindowDialog");

        var translated = TranslationServer.Translate(WindowTitle);

        var fontHeight = titleFont.GetHeight() - titleFont.GetDescent() * 2;

        var titlePosition = new Vector2(
            (RectSize.x - titleFont.GetStringSize(translated).x) / 2, (-titleHeight + fontHeight) / 2);

        DrawString(titleFont, titlePosition, translated, titleColor, (int)(RectSize.x - panel.GetMinimumSize().x));

        // Draw close button highlight
        if (closeHovered)
        {
            var highlight = GetStylebox("custom_close_highlight", "WindowDialog");

            DrawStyleBox(highlight, closeButton.GetRect());
        }
    }

    public override void _GuiInput(InputEvent @event)
    {
        // Handle title bar dragging
        if (@event is InputEventMouseButton mouseButton && mouseButton.ButtonIndex == (int)ButtonList.Left)
        {
            if (mouseButton.Pressed)
            {
                // Begin a possible dragging operation
                dragType = DragHitTest(new Vector2(mouseButton.Position.x, mouseButton.Position.y));

                if (dragType != DragType.None)
                    dragOffset = GetGlobalMousePosition() - RectPosition;

                dragOffsetFar = RectPosition + RectSize - GetGlobalMousePosition();
            }
            else if (dragType != DragType.None && !mouseButton.Pressed)
            {
                // End a dragging operation
                dragType = DragType.None;
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
    ///   This is overriden so mouse position could take the titlebar into account due to it being drawn
    ///   outside of the normal Control's rect bounds.
    /// </summary>
    public override bool HasPoint(Vector2 point)
    {
        var rect = new Rect2(Vector2.Zero, RectSize);

        var titleBarHeight = GetConstant("custom_titlebar_height", "WindowDialog");

        // Enlarge upwards for title bar
        var adjustedRect = new Rect2(
            new Vector2(rect.Position.x, rect.Position.y - titleBarHeight),
            new Vector2(rect.Size.x, rect.Size.y + titleBarHeight));

        // Inflate by the resizable border thickness
        if (Resizable)
        {
            var scaleborderSize = GetConstant("custom_scaleborder_size", "WindowDialog");

            adjustedRect = new Rect2(
                new Vector2(adjustedRect.Position.x - scaleborderSize, adjustedRect.Position.y - scaleborderSize),
                new Vector2(adjustedRect.Size.x + scaleborderSize * 2, adjustedRect.Size.y + scaleborderSize * 2));
        }

        return adjustedRect.HasPoint(point);
    }

    /// <summary>
    ///   Overrides the minimum size to account for default elements (e.g title, close button, margin) rect size
    ///   and for the other custom added contents on the window.
    /// </summary>
    public override Vector2 _GetMinimumSize()
    {
        var margin = GetConstant("custom_margin", "Dialogs");
        var font = GetFont("custom_title_font", "WindowDialog");

        var buttonWidth = closeButton?.GetCombinedMinimumSize().x;
        var titleWidth = font.GetStringSize(TranslationServer.Translate(windowTitle)).x;
        var buttonArea = buttonWidth + (buttonWidth / 2);

        var contentSize = Vector2.Zero;

        for (int i = 0; i < GetChildCount(); ++i)
        {
            var child = GetChildOrNull<Control>(i);

            if (child == null || child == closeButton || child.IsSetAsToplevel())
                continue;

            var childMinSize = child.GetCombinedMinimumSize();

            contentSize = new Vector2(
                Mathf.Max(childMinSize.x, contentSize.x),
                Mathf.Max(childMinSize.y, contentSize.y));
        }

        // Redecide whether the largest rect is the default elements' or the contents'
        return new Vector2(Mathf.Max(2 * buttonArea.GetValueOrDefault() + titleWidth,
            contentSize.x + margin * 2), contentSize.y + margin * 2);
    }

    public new void Hide()
    {
        Visible = false;
    }

    public void ClosePopup()
    {
        // TODO: implement hide animation
        base.Hide();
    }

    /// <summary>
    ///   Called after the popup is made visible.
    /// </summary>
    protected virtual void OnShown()
    {
        // TODO: implement show animation(?)
    }

    /// <summary>
    ///   Called after popup is made invisible.
    /// </summary>
    protected virtual void OnHidden()
    {
        closeHovered = false;
    }

    /// <summary>
    ///   Evaluates what kind of drag type is being done on the window based on the current mouse position.
    /// </summary>
    private DragType DragHitTest(Vector2 position)
    {
        var result = DragType.None;

        if (Resizable)
        {
            var scaleborderSize = GetConstant("custom_scaleborder_size", "WindowDialog");
            var titleBarHeight = GetConstant("custom_titlebar_height", "WindowDialog");

            if (position.y < (-titleBarHeight + scaleborderSize))
            {
                result = DragType.ResizeTop;
            }
            else if (position.y >= (RectSize.y - scaleborderSize))
            {
                result = DragType.ResizeBottom;
            }

            if (position.x < scaleborderSize)
            {
                result |= DragType.ResizeLeft;
            }
            else if (position.x >= (RectSize.x - scaleborderSize))
            {
                result |= DragType.ResizeRight;
            }
        }

        if (result == DragType.None && position.y < 0)
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
            var previewDragType = DragHitTest(new Vector2(mouseMotion.Position.x, mouseMotion.Position.y));

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
                case (int)DragType.ResizeTop + DragType.ResizeLeft:
                case (int)DragType.ResizeBottom + DragType.ResizeRight:
                    cursor = CursorShape.Fdiagsize;
                    break;
                case (int)DragType.ResizeTop + DragType.ResizeRight:
                case (int)DragType.ResizeBottom + DragType.ResizeLeft:
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

        var newPosition = new Vector2(RectPosition);
        var newSize = new Vector2(RectSize);

        if (dragType == DragType.Move)
        {
            newPosition = globalMousePos - dragOffset;
        }
        else
        {
            // Handle border dragging

            if (dragType.HasFlag(DragType.ResizeTop))
            {
                var bottom = RectPosition.y + RectSize.y;
                var maxY = bottom - minSize.y;

                newPosition.y = Mathf.Min(globalMousePos.y - dragOffset.y, maxY);
                newSize.y = bottom - newPosition.y;
            }
            else if (dragType.HasFlag(DragType.ResizeBottom))
            {
                newSize.y = globalMousePos.y - newPosition.y + dragOffsetFar.y;
            }

            if (dragType.HasFlag(DragType.ResizeLeft))
            {
                var right = RectPosition.x + RectSize.x;
                var maxX = right - minSize.x;

                newPosition.x = Mathf.Min(globalMousePos.x - dragOffset.x, maxX);
                newSize.x = right - newPosition.x;
            }
            else if (dragType.HasFlag(DragType.ResizeRight))
            {
                newSize.x = globalMousePos.x - newPosition.x + dragOffsetFar.x;
            }
        }

        RectPosition = newPosition;
        RectSize = newSize;

        if (BoundToScreenArea)
            FixRect();
    }

    /// <summary>
    ///   Applies final adjustments to the window's rect.
    /// </summary>
    private void FixRect()
    {
        var screenSize = GetViewport()?.GetVisibleRect().Size;
        var screenSizeValue = screenSize.GetValueOrDefault();

        var titleBarHeight = GetConstant("custom_titlebar_height", "WindowDialog");

        // Clamp position to ensure window stays inside the screen
        RectPosition = new Vector2(
            Mathf.Clamp(RectPosition.x, 0, screenSizeValue.x - RectSize.x),
            Mathf.Clamp(RectPosition.y, titleBarHeight, screenSizeValue.y - RectSize.y));

        if (Resizable)
        {
            // Size can't be bigger than the viewport
            RectSize = new Vector2(
                Mathf.Min(RectSize.x, screenSizeValue.x), Mathf.Min(RectSize.y, screenSizeValue.y - titleBarHeight));
        }
    }

    private void SetupCloseButton()
    {
        if (closeButton != null)
            return;

        var closeColor = GetColor("custom_close_color", "WindowDialog");

        closeButton = new TextureButton
        {
            Expand = true,
            RectMinSize = new Vector2(14, 14),
            SelfModulate = closeColor,
            MouseFilter = MouseFilterEnum.Pass,
            TextureNormal = GetIcon("custom_close", "WindowDialog"),
        };

        closeButton.SetAnchorsPreset(LayoutPreset.TopRight);

        closeButton.RectPosition = new Vector2(
            -GetConstant("custom_close_h_ofs", "WindowDialog"),
            -GetConstant("custom_close_v_ofs", "WindowDialog"));

        closeButton.Connect("mouse_entered", this, nameof(OnCloseButtonMouseEnter));
        closeButton.Connect("mouse_exited", this, nameof(OnCloseButtonMouseExit));
        closeButton.Connect("pressed", this, nameof(OnCloseButtonPressed));

        AddChild(closeButton);
    }

    private void UpdateChildRects()
    {
        var margin = GetConstant("custom_margin", "Dialogs");

        var childPos = new Vector2(margin, margin);
        var childSize = new Vector2(RectSize.x - margin * 2, RectSize.y - margin * 2);

        for (int i = 0; i < GetChildCount(); ++i)
        {
            var child = GetChildOrNull<Control>(i);

            if (child == null || child == closeButton || child.IsSetAsToplevel())
                continue;

            child.RectPosition = childPos;
            child.RectSize = childSize;
        }
    }

    private void OnCloseButtonMouseEnter()
    {
        closeHovered = true;
        Update();
    }

    private void OnCloseButtonMouseExit()
    {
        closeHovered = false;
        Update();
    }

    private void OnCloseButtonPressed()
    {
        ClosePopup();
    }

    private void OnViewportResized()
    {
        if (BoundToScreenArea)
            FixRect();
    }
}
