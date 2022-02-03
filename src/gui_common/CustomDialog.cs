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
using Godot;

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
public class CustomDialog : Popup, ICustomPopup
{
    private string windowTitle;
    private string translatedWindowTitle;

    private bool closeHovered;

    private Vector2 dragOffset;
    private Vector2 dragOffsetFar;

    private TextureButton closeButton;

    private DragType dragType = DragType.None;

    private StyleBox customPanel;
    private StyleBox titleBarPanel;
    private StyleBox closeButtonHighlight;

    private Font titleFont;
    private Color titleColor;

    private int titleBarHeight;
    private int titleHeight;
    private int scaleBorderSize;
    private int customMargin;
    private bool showCloseButton = true;
    private bool decorate = true;

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
            translatedWindowTitle = TranslationServer.Translate(value);

            MinimumSizeChanged();
            Update();
        }
    }

    /// <summary>
    ///   If true, the dialog window size is locked to the size of the viewport.
    /// </summary>
    [Export]
    public bool FullRect { get; set; }

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
    public bool ExclusiveAllowCloseOnEscape { get; set; } = true;

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
            Update();
        }
    }

    public override void _EnterTree()
    {
        // To make popup rect readjustment react to window resizing
        GetTree().Root.Connect("size_changed", this, nameof(OnViewportResized));

        customPanel = GetStylebox("custom_panel", "WindowDialog");
        titleBarPanel = GetStylebox("custom_titlebar", "WindowDialog");
        titleBarHeight = decorate ? GetConstant("custom_titlebar_height", "WindowDialog") : 0;
        titleFont = GetFont("custom_title_font", "WindowDialog");
        titleHeight = GetConstant("custom_title_height", "WindowDialog");
        titleColor = GetColor("custom_title_color", "WindowDialog");
        closeButtonHighlight = GetStylebox("custom_close_highlight", "WindowDialog");
        scaleBorderSize = GetConstant("custom_scaleBorder_size", "WindowDialog");
        customMargin = decorate ? GetConstant("custom_margin", "Dialogs") : 0;

        SetupCloseButton();
        UpdateChildRects();

        base._EnterTree();
    }

    public override void _ExitTree()
    {
        GetTree().Root.Disconnect("size_changed", this, nameof(OnViewportResized));

        base._ExitTree();
    }

    public override void _Notification(int what)
    {
        switch (what)
        {
            case NotificationResized:
            {
                UpdateChildRects();

                if (FullRect)
                    SetToFullRect();

                break;
            }

            case NotificationVisibilityChanged:
            {
                if (Visible)
                {
                    OnShown();

                    if (FullRect)
                        SetToFullRect();
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

            case NotificationTranslationChanged:
            {
                translatedWindowTitle = TranslationServer.Translate(windowTitle);
                break;
            }
        }
    }

    public override void _Draw()
    {
        if (!Decorate)
            return;

        // Draw background panels
        DrawStyleBox(customPanel, new Rect2(
            new Vector2(0, -titleBarHeight), new Vector2(RectSize.x, RectSize.y + titleBarHeight)));

        DrawStyleBox(titleBarPanel, new Rect2(
            new Vector2(3, -titleBarHeight + 3), new Vector2(RectSize.x - 6, titleBarHeight - 3)));

        // Draw title in the title bar
        var fontHeight = titleFont.GetHeight() - titleFont.GetDescent() * 2;

        var titlePosition = new Vector2(
            (RectSize.x - titleFont.GetStringSize(translatedWindowTitle).x) / 2, (-titleHeight + fontHeight) / 2);

        DrawString(titleFont, titlePosition, translatedWindowTitle, titleColor,
            (int)(RectSize.x - customPanel.GetMinimumSize().x));

        // Draw close button highlight
        if (closeHovered)
        {
            DrawStyleBox(closeButtonHighlight, closeButton.GetRect());
        }
    }

    public override void _GuiInput(InputEvent @event)
    {
        // Handle title bar dragging
        if (@event is InputEventMouseButton { ButtonIndex: (int)ButtonList.Left } mouseButton)
        {
            if (mouseButton.Pressed && Movable)
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

        // Enlarge upwards for title bar
        var adjustedRect = new Rect2(
            new Vector2(rect.Position.x, rect.Position.y - titleBarHeight),
            new Vector2(rect.Size.x, rect.Size.y + titleBarHeight));

        // Inflate by the resizable border thickness
        if (Resizable)
        {
            adjustedRect = new Rect2(
                new Vector2(adjustedRect.Position.x - scaleBorderSize, adjustedRect.Position.y - scaleBorderSize),
                new Vector2(adjustedRect.Size.x + scaleBorderSize * 2, adjustedRect.Size.y + scaleBorderSize * 2));
        }

        return adjustedRect.HasPoint(point);
    }

    /// <summary>
    ///   Overrides the minimum size to account for default elements (e.g title, close button, margin) rect size
    ///   and for the other custom added contents on the window.
    /// </summary>
    public override Vector2 _GetMinimumSize()
    {
        var buttonWidth = closeButton?.GetCombinedMinimumSize().x;
        var titleWidth = titleFont?.GetStringSize(TranslationServer.Translate(windowTitle)).x;
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

        // Re-decide whether the largest rect is the default elements' or the contents'
        return new Vector2(Mathf.Max(2 * buttonArea.GetValueOrDefault() + titleWidth.GetValueOrDefault(),
            contentSize.x + customMargin * 2), contentSize.y + customMargin * 2);
    }

    /// <summary>
    ///   Called after the popup is made visible.
    /// </summary>
    protected virtual void OnShown()
    {
        // TODO: implement default show animation(?)
    }

    /// <summary>
    ///   Called after popup is made invisible.
    /// </summary>
    protected virtual void OnHidden()
    {
        // TODO: add proper close animation
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
            if (position.y < (-titleBarHeight + scaleBorderSize))
            {
                result = DragType.ResizeTop;
            }
            else if (position.y >= (RectSize.y - scaleBorderSize))
            {
                result = DragType.ResizeBottom;
            }

            if (position.x < scaleBorderSize)
            {
                result |= DragType.ResizeLeft;
            }
            else if (position.x >= (RectSize.x - scaleBorderSize))
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

    private Vector2 GetScreenSize()
    {
        return GetViewport().GetVisibleRect().Size;
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
            var screenSize = GetScreenSize();

            if (dragType.HasFlag(DragType.ResizeTop))
            {
                var bottom = RectPosition.y + RectSize.y;
                var maxY = bottom - minSize.y;

                newPosition.y = Mathf.Clamp(globalMousePos.y - dragOffset.y, titleBarHeight, maxY);
                newSize.y = bottom - newPosition.y;
            }
            else if (dragType.HasFlag(DragType.ResizeBottom))
            {
                newSize.y = Mathf.Min(globalMousePos.y - newPosition.y + dragOffsetFar.y, screenSize.y - newPosition.y);
            }

            if (dragType.HasFlag(DragType.ResizeLeft))
            {
                var right = RectPosition.x + RectSize.x;
                var maxX = right - minSize.x;

                newPosition.x = Mathf.Clamp(globalMousePos.x - dragOffset.x, 0, maxX);
                newSize.x = right - newPosition.x;
            }
            else if (dragType.HasFlag(DragType.ResizeRight))
            {
                newSize.x = Mathf.Min(globalMousePos.x - newPosition.x + dragOffsetFar.x, screenSize.x - newPosition.x);
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
        var screenSize = GetScreenSize();

        // Clamp position to ensure window stays inside the screen
        RectPosition = new Vector2(
            Mathf.Clamp(RectPosition.x, 0, screenSize.x - RectSize.x),
            Mathf.Clamp(RectPosition.y, titleBarHeight, screenSize.y - RectSize.y));

        if (Resizable)
        {
            // Size can't be bigger than the viewport
            RectSize = new Vector2(
                Mathf.Min(RectSize.x, screenSize.x), Mathf.Min(RectSize.y, screenSize.y - titleBarHeight));
        }
    }

    private void SetupCloseButton()
    {
        if (!ShowCloseButton)
        {
            if (closeButton != null)
            {
                RemoveChild(closeButton);
                closeButton = null;
            }

            return;
        }

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
        var childPos = new Vector2(customMargin, customMargin);
        var childSize = new Vector2(RectSize.x - customMargin * 2, RectSize.y - customMargin * 2);

        for (int i = 0; i < GetChildCount(); ++i)
        {
            var child = GetChildOrNull<Control>(i);

            if (child == null || child == closeButton || child.IsSetAsToplevel())
                continue;

            child.RectPosition = childPos;
            child.RectSize = childSize;
        }
    }

    private void SetToFullRect()
    {
        var viewportSize = GetScreenSize();

        RectPosition = new Vector2(0, titleBarHeight);
        RectSize = new Vector2(viewportSize.x, viewportSize.y - titleBarHeight);
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
        Hide();
    }

    private void OnViewportResized()
    {
        if (BoundToScreenArea)
            FixRect();

        if (FullRect)
            SetToFullRect();
    }
}
