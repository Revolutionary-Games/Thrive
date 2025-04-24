using Godot;

/// <summary>
///   Shows to the player their current heat as well as left and right heat bounds
/// </summary>
public partial class HeatAccumulationBar : VBoxContainer
{
    [Export]
    public float IndicatorImageCenterOffset = 8;

    [Export]
    public float IndicatorStickGoodStateSeconds = 0.3f;

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

    private double goodIndicatorTime;

    private bool firstUpdate = true;

    public override void _Ready()
    {
    }

    public override void _Process(double delta)
    {
        if (goodIndicatorTime > 0)
        {
            goodIndicatorTime -= delta;

            if (goodIndicatorTime <= 0)
            {
                currentPositionImage.SelfModulate = Colors.White;
            }
        }

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

    /// <summary>
    ///   Call with true when the indicator should be turned to a good colour. The colour sticks for a little bit even
    ///   when this is called with false to make sure the colour doesn't flicker.
    /// </summary>
    /// <param name="goingUp">True when the indicator should be marked as good</param>
    public void UpdateIndicator(bool goingUp)
    {
        if (goingUp)
        {
            currentPositionImage.SelfModulate = PositiveIndicatorDirectionColour;
            goodIndicatorTime = IndicatorStickGoodStateSeconds;
        }
    }
}
