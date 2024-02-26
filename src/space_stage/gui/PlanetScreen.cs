using Godot;

/// <summary>
///   Shows info about a single planet
/// </summary>
public partial class PlanetScreen : CustomWindow
{
    [Export]
    public NodePath? ShortStatsLabelPath;

    [Export]
    public NodePath GodToolsButtonPath = null!;

#pragma warning disable CA2213
    private Label shortStatsLabel = null!;
    private Button godToolsButton = null!;
#pragma warning restore CA2213

    private EntityReference<PlacedPlanet>? managedPlanet;

    private double elapsed = 1;

    [Signal]
    public delegate void OnOpenGodToolsEventHandler(GodotObject unit);

    public override void _Ready()
    {
        base._Ready();

        shortStatsLabel = GetNode<Label>(ShortStatsLabelPath);
        godToolsButton = GetNode<Button>(GodToolsButtonPath);
    }

    public override void _Process(double delta)
    {
        base._Process(delta);

        if (!Visible)
            return;

        elapsed += delta;

        if (elapsed > Constants.PLANET_SCREEN_UPDATE_INTERVAL)
        {
            if (managedPlanet?.Value == null)
            {
                GD.Print("Closing planet screen with missing planet");
                Close();
                return;
            }

            elapsed = 0;

            UpdateAllPlanetInfo();
        }
    }

    /// <summary>
    ///   Opens this screen for a planet
    /// </summary>
    /// <param name="planet">The planet to open this for</param>
    /// <param name="showGodTools">If true shows the god tools for a planet</param>
    public void ShowForPlanet(PlacedPlanet planet, bool showGodTools)
    {
        if (Visible)
        {
            Close();
        }

        managedPlanet = new EntityReference<PlacedPlanet>(planet);
        elapsed = 1;

        UpdateAllPlanetInfo();
        Show();

        godToolsButton.Visible = showGodTools;
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            if (ShortStatsLabelPath != null)
            {
                ShortStatsLabelPath.Dispose();
                GodToolsButtonPath.Dispose();
            }
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
        var target = managedPlanet?.Value;

        if (target == null)
            return;

        WindowTitle = target.PlanetName;

        // TODO: research speed, see the TODO in PlacedPlanet.ProcessResearch
        float researchSpeed = -1;

        var foodBalance = target.CalculateFoodProduction() - target.CalculateFoodConsumption();

        // Update the bottom stats bar
        shortStatsLabel.Text = TranslationServer.Translate("CITY_SHORT_STATISTICS")
            .FormatSafe(StringUtils.ThreeDigitFormat(target.Population),
                StringUtils.FormatPositiveWithLeadingPlus(StringUtils.ThreeDigitFormat(foodBalance), foodBalance),
                researchSpeed);
    }

    private void ForwardGodTools()
    {
        var target = managedPlanet?.Value;
        if (target == null)
            return;

        GUICommon.Instance.PlayButtonPressSound();
        EmitSignal(nameof(OnOpenGodToolsEventHandler), target);
    }
}
