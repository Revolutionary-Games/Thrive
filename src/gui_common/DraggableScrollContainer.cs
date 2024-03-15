using System;
using Godot;
using Range = Godot.Range;

/// <summary>
///   A scroll container which can be also moved by clicking and dragging.
/// </summary>
public partial class DraggableScrollContainer : ScrollContainer
{
    /// <summary>
    ///   If set to null, the first child is used.
    /// </summary>
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

    /// <summary>
    ///   Whether we're currently centering (with Lerp) to a specific coordinate, to prevent dragging while
    ///   still lerping.
    /// </summary>
    private bool centering;

#pragma warning disable CA2213
    private Control content = null!;
#pragma warning restore CA2213

    private bool showScrollbars;
    private float contentScale = 1;

    /// <summary>
    ///   Unwanted scrolling happens while zooming with mouse wheel, this is used to reset the scroll values back to
    ///   its last state before zooming started.
    /// </summary>
    private Vector2I lastScrollValues;

    [Export]
    public float DragSensitivity { get; set; } = 1.0f;

    [Export]
    public float MaxZoom { get; set; } = 2.5f;

    [Export]
    public float MinZoom { get; set; } = 0.4f;

    [Export]
    public float ZoomFactor { get; set; } = 1.0f;

    [Export]
    public float ZoomStep { get; set; } = 0.1f;

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

        // Child 2 is the first child added by us, while child 0 and 1 are scroll bars
        ContentPath ??= GetChild(2).GetPath();

        content = GetNode<Control>(ContentPath);

        // Workaround a bug in Godot (https://github.com/godotengine/godot/issues/22936).
        GetVScrollBar().Connect(Range.SignalName.ValueChanged, new Callable(this, nameof(OnScrollStarted)));
        GetHScrollBar().Connect(Range.SignalName.ValueChanged, new Callable(this, nameof(OnScrollStarted)));

        UpdateScrollbars();
    }

    public override void _EnterTree()
    {
        base._EnterTree();
        this.RegisterToolTipForControl(ToolTipManager.Instance.GetToolTip("navigationHint", "patchMap"), false);
    }

    public override void _ExitTree()
    {
        base._ExitTree();
        this.UnRegisterToolTipForControl(ToolTipManager.Instance.GetToolTip("navigationHint", "patchMap"));
    }

    public override void _Draw()
    {
        base._Draw();

        content.PivotOffset = new Vector2(ScrollHorizontal, ScrollVertical) + GetRect().End * 0.5f;
        contentScale = Mathf.Clamp(contentScale, MinZoom, MaxZoom);
        var pairValue = new Vector2(contentScale, contentScale);

        if (content.Scale != pairValue)
            content.Scale = pairValue;
    }

    public override void _GuiInput(InputEvent @event)
    {
        if (!Visible || centering)
        {
            dragging = false;
            return;
        }

        var mouse = GetGlobalMousePosition();
        var min = GlobalPosition;
        var max = min + Size;

        // Don't allow drag motion to continue outside this control
        if (mouse.X < min.X || mouse.Y < min.Y || mouse.X > max.X || mouse.Y > max.Y)
        {
            dragging = false;
            return;
        }

        if (@event is InputEventMouseButton
            {
                Pressed: true, ButtonIndex: MouseButton.Left or MouseButton.Right,
            })
        {
            dragging = true;
        }
        else if (@event is InputEventMouseButton buttonDown && buttonDown.Pressed)
        {
            if (buttonDown.ButtonIndex == MouseButton.WheelUp)
            {
                Zoom(contentScale + ZoomFactor * ZoomStep);
            }
            else if (buttonDown.ButtonIndex == MouseButton.WheelDown)
            {
                Zoom(contentScale - ZoomFactor * ZoomStep);
            }
        }
        else if (@event is InputEventMouseMotion motion && dragging)
        {
            // Inverse of the content's scale as a speed multiplier to make panning faster when zoomed out and
            // vice versa.
            var scaleInverse = 1 / contentScale;

            ImmediatePan(new Vector2(ScrollHorizontal - (int)(motion.Relative.X * scaleInverse) * DragSensitivity,
                ScrollVertical - (int)(motion.Relative.Y * scaleInverse) * DragSensitivity));
        }
    }

    public override void _Input(InputEvent @event)
    {
        if (!Visible || centering)
        {
            dragging = false;
            return;
        }

        if (@event is InputEventMouseButton
            {
                Pressed: false, ButtonIndex: MouseButton.Left or MouseButton.Right,
            })
        {
            dragging = false;
        }
    }

    public void Zoom(float value, float lerpDuration = 0.1f)
    {
        zooming = true;

        var tween = CreateTween();
        tween.SetTrans(Tween.TransitionType.Sine);
        tween.SetEase(Tween.EaseType.Out);

        tween.TweenMethod(new Callable(this, nameof(ImmediateZoom)), contentScale, value, lerpDuration);
    }

    public void Pan(Vector2 coordinates, Action? onPanned = null, float lerpDuration = 0.1f)
    {
        var initial = new Vector2(ScrollHorizontal, ScrollVertical);

        var tween = CreateTween();
        tween.SetTrans(Tween.TransitionType.Sine);
        tween.SetEase(Tween.EaseType.Out);

        tween.TweenMethod(new Callable(this, nameof(ImmediatePan)), initial, coordinates, lerpDuration);

        if (onPanned != null)
            tween.TweenCallback(Callable.From(onPanned));
    }

    public void ResetZoom()
    {
        Zoom(1, 1.0f);
    }

    public void CenterTo(Vector2 coordinates, bool smoothed)
    {
        var viewCoords = coordinates - GetRect().End / 2.0f;

        if (smoothed)
        {
            centering = true;
            Pan(viewCoords, () => centering = false, 1.0f);
        }
        else
        {
            ImmediatePan(viewCoords);
        }

        ResetZoom();
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            ContentPath?.Dispose();
        }

        base.Dispose(disposing);
    }

    private void ImmediateZoom(float value)
    {
        contentScale = value;
        QueueRedraw();
    }

    private void ImmediatePan(Vector2 coordinates)
    {
        ScrollHorizontal = (int)coordinates.X;
        ScrollVertical = (int)coordinates.Y;
    }

    private void UpdateScrollbars()
    {
        GetHScrollBar().Scale = GetVScrollBar().Scale = ShowScrollbars ? Vector2.One : Vector2.Zero;
    }

    private void OnScrollStarted(float value)
    {
        _ = value;

        if (zooming)
        {
            ScrollHorizontal = lastScrollValues.X;
            ScrollVertical = lastScrollValues.Y;
            zooming = false;
        }
        else
        {
            lastScrollValues = new Vector2I(ScrollHorizontal, ScrollVertical);
        }
    }
}
