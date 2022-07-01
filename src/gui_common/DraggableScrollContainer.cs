using System;
using Godot;

/// <summary>
///   A scroll container which can be also moved by clicking and dragging.
/// </summary>
public class DraggableScrollContainer : ScrollContainer
{
    [Export]
    public NodePath? ContentPath;

    /// <summary>
    ///   Whether we're currently dragging, to prevent us starting dragging again.
    /// </summary>
    private bool dragging;

    /// <summary>
    ///   Whether we're currently zooming with mouse wheel, to prevent unwanted scroll while zooming.
    /// </summary>
    private bool zooming;

    private Control content = null!;
    private bool showScrollbars = true;
    private float contentScale = 1;

    /// <summary>
    ///   Unwanted scrolling happen when zooming with mouse wheel, this is to reset the scroll values back to its
    ///   last state before zooming started.
    /// </summary>
    private Int2 lastScrollValues;

    [Export]
    public float MaxZoom { get; set; } = 2.5f;

    [Export]
    public float MinZoom { get; set; } = 0.4f;

    [Export]
    public float ZoomFactor { get; set; } = 1.0f;

    [Export]
    public float ZoomStep { get; set; } = 0.05f;

    /// <summary>
    ///   Controls whether the horizontal and vertical scrollbars should be shown or not. This doesn't enable nor
    ///   disable the scrollbars.
    /// </summary>
    /// <remarks>
    ///   <para>
    ///     Workaround until scrollbar hiding feature <see href="https://github.com/godotengine/godot/pull/48008"/>
    ///     is available.
    ///   </para>
    /// </remarks>
    [Export]
    public bool ShowScrollbars
    {
        get => showScrollbars;
        set
        {
            showScrollbars = value;
            UpdateScrollbars();
        }
    }

    public override void _Ready()
    {
        base._Ready();

        if (ContentPath == null)
            throw new InvalidOperationException("Path to child control is expected");

        content = GetNode<Control>(ContentPath);

        // Workaround a bug in Godot (https://github.com/godotengine/godot/issues/22936).
        GetVScrollbar().Connect("value_changed", this, nameof(OnScrollStarted));
        GetHScrollbar().Connect("value_changed", this, nameof(OnScrollStarted));

        UpdateScrollbars();
    }

    public override void _GuiInput(InputEvent @event)
    {
        if (!Visible)
        {
            dragging = false;
            return;
        }

        var mouse = GetGlobalMousePosition();
        var min = RectGlobalPosition;
        var max = min + RectSize;

        // Don't allow drag motion to continue outside this control
        if (mouse.x < min.x || mouse.y < min.y || mouse.x > max.x || mouse.y > max.y)
        {
            dragging = false;
            return;
        }

        if (@event is InputEventMouseButton
            {
                Pressed: true, ButtonIndex: (int)ButtonList.Left or (int)ButtonList.Right,
            })
        {
            dragging = true;
        }
        else if (@event is InputEventMouseButton buttonDown && buttonDown.Pressed)
        {
            if (buttonDown.ButtonIndex == (int)ButtonList.WheelUp)
            {
                contentScale += ZoomFactor * ZoomStep;
                zooming = true;
                Update();
            }
            else if (buttonDown.ButtonIndex == (int)ButtonList.WheelDown)
            {
                contentScale -= ZoomFactor * ZoomStep;
                zooming = true;
                Update();
            }
        }
        else if (@event is InputEventMouseMotion motion && dragging)
        {
            // Multiplied by an inverse of the content's scale to make panning faster when zoomed out and vice versa.
            ScrollHorizontal -= (int)(motion.Relative.x * (1 / contentScale));
            ScrollVertical -= (int)(motion.Relative.y * (1 / contentScale));
        }
    }

    public override void _Input(InputEvent @event)
    {
        if (!Visible)
        {
            dragging = false;
            return;
        }

        if (@event is InputEventMouseButton
            {
                Pressed: false, ButtonIndex: (int)ButtonList.Left or (int)ButtonList.Right,
            })
        {
            dragging = false;
        }
    }

    public override void _Draw()
    {
        base._Draw();

        content.RectPivotOffset = new Vector2(ScrollHorizontal, ScrollVertical) + GetRect().End * 0.5f;
        contentScale = Mathf.Clamp(contentScale, MinZoom, MaxZoom);
        var pairValue = new Vector2(contentScale, contentScale);

        if (content.RectScale != pairValue)
            content.RectScale = pairValue;
    }

    public void ResetZoom()
    {
        contentScale = 1;
        Update();
    }

    private void UpdateScrollbars()
    {
        GetHScrollbar().RectScale = GetVScrollbar().RectScale = ShowScrollbars ? Vector2.One : Vector2.Zero;
    }

    private void OnScrollStarted(float value)
    {
        _ = value;

        if (zooming)
        {
            ScrollHorizontal = lastScrollValues.x;
            ScrollVertical = lastScrollValues.y;
            zooming = false;
            return;
        }
        else
        {
            lastScrollValues = new Int2(ScrollHorizontal, ScrollVertical);
        }
    }
}
