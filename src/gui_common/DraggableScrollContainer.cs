using Godot;

/// <summary>
///   A scroll container which can be also moved by clicking and dragging.
/// </summary>
public class DraggableScrollContainer : ScrollContainer
{
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
    private SceneTreeTimer? timer;

    public override void _Ready()
    {
        GetVScrollbar().Connect("scrolling", this, "OnScrollStarted");
        GetHScrollbar().Connect("scrolling", this, "OnScrollStarted");

        base._Ready();
    }

    public override void _Input(InputEvent @event)
    {
        if (!Visible || scrolling)
        {
            dragging = false;
            return;
        }

        var mouse = GetGlobalMousePosition();
        var min = RectGlobalPosition;
        var max = min + RectSize;

        // Don't allow click and drag outside this container
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
        else if (@event is InputEventMouseMotion input && dragging)
        {
            ScrollHorizontal -= (int)input.Relative.x;
            ScrollVertical -= (int)input.Relative.y;
        }
        else if (@event is InputEventMouseButton
                 {
                     Pressed: false, ButtonIndex: (int)ButtonList.Left or (int)ButtonList.Right,
                 })
        {
            dragging = false;
        }
    }

    private void OnScrollStarted()
    {
        if (timer == null)
        {
            scrolling = true;
            timer = GetTree().CreateTimer(0.1f);
            timer.Connect("timeout", this, "OnScrollEnded");
        }
    }

    private void OnScrollEnded()
    {
        scrolling = false;
        timer = null;
    }
}
