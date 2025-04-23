using System;
using Godot;

/// <summary>
///   Highlights a target Control by blanking out other areas of the screen
/// </summary>
public partial class ControlHighlight : Control
{
    /// <summary>
    ///   When true the parent Control (this must be the child of a Control) is used as the window size.
    ///   Works when playing at smaller resolution than project minimum resolution.
    /// </summary>
    [Export]
    public bool UseParentControlSizeAsWindowSize = true;

#pragma warning disable CA2213
    [Export]
    private Control leftPlane = null!;
    [Export]
    private Control topPlane = null!;
    [Export]
    private Control rightPlane = null!;
    [Export]
    private Control bottomPlane = null!;
#pragma warning restore CA2213

    /// <summary>
    ///   The control that is highlighted by this object
    /// </summary>
    public Control? TargetControl { get; set; }

    public override void _Process(double delta)
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
            var screenSize = UseParentControlSizeAsWindowSize ? ((Control)GetParent()).Size : GetWindow().Size;
            var screenHeight = screenSize.Y;
            var screenWidth = screenSize.X;

            var nonCoveredArea = TargetControl!.GetGlobalRect();

            leftPlane.GlobalPosition = new Vector2(0, 0);
            leftPlane.Size = new Vector2(MathF.Ceiling(nonCoveredArea.Position.X), screenHeight);

            var rightWidth = screenWidth - nonCoveredArea.End.X;
            rightPlane.GlobalPosition = new Vector2(screenWidth - rightWidth, 0);
            rightPlane.Size = new Vector2(rightWidth, screenHeight);

            var middleWidth = nonCoveredArea.Size.X;

            topPlane.GlobalPosition = new Vector2(nonCoveredArea.Position.X, 0);
            topPlane.Size = new Vector2(middleWidth, nonCoveredArea.Position.Y);

            var bottomHeight = screenHeight - nonCoveredArea.End.Y;
            bottomPlane.GlobalPosition = new Vector2(nonCoveredArea.Position.X, screenHeight - bottomHeight);
            bottomPlane.Size = new Vector2(middleWidth, bottomHeight);
        }
    }
}
