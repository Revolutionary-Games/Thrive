using Godot;

/// <summary>
///   Highlights a target Control by blanking out other areas of the screen
/// </summary>
public class ControlHighlight : Control
{
    [Export]
    public NodePath? LeftPlanePath;

    [Export]
    public NodePath TopPlanePath = null!;

    [Export]
    public NodePath RightPlanePath = null!;

    [Export]
    public NodePath BottomPlanePath = null!;

    /// <summary>
    ///   When true the parent Control (this must be the child of a Control) is used as the window size.
    ///   Works when playing at smaller resolution than project minimum resolution.
    /// </summary>
    [Export]
    public bool UseParentControlSizeAsWindowSize = true;

#pragma warning disable CA2213
    private Control leftPlane = null!;
    private Control topPlane = null!;
    private Control rightPlane = null!;
    private Control bottomPlane = null!;
#pragma warning restore CA2213

    /// <summary>
    ///   The control that is highlighted by this object
    /// </summary>
    public Control? TargetControl { get; set; }

    public override void _Ready()
    {
        leftPlane = GetNode<Control>(LeftPlanePath);
        topPlane = GetNode<Control>(TopPlanePath);
        rightPlane = GetNode<Control>(RightPlanePath);
        bottomPlane = GetNode<Control>(BottomPlanePath);
    }

    public override void _Process(float delta)
    {
        if (!Visible)
            return;

        bool hasTarget = TargetControl != null;

        leftPlane.Visible = hasTarget;
        topPlane.Visible = hasTarget;
        rightPlane.Visible = hasTarget;
        bottomPlane.Visible = hasTarget;

        if (hasTarget)
        {
            var screenSize = UseParentControlSizeAsWindowSize ? ((Control)GetParent()).RectSize : GetViewport().Size;
            var screenHeight = screenSize.y;
            var screenWidth = screenSize.x;

            var nonCoveredArea = TargetControl!.GetGlobalRect();

            leftPlane.RectGlobalPosition = new Vector2(0, 0);
            leftPlane.RectSize = new Vector2(Mathf.Ceil(nonCoveredArea.Position.x), screenHeight);

            var rightWidth = screenWidth - nonCoveredArea.End.x;
            rightPlane.RectGlobalPosition = new Vector2(screenWidth - rightWidth, 0);
            rightPlane.RectSize = new Vector2(rightWidth, screenHeight);

            var middleWidth = nonCoveredArea.Size.x;

            topPlane.RectGlobalPosition = new Vector2(nonCoveredArea.Position.x, 0);
            topPlane.RectSize = new Vector2(middleWidth, nonCoveredArea.Position.y);

            var bottomHeight = screenHeight - nonCoveredArea.End.y;
            bottomPlane.RectGlobalPosition = new Vector2(nonCoveredArea.Position.x, screenHeight - bottomHeight);
            bottomPlane.RectSize = new Vector2(middleWidth, bottomHeight);
        }
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            if (LeftPlanePath != null)
            {
                LeftPlanePath.Dispose();
                TopPlanePath.Dispose();
                RightPlanePath.Dispose();
                BottomPlanePath.Dispose();
            }
        }

        base.Dispose(disposing);
    }
}
