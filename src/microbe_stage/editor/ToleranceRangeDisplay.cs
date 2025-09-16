using System;
using Godot;

/// <summary>
///   Displays a tolerance with an optimal value in an intuitive way
/// </summary>
public partial class ToleranceRangeDisplay : HSlider
{
    private const float LINE_WIDTH = Constants.TOLERANCE_DISPLAY_MARKER_WIDTH;

    private readonly Color mainColor = Color.FromHtml("#11FFD5");
    private readonly Color mainColorTranslucent = Color.FromHtml("#11FFD5") with { A = 0.5f };

    [Export]
    private bool showMiddleMarker;

    [Export]
    private RangeMarker beginConnectorFromMarker;

#pragma warning disable CA2213

    [Export]
    private ToleranceOptimalMarker optimalValueMarker = null!;

#pragma warning restore CA2213

    private float upperBoundPos;
    private float lowerBoundPos;

    private float flexibilityPlus;
    private float flexibilityMinus;

    private Color rangeColor;
    private Color rangeColorTranslucent;

    public enum RangeMarker
    {
        Lower,
        Middle,
        Upper,
    }

    /// <summary>
    ///   The X position of the related slider's grabber. In local coordinates.
    /// </summary>
    public float SliderGrabberPosX { get; set; }

    public override void _Ready()
    {
        rangeColor = mainColor;
        rangeColorTranslucent = mainColorTranslucent;
    }

    public override void _Draw()
    {
        var mainLineStartPos = new Vector2(0, Size.Y / 2);
        var mainLineEndPos = new Vector2(Size.X, Size.Y / 2);

        var lowerBoundCenter = new Vector2(lowerBoundPos + LINE_WIDTH / 2, Size.Y / 2);
        var upperBoundCenter = new Vector2(upperBoundPos + LINE_WIDTH / 2, Size.Y / 2);
        var middleBoundCenter = new Vector2((float)(Size.X * (Value - MinValue) / (MaxValue - MinValue)), Size.Y / 2);

        var boundOffset = new Vector2(0, Constants.TOLERANCE_DISPLAY_BOUND_HEIGHT / 2);

        // Lower bound
        DrawLine(lowerBoundCenter, lowerBoundCenter + boundOffset, rangeColor, LINE_WIDTH);
        DrawLine(lowerBoundCenter, lowerBoundCenter - boundOffset, rangeColor, LINE_WIDTH);

        // Upper bound
        DrawLine(upperBoundCenter, upperBoundCenter + boundOffset, rangeColor, LINE_WIDTH);
        DrawLine(upperBoundCenter, upperBoundCenter - boundOffset, rangeColor, LINE_WIDTH);

        // Middle
        if (showMiddleMarker)
        {
            var middleOffset = new Vector2(0, Constants.TOLERANCE_DISPLAY_MIDDLE_HEIGHT / 2);
            DrawLine(middleBoundCenter + middleOffset, middleBoundCenter - middleOffset, rangeColor,
                LINE_WIDTH);
        }

        // Main line
        DrawLine(mainLineStartPos, lowerBoundCenter, mainColorTranslucent,
            Constants.TOLERANCE_DISPLAY_MAIN_LINE_WIDTH);
        DrawLine(upperBoundCenter, mainLineEndPos, mainColorTranslucent,
            Constants.TOLERANCE_DISPLAY_MAIN_LINE_WIDTH);
        DrawLine(lowerBoundCenter, upperBoundCenter, rangeColorTranslucent,
            Constants.TOLERANCE_DISPLAY_MAIN_LINE_WIDTH);

        // Draw connector to slider grabber
        var grabberPos = new Vector2(SliderGrabberPosX, Constants.TOLERANCE_DISPLAY_SLIDER_GRABBER_Y_OFFSET);
        var connectorStart = beginConnectorFromMarker switch
        {
            RangeMarker.Upper => upperBoundCenter,
            RangeMarker.Middle => middleBoundCenter,
            RangeMarker.Lower => lowerBoundCenter,
            _ => throw new InvalidOperationException(),
        };

        if (Math.Abs(connectorStart.X - grabberPos.X) <= MathUtils.EPSILON)
        {
            DrawLine(connectorStart, grabberPos, rangeColor, LINE_WIDTH);
        }
        else
        {
            var bendYPos = connectorStart.Y + boundOffset.Y + Constants.TOLERANCE_DISPLAY_CONNECTOR_BEND_Y_OFFSET;
            var bend1 = new Vector2(connectorStart.X, bendYPos);
            var bend2 = new Vector2(grabberPos.X, bendYPos);

            // Vector.Down and Vector.Up are added here to extend the line a bit to cover the next line completely
            // i.e. make a full corner
            DrawLine(connectorStart, bend1 + Vector2.Down, rangeColor, LINE_WIDTH);
            DrawLine(bend1, bend2, rangeColorTranslucent, LINE_WIDTH);
            DrawLine(bend2 + Vector2.Up, grabberPos, rangeColor, LINE_WIDTH);
        }
    }

    /// <summary>
    ///   Sets the position of the <see cref="optimalValueMarker"/>.
    /// </summary>
    /// <param name="value"> Has to be between this slider's max and min.</param>
    public void UpdateMarker(float value)
    {
        optimalValueMarker.OptimalValue = (value - (float)MinValue)
            / (float)(MaxValue - MinValue);
    }

    /// <summary>
    ///   Sets the positions of the upper and lower bounds based on a middle value.
    ///   All values have to be between this slider's max and min.
    /// </summary>
    /// <param name="preferred">The middle of the range</param>
    /// <param name="flexibilityPositive">The offset of the upper bound from the middle</param>
    /// <param name="flexibilityNegative">The offset of the lower bound from the middle</param>
    public void SetBoundPositions(float preferred, float flexibilityPositive, float? flexibilityNegative = null)
    {
        Value = preferred;
        flexibilityPlus = flexibilityPositive;
        flexibilityMinus = flexibilityNegative ?? flexibilityPlus;

        SetBoundPositionsInternal();
    }

    /// <summary>
    ///   Manually sets the positions of the upper and lower bounds. Does not update the middle value.
    ///   All values have to be between this slider's max and min.
    /// </summary>
    /// <param name="lower">Position of the lower bound</param>
    /// <param name="upper">Position of the upper bound</param>
    public void SetBoundPositionsManual(float lower, float upper)
    {
        var upperBoundFraction = Math.Clamp((upper - MinValue) / (MaxValue - MinValue), 0, 1);
        var lowerBoundFraction = Math.Clamp((lower - MinValue) / (MaxValue - MinValue), 0, 1);

        lowerBoundPos = Size.X * (float)lowerBoundFraction - 1;

        upperBoundPos = Size.X * (float)upperBoundFraction - 1;
    }

    /// <summary>
    ///   Sets the color of the range between the upper and lower bounds and queues a redraw.
    /// </summary>
    /// <param name="color">New color. If null, will default to <see cref="mainColor"/></param>
    public void SetColorsAndRedraw(Color? color)
    {
        rangeColor = color ?? mainColor;
        rangeColorTranslucent = rangeColor with { A = 0.5f };

        QueueRedraw();
    }

    private void SetBoundPositionsInternal()
    {
        SetBoundPositionsManual((float)(Value - flexibilityMinus), (float)(Value + flexibilityPlus));
    }
}
