using Godot;
using Object = Godot.Object;

public class SlideshowScreen : CustomDialog
{
    private Tween tween = new();

    public GalleryItem FocusedItem { get; set; } = null!;

    public override void _Ready()
    {
        AddChild(tween);
    }

    public override void CustomShow()
    {
        // TODO: Clean this up

        base.CustomShow();

        RectClipContent = true;

        var focusedItemRect = FocusedItem.GetGlobalRect();
        RectGlobalPosition = focusedItemRect.Position;
        RectSize = focusedItemRect.Size;

        tween.InterpolateProperty(
            this, "rect_position", null, GetFullRect().Position, 0.2f, Tween.TransitionType.Sine, Tween.EaseType.Out);
        tween.InterpolateProperty(
            this, "rect_size", null, GetFullRect().Size, 0.2f, Tween.TransitionType.Sine, Tween.EaseType.Out);
        tween.Start();

        if (!tween.IsConnected("tween_completed", this, nameof(OnScaledUp)))
            tween.Connect("tween_completed", this, nameof(OnScaledUp), null, (uint)ConnectFlags.Oneshot);
    }

    public override void CustomHide()
    {
        // TODO: Clean this up

        FullRect = false;
        RectClipContent = true;

        var focusedItemRect = FocusedItem.GetGlobalRect();

        tween.InterpolateProperty(this, "rect_position", null, focusedItemRect.Position, 0.2f,
            Tween.TransitionType.Sine, Tween.EaseType.Out);
        tween.InterpolateProperty(
            this, "rect_size", null, focusedItemRect.Size, 0.2f, Tween.TransitionType.Sine, Tween.EaseType.Out);
        tween.Start();

        if (!tween.IsConnected("tween_completed", this, nameof(OnScaledDown)))
            tween.Connect("tween_completed", this, nameof(OnScaledDown), null, (uint)ConnectFlags.Oneshot);
    }

    private void OnScaledUp(Object @object, NodePath key)
    {
        RectClipContent = false;
        FullRect = true;
    }

    private void OnScaledDown(Object @object, NodePath key)
    {
        RectClipContent = false;
        Hide();
    }
}
