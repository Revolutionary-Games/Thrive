using Godot;

/// <summary>
///   A custom <see cref="Control"/> that displays a list of options. Opening and closing can be animated.
///   For the built-in engine version, see <see cref="PopupMenu"/>.
/// </summary>
public class CustomPopupMenu : CustomWindow
{
    [Export]
    public NodePath? PanelPath;

    [Export]
    public NodePath ContainerPath = null!;

#pragma warning disable CA2213 // Disposable fields should be disposed
    private Panel panel = null!;
    private Container container = null!;
#pragma warning restore CA2213 // Disposable fields should be disposed

    private Vector2 cachedMinSize;

    public override void _Ready()
    {
        panel = GetNode<Panel>(PanelPath);
        container = GetNode<Container>(ContainerPath);

        cachedMinSize = RectMinSize;
        RectMinSize = Vector2.Zero;

        ResolveNodeReferences();
        RemapDynamicChildren();
    }

    public void RemapDynamicChildren()
    {
        foreach (Control child in GetChildren())
        {
            if (child.Equals(panel))
                continue;

            child.ReParent(container);
        }
    }

    protected virtual void ResolveNodeReferences()
    {
    }

    protected override void OnOpen()
    {
        CreateTween().TweenProperty(this, "rect_size", cachedMinSize, 0.2f)
            .From(Vector2.Zero)
            .SetTrans(Tween.TransitionType.Circ)
            .SetEase(Tween.EaseType.Out);
    }

    protected override void OnClose()
    {
        var tween = CreateTween();
        tween.TweenProperty(this, "rect_size", Vector2.Zero, 0.15f)
            .From(cachedMinSize)
            .SetTrans(Tween.TransitionType.Circ)
            .SetEase(Tween.EaseType.Out);
        tween.TweenCallback(this, "hide");
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            if (PanelPath != null)
            {
                PanelPath.Dispose();
                ContainerPath.Dispose();
            }
        }

        base.Dispose(disposing);
    }
}
