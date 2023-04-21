using System.Collections.Generic;
using Godot;
using Newtonsoft.Json;

/// <summary>
///   The main class handling the society stage functions
/// </summary>
public class SocietyStage : StrategyStageBase, ISocietyStructureDataAccess, IStructureSelectionReceiver
{
    [Export]
    public NodePath? SelectBuildingPopupPath;

    private readonly Dictionary<object, float> activeResearchContributions = new();

#pragma warning disable CA2213
    private SelectBuildingPopup selectBuildingPopup = null!;

    private PackedScene structureScene = null!;

    private Spatial? buildingToPlaceGhost;
#pragma warning restore CA2213

    private WorldResource foodResource = null!;

    [JsonProperty]
    [AssignOnlyChildItemsOnDeserialize]
    private SocietyStructureSystem structureSystem = null!;

    [JsonProperty]
    [AssignOnlyChildItemsOnDeserialize]
    private CitizenMovingSystem citizenMovingSystem = null!;

    private long population = 1;

    [JsonProperty]
    private SocietyResourceStorage resourceStorage = new();

    private StructureDefinition? buildingTypeToPlace;

    [JsonProperty]
    [AssignOnlyChildItemsOnDeserialize]
    public SocietyHUD HUD { get; private set; } = null!;

    [JsonIgnore]
    public IResourceContainer SocietyResources => resourceStorage;

    [JsonProperty]
    public TechnologyProgress? CurrentlyResearchedTechnology { get; private set; }

    [JsonIgnore]
    protected override IStrategyStageHUD BaseHUD => HUD;

    public override void _Ready()
    {
        base._Ready();

        ResolveNodeReferences();

        structureScene = SpawnHelpers.LoadStructureScene();

        HUD.Init(this);

        SetupStage();
    }

    public override void ResolveNodeReferences()
    {
        if (NodeReferencesResolved)
            return;

        base.ResolveNodeReferences();

        HUD = GetNode<SocietyHUD>("SocietyHUD");

        selectBuildingPopup = GetNode<SelectBuildingPopup>(SelectBuildingPopupPath);

        // Systems
        structureSystem = new SocietyStructureSystem(rootOfDynamicallySpawned);
        citizenMovingSystem = new CitizenMovingSystem(rootOfDynamicallySpawned);
    }

    public override void _Process(float delta)
    {
        base._Process(delta);

        if (!IsGameOver())
        {
            structureSystem.Process(delta, this);

            // This doesn't really need to update all that often but for now this is fine performance-wise and is
            // easier to program
            resourceStorage.Capacity = structureSystem.CachedTotalStorage;

            // TODO: make population consume food

            HUD.UpdateScienceSpeed(activeResearchContributions.SumValues());

            // Update the place to place the selected building
            if (buildingToPlaceGhost != null)
            {
                // TODO: collision check with other buildings
                buildingToPlaceGhost.GlobalTranslation = GetPlayerCursorPointedWorldPosition();
            }

            HandlePopulationGrowth();

            citizenMovingSystem.Process(delta, population);

            if (CurrentlyResearchedTechnology?.Completed == true)
            {
                GD.Print("Current technology research completed");
                CurrentGame!.TechWeb.UnlockTechnology(CurrentlyResearchedTechnology.Technology);
                CurrentlyResearchedTechnology = null;

                // TODO: if research screen is open, it should have its state update here in regards to the unlocked
                // technology
            }
        }

        HUD.UpdateResourceDisplay(resourceStorage);
        HUD.UpdatePopulationDisplay(population);
    }

    public override void StartMusic()
    {
        Jukebox.Instance.PlayCategory("SocietyStage");
    }

    public PlacedStructure AddBuilding(StructureDefinition structureDefinition, Transform location)
    {
        // TODO: Proper storing of created structures for easier processing
        return SpawnHelpers.SpawnStructure(structureDefinition, location, rootOfDynamicallySpawned, structureScene);
    }

    public override void StartNewGame()
    {
        CurrentGame = GameProperties.StartSocietyStageGame(new WorldGenerationSettings());

        // Spawn an initial society center to get the player started when directly going to this stage
        var societyCenter =
            AddBuilding(SimulationParameters.Instance.GetStructure("societyCenter"), Transform.Identity);
        societyCenter.ForceCompletion();

        base.StartNewGame();
    }

    public void PerformBuildOrOpenMenu()
    {
        if (buildingTypeToPlace != null)
        {
            PlaceGhostBuilding();
            return;
        }

        // TODO: the buildable structures will need to refresh every few seconds as the passive resource income makes
        // it likely the player can build something new while this is open
        selectBuildingPopup.OpenWithStructures(CurrentGame!.TechWeb.GetAvailableStructures(), this, resourceStorage);
    }

