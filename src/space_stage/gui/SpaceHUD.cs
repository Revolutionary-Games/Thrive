using System;
using Godot;

/// <summary>
///   HUD for the space stage. Very similar to <see cref="SocietyHUD"/>
/// </summary>
public class SpaceHUD : StrategyStageHUDBase<SpaceStage>, IStructureSelectionReceiver<SpaceStructureDefinition>
{
    // TODO: merge the common parts with the society stage hud into its own sub-scenes
    [Export]
    public NodePath? PopulationLabelPath;

    [Export]
    public NodePath PlanetScreenPopupPath = null!;

    [Export]
    public NodePath FleetPopupPath = null!;

    [Export]
    public NodePath ConstructionPopupPath = null!;

#pragma warning disable CA2213
    private Label populationLabel = null!;

    private PlanetScreen planetScreenPopup = null!;
    private SpaceFleetInfoPopup fleetPopup = null!;
    private SpaceConstructionPopup constructionPopup = null!;
#pragma warning restore CA2213

    private SpaceFleet? fleetToConstructWith;

    // TODO: real button referencing text for this
    protected override string UnPauseHelpText => "TODO: unpause text for this stage";

    public override void _Ready()
    {
        base._Ready();

        populationLabel = GetNode<Label>(PopulationLabelPath);
        planetScreenPopup = GetNode<PlanetScreen>(PlanetScreenPopupPath);
        fleetPopup = GetNode<SpaceFleetInfoPopup>(FleetPopupPath);
        constructionPopup = GetNode<SpaceConstructionPopup>(ConstructionPopupPath);
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

    public void ShowConstructionOptionsForFleet(SpaceFleet fleet)
    {
        fleetToConstructWith = fleet;

        // TODO: maybe this will need to fleet if some structures would have special requirements for building them
        constructionPopup.OpenWithStructures(stage!.CurrentGame!.TechWeb.GetAvailableSpaceStructures(), this,
            stage.SocietyResources);
    }

    public void OnStructureTypeSelected(SpaceStructureDefinition structureDefinition)
    {
        if (fleetToConstructWith == null)
        {
            GD.PrintErr("No fleet to construct with set");
            return;
        }

        // TODO: forward to the stage
        throw new NotImplementedException();
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
                ConstructionPopupPath.Dispose();
            }
        }

        base.Dispose(disposing);
    }
}
