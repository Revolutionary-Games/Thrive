using System;
using Godot;

/// <summary>
///   Shows to the player their current heat as well as left and right heat bounds
/// </summary>
public partial class HeatAccumulationBar : VBoxContainer
{
    [Export]
    public float IndicatorImageCenterOffset = 8;

#pragma warning disable CA2213
    [Export]
    private Control leftIndicator = null!;

    [Export]
    private Control middleIndicator = null!;

    [Export]
    private Control rightIndicator = null!;

    [Export]
    private Control currentIndicator = null!;

    [Export]
    private Control currentPositionImage = null!;

#pragma warning restore CA2213

    public override void _Ready()
    {
    }

    public override void _Process(double delta)
    {
        // TODO: smooth animation?
    }

    public void UpdateHeat(float currentHeat, float environmentHeat, float leftMarker, float middleMarker,
        float rightMarker)
    {
        // Scale all values to be between 0 and 1
        throw new NotImplementedException();
    }
}
