using System;
using Godot;

/// <summary>
///  Displays a tolerance with an optimal value in an intuitive way
/// </summary>
public partial class ToleranceOptimalDisplay : HSlider
{
    private readonly Color mainColor = Color.FromHtml("#11FFD5");

#pragma warning disable CA2213

    [Export]
    private ToleranceOptimalMarker optimalValueMarker = null!;

    [Export]
    private TextureRect upperBound = null!;

    [Export]
    private TextureRect lowerBound = null!;

#pragma warning restore CA2213

    private float flexibilityRange;

    private Color rangeColor;

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

        DrawLine(mainLineStartPos, lowerBoundLeft, mainColor with { A = 0.25f }, 4);
        DrawLine(upperBoundRight, mainLineEndPos, mainColor with { A = 0.25f }, 4);
        DrawLine(lowerBoundRight, upperBoundLeft, rangeColor with { A = 0.5f }, 4);
    }

    public void UpdateMarker(float value)
    {
        optimalValueMarker.OptimalValue = (value - (float)MinValue)
            / (float)(MaxValue - MinValue);
    }

    public void SetBoundPositions(float preferred, float flexibility)
    {
        Value = preferred;
        flexibilityRange = flexibility;

        SetBoundPositionsInternal();
    }

    public void SetBoundPositionsManual(double lower, double upper)
    {
        var upperBoundFraction = Math.Clamp((upper - MinValue) / (MaxValue - MinValue), 0, 1);
        var lowerBoundFraction = Math.Clamp((lower - MinValue) / (MaxValue - MinValue), 0, 1);

        lowerBound.Position = new Vector2((Size.X - 1) * (float)lowerBoundFraction,
            lowerBound.Position.Y);

        upperBound.Position = new Vector2((Size.X - 1) * (float)upperBoundFraction,
            upperBound.Position.Y);
    }

    public void SetColorsAndRedraw(Color? color)
    {
        upperBound.Modulate = color ?? mainColor;
        lowerBound.Modulate = color ?? mainColor;
        rangeColor = color ?? mainColor;

        QueueRedraw();
    }

    private void SetBoundPositionsInternal()
    {
        SetBoundPositionsManual(Value - flexibilityRange, Value + flexibilityRange);
    }
}
