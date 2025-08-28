using System;
using Godot;

/// <summary>
///  Displays a tolerance with an optimal value in an intuitive way
/// </summary>
public partial class ToleranceOptimalDisplay : HSlider
{
#pragma warning disable CA2213

    [Export]
    private ToleranceOptimalMarker optimalValueMarker = null!;

    [Export]
    private TextureRect upperBound = null!;

    [Export]
    private TextureRect lowerBound = null!;

#pragma warning restore CA2213

    private float flexibilityRange;

    public void SetBoundPositions(float preferred, float flexibility)
    {
        Value = preferred;
        flexibilityRange = flexibility;

        SetBoundPositions();
    }

    public void UpdateMarker(float value)
    {
        optimalValueMarker.OptimalValue = (value - (float)MinValue)
            / (float)(MaxValue - MinValue);
    }

    private void SetBoundPositions()
    {
        var upperBoundValue = Value + flexibilityRange;
        var lowerBoundValue = Value - flexibilityRange;

        var upperBoundFraction = Math.Clamp(upperBoundValue / MaxValue, 0, 1);
        var lowerBoundFraction = Math.Clamp(lowerBoundValue / MaxValue, 0, 1);

        lowerBound.Position = new Vector2((Size.X - 1) * (float)lowerBoundFraction,
            lowerBound.Position.Y);

        upperBound.Position = new Vector2((Size.X - 1) * (float)upperBoundFraction,
            upperBound.Position.Y);
    }

    private void SetColor(Color color)
    {
        upperBound.Modulate = color;
        lowerBound.Modulate = color;
    }
}
