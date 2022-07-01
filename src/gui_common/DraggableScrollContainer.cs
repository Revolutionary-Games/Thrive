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
    ///   Whether we're currently scrolling, to prevent dragging.
    /// </summary>
    private bool scrolling;

    /// <summary>
    ///   Whether we're currently dragging, to prevent us starting dragging again.
    /// </summary>
    private bool dragging;

    /// <summary>
    ///   Timer used for scroll bug workaround in Godot (https://github.com/godotengine/godot/issues/22936).
    /// </summary>
    private float scrollingTimer;

    private bool showScrollbars = true;

    private Control content = null!;
    private Vector2 contentScale = Vector2.One;

    [Export]
    public float ZoomFactor { get; set; } = 1.05f;

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

        GetVScrollbar().Connect("scrolling", this, nameof(OnScrollStarted));
        GetHScrollbar().Connect("scrolling", this, nameof(OnScrollStarted));

        UpdateScrollbars();
    }

    public override void _Process(float delta)
    {
        base._Process(delta);

        if (scrollingTimer > 0)
        {
            scrollingTimer -= delta;

            if (scrollingTimer <= 0)
                OnScrollEnded();
        }
    }

    public override void _GuiInput(InputEvent @event)
    {
        if (!Visible || scrolling)
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
        else if (@event is InputEventMouseMotion motion && dragging)
        {
            ScrollHorizontal -= (int)(motion.Relative.x * (1 / contentScale.x));
            ScrollVertical -= (int)(motion.Relative.y * (1 / contentScale.y));
        }
    }

    public override void _Input(InputEvent @event)
    {
        if (!Visible || scrolling)
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
        else if (@event is InputEventMouseButton button && button.Pressed)
        {
            if (button.ButtonIndex == (int)ButtonList.WheelUp)
            {
                contentScale *= ZoomFactor;
                Update();
                GetTree().SetInputAsHandled();
            }
            else if (button.ButtonIndex == (int)ButtonList.WheelDown)
            {
                contentScale *= 1 / ZoomFactor;
                Update();
                GetTree().SetInputAsHandled();
            }
        }
    }

    public override void _Draw()
    {
        base._Draw();

        content.RectPivotOffset = new Vector2(ScrollHorizontal, ScrollVertical) + GetRect().End / 2;

        if (content.RectScale != contentScale)
            content.RectScale = contentScale;
    }

    public void ResetZoom()
    {
        contentScale = Vector2.One;
        Update();
    }

    private void UpdateScrollbars()
    {
        GetHScrollbar().RectScale = GetVScrollbar().RectScale = ShowScrollbars ? Vector2.One : Vector2.Zero;
    }

    private void OnScrollStarted()
    {
        if (scrollingTimer <= 0)
        {
            scrolling = true;
            scrollingTimer = 0.1f;
        }
    }

    private void OnScrollEnded()
    {
        scrolling = false;
        scrollingTimer = 0;
    }
}
