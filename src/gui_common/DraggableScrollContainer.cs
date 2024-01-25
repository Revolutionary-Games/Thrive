using System;
using Godot;
using Object = Godot.Object;

/// <summary>
///   A scroll container which can be also moved by clicking and dragging.
/// </summary>
public class DraggableScrollContainer : ScrollContainer
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
    private Tween tween = null!;
#pragma warning restore CA2213

    private bool showScrollbars;
    private float contentScale = 1;

    private Action? onPanned;

    /// <summary>
    ///   Unwanted scrolling happens while zooming with mouse wheel, this is used to reset the scroll values back to
    ///   its last state before zooming started.
    /// </summary>
    private Int2 lastScrollValues;

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
        tween = new Tween();
        AddChild(tween);

        // Workaround a bug in Godot (https://github.com/godotengine/godot/issues/22936).
        GetVScrollbar().Connect("value_changed", this, nameof(OnScrollStarted));
        GetHScrollbar().Connect("value_changed", this, nameof(OnScrollStarted));

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

        content.RectPivotOffset = new Vector2(ScrollHorizontal, ScrollVertical) + GetRect().End * 0.5f;
        contentScale = Mathf.Clamp(contentScale, MinZoom, MaxZoom);
        var pairValue = new Vector2(contentScale, contentScale);

        if (content.RectScale != pairValue)
            content.RectScale = pairValue;
    }

    public override void _GuiInput(InputEvent @event)
    {
        if (!Visible || centering)
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
                Zoom(contentScale + ZoomFactor * ZoomStep);
            }
            else if (buttonDown.ButtonIndex == (int)ButtonList.WheelDown)
            {
                Zoom(contentScale - ZoomFactor * ZoomStep);
            }
        }
        else if (@event is InputEventMouseMotion motion && dragging)
        {
            // Inverse of the content's scale as a speed multiplier to make panning faster when zoomed out and
            // vice versa.
            var scaleInverse = 1 / contentScale;

            ImmediatePan(new Vector2(ScrollHorizontal - (int)(motion.Relative.x * scaleInverse) * DragSensitivity,
                ScrollVertical - (int)(motion.Relative.y * scaleInverse) * DragSensitivity));
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
                Pressed: false, ButtonIndex: (int)ButtonList.Left or (int)ButtonList.Right,
            })
        {
            dragging = false;
        }
    }

    public void Zoom(float value, float lerpDuration = 0.1f)
    {
        zooming = true;
        tween.InterpolateMethod(this, nameof(ImmediateZoom), contentScale, value, lerpDuration,
            Tween.TransitionType.Sine, Tween.EaseType.Out);
        tween.Start();
    }

    public void Pan(Vector2 coordinates, Action? onPanned = null, float lerpDuration = 0.1f)
    {
        var initial = new Vector2(ScrollHorizontal, ScrollVertical);
        tween.InterpolateMethod(this, nameof(ImmediatePan), initial, coordinates, lerpDuration,
            Tween.TransitionType.Sine, Tween.EaseType.Out);
        tween.Start();

        this.onPanned = onPanned;
        tween.CheckAndConnect("tween_completed", this, nameof(OnPanningStopped), null, (uint)ConnectFlags.Oneshot);
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

    private void ImmediateZoom(float value)
    {
        contentScale = value;
        Update();
    }

    private void ImmediatePan(Vector2 coordinates)
    {
        ScrollHorizontal = (int)coordinates.x;
        ScrollVertical = (int)coordinates.y;
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
        }
        else
        {
            lastScrollValues = new Int2(ScrollHorizontal, ScrollVertical);
        }
    }

    private void OnPanningStopped(Object @object, NodePath key)
    {
        _ = @object;
        _ = key;

        onPanned?.Invoke();
    }
}
