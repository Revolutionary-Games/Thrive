using Godot;

/// <summary>
///   A custom <see cref="Control"/> that displays a list of options. Opening and closing can be animated.
///   For the built-in engine version, see <see cref="PopupMenu"/>.
/// </summary>
public class CustomPopupMenu : TopLevelContainer
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

    /// <summary>
    ///   Calculates the size the popup should be taking into account minimum
    ///   height needed by content
    /// </summary>
    /// <returns>Returns what should be the size of popup considering content height</returns>
    protected virtual Vector2 CalculateSize()
    {
        RectSize = cachedMinSize;

        var clipControl = GetNodeOrNull<Control>("Panel/Control");
        var clipControlHeightMargin = 0.0f;
        if (clipControl != null)
        {
            clipControlHeightMargin =
                Mathf.Abs(clipControl.MarginTop) +
                Mathf.Abs(clipControl.MarginBottom);
        }

        // Apply the margins to base size to get parent size. It uses the absolute value of the margins because when
        // however many units of negative margin is removed from a size those so many units are added to the parent
        // nodes size. Given we're calculating the size of the parent-most node, adding the absolute of all the
        // childrens margins will give the appropriate size.
        var contentSize = new Vector2(
            RectSize.x,
            container.RectSize.y +
            Mathf.Abs(container.MarginTop) +
            Mathf.Abs(container.MarginBottom) +
            clipControlHeightMargin);

        var minSize = new Vector2(
            Mathf.Max(contentSize.x, cachedMinSize.x),
            Mathf.Max(contentSize.y, cachedMinSize.y));

        return minSize;
    }

    /// <summary>
    /// Not entirely sure why this works, but it is suspected to force an update to the size (at the time of writing).
    /// </summary>
    protected void ContainerSizeProblemWorkaround()
    {
        container.Hide();
        container.Show();
    }

    protected override void OnOpen()
    {
        RectSize = CalculateSize();

        CreateTween().TweenProperty(this, "rect_scale", Vector2.One, 0.2f)
            .From(Vector2.Zero)
            .SetTrans(Tween.TransitionType.Circ)
            .SetEase(Tween.EaseType.Out);
    }

    protected override void OnClose()
    {
        var tween = CreateTween();
        tween.TweenProperty(this, "rect_scale", Vector2.Zero, 0.15f)
            .From(Vector2.One)
            .SetTrans(Tween.TransitionType.Circ)
            .SetEase(Tween.EaseType.Out);
        tween.TweenCallback(this, nameof(OnClosingAnimationFinished));
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
