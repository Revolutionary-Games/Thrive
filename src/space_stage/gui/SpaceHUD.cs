using Godot;

/// <summary>
///   HUD for the space stage. Very similar to <see cref="SocietyHUD"/>
/// </summary>
public class SpaceHUD : StrategyStageHUDBase<SpaceStage>
{
    // TODO: merge the common parts with the society stage hud into its own sub-scenes
    [Export]
    public NodePath? PopulationLabelPath;

    [Export]
    public NodePath PlanetScreenPopupPath = null!;

    [Export]
    public NodePath FleetPopupPath = null!;

#pragma warning disable CA2213
    private Label populationLabel = null!;

    private PlanetScreen planetScreenPopup = null!;
    private SpaceFleetInfoPopup fleetPopup = null!;
#pragma warning restore CA2213

    // TODO: real button referencing text for this
    protected override string UnPauseHelpText => "TODO: unpause text for this stage";

    public override void _Ready()
    {
        base._Ready();

        populationLabel = GetNode<Label>(PopulationLabelPath);
        planetScreenPopup = GetNode<PlanetScreen>(PlanetScreenPopupPath);
        fleetPopup = GetNode<SpaceFleetInfoPopup>(FleetPopupPath);
    }

    public void UpdatePopulationDisplay(long population)
    {
        populationLabel.Text = StringUtils.ThreeDigitFormat(population);
    }

    public void OpenPlanetScreen(PlacedPlanet planet)
    {
        planetScreenPopup.ShowForPlanet(planet);
    }

    public void OpenFleetInfo(SpaceFleet fleet)
    {
        fleetPopup.ShowForUnit(fleet);
    }

    public void CloseFleetInfo()
    {
        fleetPopup.Close();
    }

    public SpaceFleet? GetSelectedFleet()
    {
        return fleetPopup.OpenedForUnit;
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            if (PopulationLabelPath != null)
            {
                PopulationLabelPath.Dispose();
                PlanetScreenPopupPath.Dispose();
                FleetPopupPath.Dispose();
            }
        }

        base.Dispose(disposing);
    }
}
