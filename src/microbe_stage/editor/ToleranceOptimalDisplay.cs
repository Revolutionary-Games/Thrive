using System;
using Godot;

/// <summary>
///  Displays a tolerance with an optimal value in an intuitive way
/// </summary>
public partial class ToleranceOptimalDisplay : HSlider
{
    private readonly Color mainColor = Color.FromHtml("#11FFD5");

    [Export]
    private bool showMiddleMarker;

    [Export]
    private RangeMarker drawLineAtMarker;

#pragma warning disable CA2213

    [Export]
    private ToleranceOptimalMarker optimalValueMarker = null!;

    [Export]
    private Control upperBound = null!;

    [Export]
    private Control lowerBound = null!;

#pragma warning restore CA2213

    private float flexibilityPlus;
    private float flexibilityMinus;

    private Color rangeColor;

    public enum RangeMarker
    {
        Lower,
        Middle,
        Upper,
    }

    public float SliderGrabberPosX { get; set; }

    public override void _Ready()
    {
        rangeColor = mainColor;
    }

    public override void _Draw()
    {
        var mainLineStartPos = new Vector2(0, Size.Y / 2);
        var mainLineEndPos = new Vector2(Size.X, Size.Y / 2);

        var lowerBoundRight = lowerBound.Position + new Vector2(lowerBound.Size.X, lowerBound.Size.Y / 2);
        var lowerBoundLeft = lowerBound.Position + new Vector2(0, lowerBound.Size.Y / 2);

        var upperBoundRight = upperBound.Position + new Vector2(upperBound.Size.X, upperBound.Size.Y / 2);
        var upperBoundLeft = upperBound.Position + new Vector2(0, upperBound.Size.Y / 2);

        var lowerBoundCenter = lowerBound.Position + lowerBound.Size / 2;
        var upperBoundCenter = upperBound.Position + upperBound.Size / 2;
        var middleBoundCenter = new Vector2((float)(Size.X * (Value - MinValue) / (MaxValue - MinValue)), Size.Y / 2);

        var boundOffset = new Vector2(0, 7.5f);

        if (showMiddleMarker)
            DrawLine(middleBoundCenter + new Vector2(0, 3), middleBoundCenter - new Vector2(0, 3), rangeColor, 2);

        DrawLine(mainLineStartPos, lowerBoundLeft, mainColor with { A = 0.25f }, 4);
        DrawLine(upperBoundRight, mainLineEndPos, mainColor with { A = 0.25f }, 4);
        DrawLine(lowerBoundRight, upperBoundLeft, rangeColor with { A = 0.5f }, 4);

        DrawLine(lowerBoundCenter, lowerBoundCenter + boundOffset, rangeColor, 2);
        DrawLine(lowerBoundCenter, lowerBoundCenter - boundOffset, rangeColor, 2);

        DrawLine(upperBoundCenter, upperBoundCenter + boundOffset, rangeColor, 2);
        DrawLine(upperBoundCenter, upperBoundCenter - boundOffset, rangeColor, 2);

        var grabberPos = new Vector2(SliderGrabberPosX, 28);
        var lineStartPos = drawLineAtMarker switch
        {
            RangeMarker.Upper => upperBoundCenter,
            RangeMarker.Middle => middleBoundCenter,
            RangeMarker.Lower => lowerBoundCenter,
            _ => throw new InvalidOperationException(),
        };

        if (Math.Abs(lineStartPos.X - grabberPos.X) <= Mathf.Epsilon)
        {
            DrawLine(lineStartPos, grabberPos, rangeColor, 2);
        }
        else
        {
            var intermediate1 = lineStartPos + Vector2.Down * 9;
            var intermediate2 = grabberPos + Vector2.Up * 11;
            DrawLine(lineStartPos, intermediate1 + Vector2.Down, rangeColor, 2);
            DrawLine(intermediate1, intermediate2, rangeColor with { A = 0.5f }, 2);
            DrawLine(intermediate2 + Vector2.Up, grabberPos, rangeColor, 2);
        }

    }

    public void UpdateMarker(float value)
    {
        optimalValueMarker.OptimalValue = (value - (float)MinValue)
            / (float)(MaxValue - MinValue);
    }

    public void SetBoundPositions(float preferred, float flexibilityPositive, float? flexibilityNegative = null)
    {
        Value = preferred;
        flexibilityPlus = flexibilityPositive;
        flexibilityMinus = flexibilityNegative ?? flexibilityPlus;

        SetBoundPositionsInternal();
    }

    public void SetBoundPositionsManual(double lower, double upper)
    {
        var upperBoundFraction = Math.Clamp((upper - MinValue) / (MaxValue - MinValue), 0, 1);
        var lowerBoundFraction = Math.Clamp((lower - MinValue) / (MaxValue - MinValue), 0, 1);

        lowerBound.Position = new Vector2((Size.X * (float)lowerBoundFraction) - 1,
            lowerBound.Position.Y);

        upperBound.Position = new Vector2((Size.X * (float)upperBoundFraction) - 1,
            upperBound.Position.Y);
    }

    public void SetColorsAndRedraw(Color? color)
    {
        rangeColor = color ?? mainColor;

        QueueRedraw();
    }

    private void SetBoundPositionsInternal()
    {
        SetBoundPositionsManual(Value - flexibilityMinus, Value + flexibilityPlus);
    }
}
