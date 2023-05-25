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

    [Export]
    public NodePath StructurePopupPath = null!;

    [Export]
    public NodePath DescendButtonPath = null!;

#pragma warning disable CA2213
    private Label populationLabel = null!;

    private PlanetScreen planetScreenPopup = null!;
    private SpaceFleetInfoPopup fleetPopup = null!;
    private SpaceConstructionPopup constructionPopup = null!;

    private SpaceStructureInfoPopup structurePopup = null!;

    private Button descendButton = null!;
#pragma warning restore CA2213

    private SpaceFleet? fleetToConstructWith;

    private bool wasAscended;

    [Signal]
    public delegate void OnDescendPressed();

    // TODO: real button referencing text for this
    protected override string UnPauseHelpText => "TODO: unpause text for this stage";

    public override void _Ready()
    {
        base._Ready();

        populationLabel = GetNode<Label>(PopulationLabelPath);
        planetScreenPopup = GetNode<PlanetScreen>(PlanetScreenPopupPath);
        fleetPopup = GetNode<SpaceFleetInfoPopup>(FleetPopupPath);
        constructionPopup = GetNode<SpaceConstructionPopup>(ConstructionPopupPath);
        structurePopup = GetNode<SpaceStructureInfoPopup>(StructurePopupPath);

        descendButton = GetNode<Button>(DescendButtonPath);
    }

    public override void Init(SpaceStage containedInStage)
    {
        base.Init(containedInStage);

        UpdateButtonState();

        wasAscended = containedInStage.Ascended;

        // Setup multi level god tools signals, these are done this way as they would be pretty annoying to hook up
        // all over the place purely through Godot
        fleetPopup.Connect(nameof(SpaceFleetInfoPopup.OnOpenGodTools), containedInStage,
            nameof(StageBase.OpenGodToolsForEntity));

        planetScreenPopup.Connect(nameof(PlanetScreen.OnOpenGodTools), containedInStage,
            nameof(StageBase.OpenGodToolsForEntity));
    }

    public void OnAscended()
    {
        UpdateButtonState();

        if (!wasAscended)
        {
            wasAscended = true;

            // Close all windows to have them be reopened by the player to get the ascension stuff in them
            CloseAllOpenWindows();
        }
    }

    public void UpdatePopulationDisplay(long population)
    {
        populationLabel.Text = StringUtils.ThreeDigitFormat(population);
    }

    public void OpenPlanetScreen(PlacedPlanet planet)
    {
        planetScreenPopup.ShowForPlanet(planet, stage!.Ascended);
    }

    public void OpenFleetInfo(SpaceFleet fleet)
    {
        fleetPopup.ShowForUnit(fleet, stage!.Ascended);
    }

    public void CloseFleetInfo()
    {
        fleetPopup.Close();
    }

    public SpaceFleet? GetSelectedFleet()
    {
        return fleetPopup.OpenedForUnit;
    }

    public void OpenStructureInfo(PlacedSpaceStructure structure)
    {
        structurePopup.ShowForStructure(structure);
    }

    /// <summary>
    ///   Closes all open windows, called when something really important is being shown on screen
    /// </summary>
    public void CloseAllOpenWindows()
    {
        planetScreenPopup.Close();
        fleetPopup.Close();
        constructionPopup.Close();
        structurePopup.Close();
        researchScreen.Close();
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

        stage!.StartPlacingStructure(fleetToConstructWith, structureDefinition);
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
                StructurePopupPath.Dispose();
                DescendButtonPath.Dispose();
            }
        }

        base.Dispose(disposing);
    }

    private void UpdateButtonState()
    {
        descendButton.Visible = stage?.CurrentGame?.Ascended == true;
    }

    private void ForwardDescendPress()
    {
        GUICommon.Instance.PlayButtonPressSound();

        EmitSignal(nameof(OnDescendPressed));
    }
}