    [RunOnKeyDown("e_primary")]
    public bool PlaceGhostBuilding()
    {
        if (buildingTypeToPlace == null)
            return false;

        if (!PlaceCurrentStructureIfPossible())
        {
            // TODO: play an invalid placement sound
            GD.Print("Couldn't place selected building");
            return true;
        }

        return true;
    }

    [RunOnKeyDown("e_secondary", Priority = 1)]
    [RunOnKeyDown("e_cancel_current_action", Priority = 1)]
    public bool CancelBuildingPlaceIfInProgress()
    {
        if (buildingTypeToPlace == null)
            return false;

        buildingToPlaceGhost?.QueueFree();
        buildingToPlaceGhost = null;

        buildingTypeToPlace = null;
        return true;
    }

    public void OnStructureTypeSelected(StructureDefinition structureDefinition)
    {
        // This is canceled to free up any previous resources
        CancelBuildingPlaceIfInProgress();

        buildingTypeToPlace = structureDefinition;

        buildingToPlaceGhost = buildingTypeToPlace.GhostScene.Instance<Spatial>();

        rootOfDynamicallySpawned.AddChild(buildingToPlaceGhost);
    }

    public void ToggleResearchScreen()
    {
        HUD.OpenResearchScreen();
    }

    public void AddActiveResearchContribution(object researchSource, float researchPoints)
    {
        // TODO: come up with a way to get unique identifiers for the research sources
        // Using WeakReference doesn't work as it causes not equal objects to be created
        activeResearchContributions[researchSource] = researchPoints;
    }

    public void RemoveActiveResearchContribution(object researchSource)
    {
        activeResearchContributions.Remove(researchSource);
    }

    protected override void SetupStage()
    {
        base.SetupStage();

        foodResource = SimulationParameters.Instance.GetWorldResource("food");

        resourceStorage.Capacity = structureSystem.CalculateTotalStorage();

        citizenMovingSystem.Init(CurrentGame!.GameWorld.PlayerSpecies);

        // TODO: this is only unlocked here for now to prevent the player from accidentally wasting limited resources
        // in the previous prototype. Once that's no longer the case discovering this should be moved to the previous
        // stage. This is here rather than OnGameStarted to have this unlock appear to the player.
        CurrentGame.TechWeb.UnlockTechnology(SimulationParameters.Instance.GetTechnology("hunterGathering"));
    }

    protected override void OnGameStarted()
    {
        // Intentionally not translated prototype message
        HUD.HUDMessages.ShowMessage(
            "You are now in the Society Stage prototype. Build a few basic structures to gain resources, " +
            "then research factories and build one to advance to the next stage.",
            DisplayDuration.ExtraLong);
    }

    protected override bool IsGameOver()
    {
        // TODO: lose condition
        return false;
    }

    protected override void OnGameOver()
    {
        // TODO: once possible to lose, show in the GUI
    }

    protected override void OnLightLevelUpdate()
    {
    }

    protected override void AutoSave()
    {
        SaveHelper.ShowErrorAboutPrototypeSaving(this);
    }

    protected override void PerformQuickSave()
    {
        SaveHelper.ShowErrorAboutPrototypeSaving(this);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            SelectBuildingPopupPath?.Dispose();
        }

        base.Dispose(disposing);
    }

    private void HandlePopulationGrowth()
    {
        var housing = structureSystem.CachedHousingCapacity;

        // TODO: adjust food need and reproduction rate based on species properties
        float requiredForNewMember = 1;

        // Don't grow if not enough housing
        // And for now just for the prototype only grow when otherwise full on food
        if (population >= housing || resourceStorage.GetAvailableAmount(foodResource) < resourceStorage.Capacity)
            return;

        if (resourceStorage.Take(foodResource, requiredForNewMember) > 0)
        {
            // Took some food to grow
            ++population;
        }
    }

    private bool PlaceCurrentStructureIfPossible()
    {
        if (buildingTypeToPlace == null || buildingToPlaceGhost == null)
            return false;

        if (!buildingTypeToPlace.TakeResourcesToStartIfPossible(resourceStorage))
            return false;

        // TODO: free space check, could maybe set a flag in _Process that is then used here
        AddBuilding(buildingTypeToPlace, buildingToPlaceGhost.GlobalTransform);

        buildingToPlaceGhost?.QueueFree();
        buildingToPlaceGhost = null;

        buildingTypeToPlace = null;
        return true;
    }

    private void StartResearching(string technologyName)
    {
        // Skip if trying to start the same research again, just to not lose progress as the GUI data passing to
        // ensure a technology is not started multiple times is complicated
        if (CurrentlyResearchedTechnology?.Technology.InternalName == technologyName)
        {
            GD.Print("Skipping trying to start the same research again");
            return;
        }

        GD.Print("Starting researching: ", technologyName);
        CurrentlyResearchedTechnology =
            new TechnologyProgress(SimulationParameters.Instance.GetTechnology(technologyName));
    }
}
