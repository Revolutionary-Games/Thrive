using Godot;

/// <summary>
///   Shows info about a single planet
/// </summary>
public class PlanetScreen : CustomDialog
{
    [Export]
    public NodePath? ShortStatsLabelPath;

#pragma warning disable CA2213
    private Label shortStatsLabel = null!;
#pragma warning restore CA2213

    private PlacedPlanet? managedPlanet;

    private float elapsed = 1;

    public override void _Ready()
    {
        base._Ready();

        shortStatsLabel = GetNode<Label>(ShortStatsLabelPath);
    }

    public override void _Process(float delta)
    {
        base._Process(delta);

        if (!Visible || managedPlanet == null)
            return;

        elapsed += delta;

        if (elapsed > Constants.PLANET_SCREEN_UPDATE_INTERVAL)
        {
            elapsed = 0;

            UpdateAllPlanetInfo();
        }
    }

    /// <summary>
    ///   Opens this screen for a planet
    /// </summary>
    /// <param name="planet">The planet to open this for</param>
    public void ShowForPlanet(PlacedPlanet planet)
    {
        if (Visible)
        {
            Close();
        }

        managedPlanet = planet;
        elapsed = 1;

        UpdateAllPlanetInfo();
        Show();
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            ShortStatsLabelPath?.Dispose();
        }

        base.Dispose(disposing);
    }

    private void UpdateAllPlanetInfo()
    {
        UpdatePlanetStats();

        // TODO: implement the city equivalents for this class
        // UpdateAvailableBuildings();
        // UpdateBuildQueue();
        // UpdateConstructedBuildings();
    }

    private void UpdatePlanetStats()
    {
        WindowTitle = managedPlanet!.PlanetName;

        // TODO: research speed, see the TODO in PlacedPlanet.ProcessResearch
        float researchSpeed = -1;

        var foodBalance = managedPlanet.CalculateFoodProduction() - managedPlanet.CalculateFoodConsumption();

        // Update the bottom stats bar
        shortStatsLabel.Text = TranslationServer.Translate("CITY_SHORT_STATISTICS")
            .FormatSafe(StringUtils.ThreeDigitFormat(managedPlanet.Population),
                StringUtils.FormatPositiveWithLeadingPlus(StringUtils.ThreeDigitFormat(foodBalance), foodBalance),
                researchSpeed);
    }
}
