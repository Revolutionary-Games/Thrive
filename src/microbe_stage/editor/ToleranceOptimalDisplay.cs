using Godot;
using System;

/// <summary>
///  Displays a tolerance with an optimal value in an intuitive way
/// </summary>
public partial class ToleranceOptimalDisplay : HSlider
{

#pragma warning disable CA2213

    [Export]
    public ToleranceOptimalMarker OptimalValueMarker = null!;

    [Export]
    private TextureRect upperBound = null!;

    [Export]
    private TextureRect lowerBound = null!;

#pragma warning restore CA2213

    private float flexibilityRange;

    public void UpdateRangePositions(float preferred, float flexibility)
    {
        Value = preferred;
        flexibilityRange = flexibility;

        var upperBoundValue = Value + flexibility;
        var lowerBoundValue = Value - flexibility;

        var upperBoundFraction = Math.Clamp(upperBoundValue / MaxValue, 0, 1);
        var lowerBoundFraction = Math.Clamp(lowerBoundValue / MaxValue, 0, 1);

        lowerBound.Position = new Vector2((Size.X - lowerBound.Size.X + 0.5f) * (float)lowerBoundFraction - 0.5f,
            lowerBound.Position.Y);

        upperBound.Position = new Vector2((Size.X - upperBound.Size.X + 0.5f) * (float)upperBoundFraction - 0.5f,
            upperBound.Position.Y);
    }

    private void SetColor(Color color)
    {
        upperBound.Modulate = color;
        lowerBound.Modulate = color;
    }
}
