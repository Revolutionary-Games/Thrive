using Godot;

/// <summary>
///   A custom <see cref="Control"/> that displays a list of options. Opening and closing can be animated.
///   For the built-in engine version, see <see cref="PopupMenu"/>.
/// </summary>
public partial class CustomPopupMenu : TopLevelContainer
{
    [Export]
    public NodePath? PanelContainerPath;

    [Export]
    public NodePath ContainerPath = null!;

#pragma warning disable CA2213 // Disposable fields should be disposed
    private PanelContainer panelContainer = null!;
    private Container container = null!;
#pragma warning restore CA2213 // Disposable fields should be disposed

    public override void _Ready()
    {
        panelContainer = GetNode<PanelContainer>(PanelContainerPath);
        container = GetNode<Container>(ContainerPath);

        ResolveNodeReferences();
        RemapDynamicChildren();
    }

    public void RemapDynamicChildren()
    {
        foreach (Control child in GetChildren())
        {
            if (child.Equals(panelContainer))
                continue;

            child.ReParent(container);
        }
    }

    protected virtual void ResolveNodeReferences()
    {
    }

    protected override void OnOpen()
    {
        CreateTween().TweenProperty(this, "scale", Vector2.One, 0.2)
            .From(Vector2.Zero)
            .SetTrans(Tween.TransitionType.Circ)
            .SetEase(Tween.EaseType.Out);
    }

    protected override void OnClose()
    {
        var tween = CreateTween();
        tween.TweenProperty(this, "scale", Vector2.Zero, 0.15)
            .From(Vector2.One)
            .SetTrans(Tween.TransitionType.Circ)
            .SetEase(Tween.EaseType.Out);
        tween.TweenCallback(new Callable(this, nameof(OnClosingAnimationFinished)));
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            if (PanelContainerPath != null)
            {
                PanelContainerPath.Dispose();
                ContainerPath.Dispose();
            }
        }

        base.Dispose(disposing);
    }
}
