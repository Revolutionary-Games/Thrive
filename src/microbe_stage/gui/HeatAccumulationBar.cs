using Godot;

/// <summary>
///   Shows to the player their current heat as well as left and right heat bounds
/// </summary>
public partial class HeatAccumulationBar : VBoxContainer
{
    [Export]
    public float IndicatorImageCenterOffset = 8;

    [Export]
    public Color PositiveIndicatorDirectionColour = Colors.LightSeaGreen;

#pragma warning disable CA2213
    [Export]
    private TextureProgressBar progressBar = null!;

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

    private bool firstUpdate = true;

    public override void _Ready()
    {
    }

    public override void _Process(double delta)
    {
        // TODO: smooth animation?
    }

    public void UpdateHeat(float currentHeat, float environmentHeat, float leftMarker, float middleMarker,
        float rightMarker, float max)
    {
        if (firstUpdate)
        {
            firstUpdate = false;
            leftIndicator.Visible = true;
            middleIndicator.Visible = true;
            rightIndicator.Visible = true;
            currentIndicator.Visible = true;
        }

        // Ensure max is correct
        if (currentHeat > max)
            max = currentHeat;

        if (environmentHeat > max)
            max = environmentHeat;

        // Ensure markers are in correct order
        if (middleMarker > rightMarker)
            middleMarker = rightMarker;

        if (leftMarker > middleMarker)
            leftMarker = middleMarker;

        // And do one last update for the max values
        if (rightMarker > max)
            max = rightMarker;

        // Scale all values to be between 0 and 1
        currentHeat /= max;
        environmentHeat /= max;
        leftMarker /= max;
        middleMarker /= max;
        rightMarker /= max;

        // Then apply positions
        progressBar.Value = currentHeat;

        var width = Size.X;

        leftIndicator.OffsetLeft = leftMarker * width;
        middleIndicator.OffsetLeft = middleMarker * width;
        rightIndicator.OffsetLeft = rightMarker * width;
        currentIndicator.OffsetLeft = environmentHeat * width;

        currentPositionImage.OffsetLeft = currentHeat * width - IndicatorImageCenterOffset;
    }

    public void UpdateIndicator(bool goingUp)
    {
        if (goingUp)
        {
            currentPositionImage.SelfModulate = PositiveIndicatorDirectionColour;
        }
        else
        {
            currentPositionImage.SelfModulate = Colors.White;
        }
    }
}
