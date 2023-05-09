using Godot;

/// <summary>
///   Shows the info and controls for a single city
/// </summary>
public class CityScreen : CustomDialog
{
    [Export]
    public NodePath? ShortStatsLabelPath;

    [Export]
    public NodePath AvailableBuildingsContainerPath = null!;

    [Export]
    public NodePath ConstructedBuildingsContainerPath = null!;

    [Export]
    public NodePath BuildQueueContainerPath = null!;

#pragma warning disable CA2213
    private Label shortStatsLabel = null!;

    private Container availableBuildingsContainer = null!;

    private Container constructedBuildingsContainer = null!;

    private Container buildQueueContainer = null!;
#pragma warning restore CA2213

    private PlacedCity? managedCity;

    private float elapsed = 1;

    public override void _Ready()
    {
        base._Ready();

        shortStatsLabel = GetNode<Label>(ShortStatsLabelPath);

        availableBuildingsContainer = GetNode<Container>(AvailableBuildingsContainerPath);

        constructedBuildingsContainer = GetNode<Container>(ConstructedBuildingsContainerPath);

        buildQueueContainer = GetNode<Container>(BuildQueueContainerPath);
    }

    public override void _Process(float delta)
    {
        base._Process(delta);

        if (!Visible || managedCity == null)
            return;

        elapsed += delta;

        if (elapsed > Constants.CITY_SCREEN_UPDATE_INTERVAL)
        {
            elapsed = 0;

            UpdateAllCityInfo();
        }
    }

    /// <summary>
    ///   Opens this screen for a city
    /// </summary>
    /// <param name="city">The city to open this for</param>
    public void ShowForCity(PlacedCity city)
    {
        if (Visible)
        {
            Close();
        }

        managedCity = city;
        elapsed = 1;

        UpdateAllCityInfo();
        Show();
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            if (ShortStatsLabelPath != null)
            {
                ShortStatsLabelPath.Dispose();
                AvailableBuildingsContainerPath.Dispose();
                ConstructedBuildingsContainerPath.Dispose();
                BuildQueueContainerPath.Dispose();
            }
        }

        base.Dispose(disposing);
    }

    private void UpdateAllCityInfo()
    {
        UpdateCityStats();
        UpdateAvailableBuildings();
        UpdateBuildQueue();
        UpdateConstructedBuildings();
    }

    private void UpdateCityStats()
    {
        WindowTitle = managedCity!.CityName;

        // TODO: research speed, see the TODO in PlacedCity.ProcessResearch
        float researchSpeed = -1;

        var foodBalance = managedCity.CalculateFoodProduction() - managedCity.CalculateFoodConsumption();

        // Update the bottom stats bar
        shortStatsLabel.Text = TranslationServer.Translate("CITY_SHORT_STATISTICS")
            .FormatSafe(StringUtils.ThreeDigitFormat(managedCity.Population),
                StringUtils.FormatPositiveWithLeadingPlus(StringUtils.ThreeDigitFormat(foodBalance), foodBalance),
                researchSpeed);
    }

    private void UpdateAvailableBuildings()
    {
        availableBuildingsContainer.QueueFreeChildren();

        // TODO: update this
    }

    private void UpdateBuildQueue()
    {
        buildQueueContainer.QueueFreeChildren();

        // TODO: update this
    }

    private void UpdateConstructedBuildings()
    {
        constructedBuildingsContainer.QueueFreeChildren();

        // TODO: update this
    }
}
