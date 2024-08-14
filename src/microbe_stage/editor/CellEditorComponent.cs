using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using AutoEvo;
using DefaultEcs;
using Godot;
using Newtonsoft.Json;
using Systems;

/// <summary>
///   The cell editor component combining the organelle and other editing logic with the GUI for it
/// </summary>
[SceneLoadedClass("res://src/microbe_stage/editor/CellEditorComponent.tscn")]
public partial class CellEditorComponent :
    HexEditorComponentBase<ICellEditorData, CombinedEditorAction, EditorAction, OrganelleTemplate, CellType>,
    ICellEditorComponent, IGodotEarlyNodeResolve
{
    [Export]
    public bool IsMulticellularEditor;

    [Export]
    public NodePath? TopPanelPath;

    [Export]
    public NodePath DayButtonPath = null!;

    [Export]
    public NodePath NightButtonPath = null!;

    [Export]
    public NodePath AverageLightButtonPath = null!;

    [Export]
    public NodePath CurrentLightButtonPath = null!;

    [Export]
    public NodePath TabButtonsPath = null!;

    [Export]
    public NodePath StructureTabButtonPath = null!;

    [Export]
    public NodePath AppearanceTabButtonPath = null!;

    [Export]
    public NodePath BehaviourTabButtonPath = null!;

    [Export]
    public NodePath StructureTabPath = null!;

    [Export]
    public NodePath AppearanceTabPath = null!;

    [Export]
    public NodePath BehaviourTabPath = null!;

    [Export]
    public NodePath PartsSelectionContainerPath = null!;

    [Export]
    public NodePath MembraneTypeSelectionPath = null!;

    [Export]
    public NodePath SizeLabelPath = null!;

    [Export]
    public NodePath OrganismStatisticsPath = null!;

    [Export]
    public NodePath SpeedLabelPath = null!;

    [Export]
    public NodePath RotationSpeedLabelPath = null!;

    [Export]
    public NodePath HpLabelPath = null!;

    [Export]
    public NodePath StorageLabelPath = null!;

    [Export]
    public NodePath DigestionSpeedLabelPath = null!;

    [Export]
    public NodePath DigestionEfficiencyLabelPath = null!;

    [Export]
    public NodePath GenerationLabelPath = null!;

    [Export]
    public NodePath AutoEvoPredictionPanelPath = null!;

    [Export]
    public NodePath TotalEnergyLabelPath = null!;

    [Export]
    public NodePath AutoEvoPredictionFailedLabelPath = null!;

    [Export]
    public NodePath WorstPatchLabelPath = null!;

    [Export]
    public NodePath BestPatchLabelPath = null!;

    [Export]
    public NodePath MembraneColorPickerPath = null!;

    [Export]
    public NodePath ATPBalancePanelPath = null!;

    [Export]
    public NodePath ATPProductionLabelPath = null!;

    [Export]
    public NodePath ATPConsumptionLabelPath = null!;

    [Export]
    public NodePath ATPProductionBarPath = null!;

    [Export]
    public NodePath ATPConsumptionBarPath = null!;

    [Export]
    public NodePath RigiditySliderPath = null!;

    [Export]
    public NodePath NegativeAtpPopupPath = null!;

    [Export]
    public NodePath OrganelleMenuPath = null!;

    [Export]
    public NodePath AutoEvoPredictionExplanationPopupPath = null!;

    [Export]
    public NodePath AutoEvoPredictionExplanationLabelPath = null!;

    [Export]
    public NodePath OrganelleUpgradeGUIPath = null!;

    [Export]
    public NodePath RightPanelScrollContainerPath = null!;

#pragma warning disable CA2213
    [Export]
    public LabelSettings ATPBalanceNormalText = null!;

    [Export]
    public LabelSettings ATPBalanceNotEnoughText = null!;
#pragma warning restore CA2213

    /// <summary>
    ///   Temporary hex memory for use by the main thread in this component
    /// </summary>
    private readonly List<Hex> hexTemporaryMemory = new();

    private readonly List<Hex> hexTemporaryMemory2 = new();

    private readonly List<Hex> islandResults = new();
    private readonly HashSet<Hex> islandsWorkMemory1 = new();
    private readonly List<Hex> islandsWorkMemory2 = new();
    private readonly Queue<Hex> islandsWorkMemory3 = new();

    private readonly Dictionary<Compound, float> processSpeedWorkMemory = new();

    private readonly List<ShaderMaterial> temporaryDisplayerFetchList = new();

    private readonly List<EditorUserOverride> ignoredEditorWarnings = new();

    private readonly Compound atp = SimulationParameters.Instance.GetCompound("atp");

#pragma warning disable CA2213

    // Light level controls
    private Control topPanel = null!;
    private Button dayButton = null!;
    private Button nightButton = null!;
    private Button averageLightButton = null!;
    private Button currentLightButton = null!;

    // Selection menu tab selector buttons
    private Button structureTabButton = null!;
    private Button appearanceTabButton = null!;
    private Button behaviourTabButton = null!;

    private PanelContainer structureTab = null!;
    private PanelContainer appearanceTab = null!;

    [JsonProperty]
    [AssignOnlyChildItemsOnDeserialize]
    private BehaviourEditorSubComponent behaviourEditor = null!;

    private VBoxContainer partsSelectionContainer = null!;
    private CollapsibleList membraneTypeSelection = null!;

    [JsonProperty]
    [AssignOnlyChildItemsOnDeserialize]
    private CellStatsIndicator sizeLabel = null!;

    [JsonProperty]
    [AssignOnlyChildItemsOnDeserialize]
    private CellStatsIndicator speedLabel = null!;

    [JsonProperty]
    [AssignOnlyChildItemsOnDeserialize]
    private CellStatsIndicator rotationSpeedLabel = null!;

    [JsonProperty]
    [AssignOnlyChildItemsOnDeserialize]
    private CellStatsIndicator hpLabel = null!;

    [JsonProperty]
    [AssignOnlyChildItemsOnDeserialize]
    private CellStatsIndicator storageLabel = null!;

    [JsonProperty]
    [AssignOnlyChildItemsOnDeserialize]
    private CellStatsIndicator digestionSpeedLabel = null!;

    [JsonProperty]
    [AssignOnlyChildItemsOnDeserialize]
    private CellStatsIndicator digestionEfficiencyLabel = null!;

    private Label generationLabel = null!;

    private CellStatsIndicator totalEnergyLabel = null!;
    private Label autoEvoPredictionFailedLabel = null!;
    private Label bestPatchLabel = null!;
    private Label worstPatchLabel = null!;

    private Control autoEvoPredictionPanel = null!;

    private Slider rigiditySlider = null!;
    private TweakedColourPicker membraneColorPicker = null!;

    private Control atpBalancePanel = null!;

    [Export]
    private Label atpBalanceLabel = null!;

    private Label atpProductionLabel = null!;
    private Label atpConsumptionLabel = null!;
    private SegmentedBar atpProductionBar = null!;
    private SegmentedBar atpConsumptionBar = null!;

    private CustomConfirmationDialog negativeAtpPopup = null!;

    [Export]
    private CustomConfirmationDialog pendingEndosymbiosisPopup = null!;

    [Export]
    private Button endosymbiosisButton = null!;

    [Export]
    private EndosymbiosisPopup endosymbiosisPopup = null!;

    private OrganellePopupMenu organelleMenu = null!;
    private OrganelleUpgradeGUI organelleUpgradeGUI = null!;

    [Export]
    private CheckBox calculateBalancesAsIfDay = null!;

    [Export]
    private CheckBox calculateBalancesWhenMoving = null!;

    [Export]
    private CompoundBalanceDisplay compoundBalance = null!;

    [Export]
    private CompoundStorageStatistics compoundStorageLastingTimes = null!;

    [Export]
    private CustomRichTextLabel notEnoughStorageWarning = null!;

    [Export]
    private Button processListButton = null!;

    [Export]
    private ProcessList processList = null!;

    [Export]
    private CustomWindow processListWindow = null!;

    private CustomWindow autoEvoPredictionExplanationPopup = null!;
    private CustomRichTextLabel autoEvoPredictionExplanationLabel = null!;

    private ScrollContainer rightPanelScrollContainer = null!;

    private PackedScene organelleSelectionButtonScene = null!;

    private PackedScene undiscoveredOrganellesScene = null!;

    private PackedScene undiscoveredOrganellesTooltipScene = null!;

    private Node3D? cellPreviewVisualsRoot;
#pragma warning restore CA2213

    private OrganelleDefinition nucleus = null!;
    private OrganelleDefinition bindingAgent = null!;

    private Compound sunlight = null!;
    private OrganelleDefinition cytoplasm = null!;

    private EnergyBalanceInfo? energyBalanceInfo;

    private string? bestPatchName;

    // This and worstPatchPopulation used to be displayed but are now kept for potential future use
    private long bestPatchPopulation;

    private float bestPatchEnergyGathered;

    private string? worstPatchName;

    private long worstPatchPopulation;

    private float worstPatchEnergyGathered;

    private Dictionary<OrganelleDefinition, MicrobePartSelection> placeablePartSelectionElements = new();

    private Dictionary<OrganelleDefinition, MicrobePartSelection> allPartSelectionElements = new();

    private Dictionary<MembraneType, MicrobePartSelection> membraneSelectionElements = new();

    [JsonProperty]
    private SelectionMenuTab selectedSelectionMenuTab = SelectionMenuTab.Structure;

    [JsonProperty]
    private LightLevelOption selectedLightLevelOption = LightLevelOption.Current;

    private bool? autoEvoPredictionRunSuccessful;
    private PendingAutoEvoPrediction? waitingForPrediction;
    private LocalizedStringBuilder? predictionDetailsText;

    /// <summary>
    ///   The new to set on the species (or cell type) after exiting (if null, no change)
    /// </summary>
    /// <remarks>
    ///   <para>
    ///     This is now nullable to make loading older saves with the new editor data structures easier
    ///   </para>
    /// </remarks>
    [JsonProperty]
    private string newName = "unset";

    /// <summary>
    ///   We're taking advantage of the available membrane and organelle system already present in the microbe stage
    ///   for the membrane preview.
    /// </summary>
    private MicrobeVisualOnlySimulation? previewSimulation;

    private MicrobeSpecies? previewMicrobeSpecies;
    private Entity previewMicrobe;

    [JsonProperty]
    private Color colour;

    [JsonProperty]
    private float rigidity;

    /// <summary>
    ///   To not have to recreate this object for each place / remove this is a cached clone of editedSpecies to which
    ///   current editor changes are applied for simulating what effect they would have on the population.
    /// </summary>
    private MicrobeSpecies? cachedAutoEvoPredictionSpecies;

    /// <summary>
    ///   This is the container that has the edited organelles in
    ///   it. This is populated when entering and used to update the
    ///   player's species template on exit.
    /// </summary>
    [JsonProperty]
    private OrganelleLayout<OrganelleTemplate> editedMicrobeOrganelles = null!;

    /// <summary>
    ///   When this is true, on next process this will handle added and removed organelles and update stats etc.
    ///   This is done to make adding a bunch of organelles at once more efficient.
    /// </summary>
    private bool organelleDataDirty = true;

    /// <summary>
    ///   Similar to organelleDataDirty but with the exception that this is only set false when the editor
    ///   membrane mesh has been redone. Used so the membrane doesn't have to be rebuild everytime when
    ///   switching back and forth between structure and membrane tab (without editing organelle placements).
    /// </summary>
    private bool microbeVisualizationOrganellePositionsAreDirty = true;

    private bool microbePreviewMode;

    public enum SelectionMenuTab
    {
        Structure,
        Membrane,
        Behaviour,
    }

    public enum LightLevelOption
    {
        Day,
        Night,
        Average,
        Current,
    }

    /// <summary>
    ///   The selected membrane rigidity
    /// </summary>
    [JsonIgnore]
    public float Rigidity
    {
        get => rigidity;
        set
        {
            rigidity = value;

            if (previewMicrobeSpecies == null)
                return;

            previewMicrobeSpecies.MembraneRigidity = value;

            if (previewMicrobe.IsAlive)
                previewSimulation!.ApplyMicrobeRigidity(previewMicrobe, previewMicrobeSpecies.MembraneRigidity);
        }
    }

    /// <summary>
    ///   Selected membrane type for the species
    /// </summary>
    [JsonProperty]
    public MembraneType Membrane { get; private set; } = null!;

    /// <summary>
    ///   Current selected colour for the species.
    /// </summary>
    [JsonIgnore]
    public Color Colour
    {
        get => colour;
        set
        {
            colour = value;

            if (previewMicrobeSpecies == null)
                return;

            previewMicrobeSpecies.Colour = value;

            if (previewMicrobe.IsAlive)
                previewSimulation!.ApplyMicrobeColour(previewMicrobe, previewMicrobeSpecies.Colour);
        }
    }

    /// <summary>
    ///   The name of organelle type that is selected to be placed
    /// </summary>
    [JsonIgnore]
    public string? ActiveActionName
    {
        get => activeActionName;
        set
        {
            if (value != activeActionName)
            {
                TutorialState?.SendEvent(TutorialEventType.MicrobeEditorOrganelleToPlaceChanged,
                    new StringEventArgs(value), this);
            }

            activeActionName = value;
        }
    }

    [JsonIgnore]
    public override bool CanCancelAction => base.CanCancelAction || PendingEndosymbiontPlace != null;

    [JsonProperty]
    public EndosymbiontPlaceActionData? PendingEndosymbiontPlace { get; protected set; }

    /// <summary>
    ///   If this is enabled the editor will show how the edited cell would look like in the environment with
    ///   parameters set in the editor. Editing hexes is disabled during this (except undo / redo).
    /// </summary>
    public bool MicrobePreviewMode
    {
        get => microbePreviewMode;
        set
        {
            microbePreviewMode = value;

            if (cellPreviewVisualsRoot != null)
                cellPreviewVisualsRoot.Visible = value;

            // Need to reapply the species as changes to it are ignored when the appearance tab is not shown
            UpdateCellVisualization();

            foreach (var hex in placedHexes)
                hex.Visible = !MicrobePreviewMode;

            foreach (var model in placedModels)
                model.Visible = !MicrobePreviewMode;
        }
    }

    [JsonIgnore]
    public bool HasNucleus => PlacedUniqueOrganelles.Any(d => d == nucleus);

    [JsonIgnore]
    public override bool HasIslands =>
        editedMicrobeOrganelles.GetIslandHexes(islandResults, islandsWorkMemory1, islandsWorkMemory2,
            islandsWorkMemory3) > 0;

    /// <summary>
    ///   Number of organelles in the microbe
    /// </summary>
    [JsonIgnore]
    public int MicrobeSize => editedMicrobeOrganelles.Organelles.Count;

    /// <summary>
    ///   Number of hexes in the microbe
    /// </summary>
    [JsonIgnore]
    public int MicrobeHexSize
    {
        get
        {
            int result = 0;

            foreach (var organelle in editedMicrobeOrganelles.Organelles)
            {
                result += organelle.Definition.HexCount;
            }

            return result;
        }
    }

    [JsonIgnore]
    public TutorialState? TutorialState { get; set; }

    /// <summary>
    ///   Needed for auto-evo prediction to be able to compare the new energy to the old energy
    /// </summary>
    [JsonProperty]
    public float? PreviousPlayerGatheredEnergy { get; set; }

    [JsonIgnore]
    public IEnumerable<OrganelleDefinition> PlacedUniqueOrganelles => editedMicrobeOrganelles
        .Where(p => p.Definition.Unique)
        .Select(p => p.Definition);

    [JsonIgnore]
    public override bool ShowFinishButtonWarning
    {
        get
        {
            if (base.ShowFinishButtonWarning)
                return true;

            if (IsNegativeAtpProduction())
                return true;

            if (HasIslands)
                return true;

            if (HasFinishedPendingEndosymbiosis)
                return true;

            return false;
        }
    }

    /// <summary>
    ///   True when there are pending endosymbiosis actions. Only works after editor is fully initialized.
    /// </summary>
    [JsonIgnore]
    public bool HasFinishedPendingEndosymbiosis =>
        Editor.EditorReady && Editor.EditedBaseSpecies.Endosymbiosis.HasCompleteEndosymbiosis();

    [JsonIgnore]
    public bool NodeReferencesResolved { get; private set; }

    protected override bool ForceHideHover => MicrobePreviewMode;

    private float CostMultiplier =>
        (IsMulticellularEditor ? Constants.MULTICELLULAR_EDITOR_COST_FACTOR : 1.0f) *
        Editor.CurrentGame.GameWorld.WorldSettings.MPMultiplier;

    public static void UpdateOrganelleDisplayerTransform(SceneDisplayer organelleModel, OrganelleTemplate organelle)
    {
        organelleModel.Transform = new Transform3D(
            new Basis(MathUtils.CreateRotationForOrganelle(1 * organelle.Orientation)),
            organelle.OrganelleModelPosition);

        organelleModel.Scale = new Vector3(Constants.DEFAULT_HEX_SIZE, Constants.DEFAULT_HEX_SIZE,
            Constants.DEFAULT_HEX_SIZE);
    }

    /// <summary>
    ///   Updates the organelle model displayer to have the specified scene in it
    /// </summary>
    public static void UpdateOrganellePlaceHolderScene(SceneDisplayer organelleModel,
        LoadedSceneWithModelInfo displayScene, int renderPriority, List<ShaderMaterial> temporaryDataHolder)
    {
        organelleModel.Scene = displayScene.LoadedScene;

        temporaryDataHolder.Clear();
        if (!organelleModel.GetMaterial(temporaryDataHolder, displayScene.ModelPath))
        {
            GD.PrintErr("Failed to get material for editor / display cell to update render priority");
            return;
        }

        // To follow MicrobeRenderPrioritySystem this sets other than the first material to be -1 in priority
        bool first = true;

        foreach (var shaderMaterial in temporaryDataHolder)
        {
            if (first)
            {
                shaderMaterial.RenderPriority = renderPriority;
                first = false;
            }
            else
            {
                shaderMaterial.RenderPriority = renderPriority - 1;
            }
        }
    }

    public override void _Ready()
    {
        base._Ready();

        ResolveNodeReferences();

        // This works only after this is attached to the scene tree
        var tabButtons = GetNode<TabButtons>(TabButtonsPath);
        structureTabButton = GetNode<Button>(tabButtons.GetAdjustedButtonPath(TabButtonsPath, StructureTabButtonPath));
        appearanceTabButton =
            GetNode<Button>(tabButtons.GetAdjustedButtonPath(TabButtonsPath, AppearanceTabButtonPath));
        behaviourTabButton = GetNode<Button>(tabButtons.GetAdjustedButtonPath(TabButtonsPath, BehaviourTabButtonPath));

        // Hidden in the Godot editor to make selecting other things easier
        organelleUpgradeGUI.Visible = true;

        atpProductionBar.SelectedType = SegmentedBar.Type.ATP;
        atpProductionBar.IsProduction = true;
        atpConsumptionBar.SelectedType = SegmentedBar.Type.ATP;

        nucleus = SimulationParameters.Instance.GetOrganelleType("nucleus");
        bindingAgent = SimulationParameters.Instance.GetOrganelleType("bindingAgent");

        organelleSelectionButtonScene =
            GD.Load<PackedScene>("res://src/microbe_stage/editor/MicrobePartSelection.tscn");

        undiscoveredOrganellesScene =
            GD.Load<PackedScene>("res://src/microbe_stage/organelle_unlocks/UndiscoveredOrganellesButton.tscn");
        undiscoveredOrganellesTooltipScene =
            GD.Load<PackedScene>("res://src/microbe_stage/organelle_unlocks/UndiscoveredOrganellesTooltip.tscn");

        sunlight = SimulationParameters.Instance.GetCompound("sunlight");
        cytoplasm = SimulationParameters.Instance.GetOrganelleType("cytoplasm");

        SetupMicrobePartSelections();

        ApplySelectionMenuTab();
        RegisterTooltips();
    }

    public override void ResolveNodeReferences()
    {
        if (NodeReferencesResolved)
            return;

        base.ResolveNodeReferences();

        NodeReferencesResolved = true;

        topPanel = GetNode<Control>(TopPanelPath);
        dayButton = GetNode<Button>(DayButtonPath);
        nightButton = GetNode<Button>(NightButtonPath);
        averageLightButton = GetNode<Button>(AverageLightButtonPath);
        currentLightButton = GetNode<Button>(CurrentLightButtonPath);

        structureTab = GetNode<PanelContainer>(StructureTabPath);

        appearanceTab = GetNode<PanelContainer>(AppearanceTabPath);

        behaviourEditor = GetNode<BehaviourEditorSubComponent>(BehaviourTabPath);

        partsSelectionContainer = GetNode<VBoxContainer>(PartsSelectionContainerPath);
        membraneTypeSelection = GetNode<CollapsibleList>(MembraneTypeSelectionPath);

        sizeLabel = GetNode<CellStatsIndicator>(SizeLabelPath);
        speedLabel = GetNode<CellStatsIndicator>(SpeedLabelPath);
        rotationSpeedLabel = GetNode<CellStatsIndicator>(RotationSpeedLabelPath);
        hpLabel = GetNode<CellStatsIndicator>(HpLabelPath);
        storageLabel = GetNode<CellStatsIndicator>(StorageLabelPath);
        digestionSpeedLabel = GetNode<CellStatsIndicator>(DigestionSpeedLabelPath);
        digestionEfficiencyLabel = GetNode<CellStatsIndicator>(DigestionEfficiencyLabelPath);
        generationLabel = GetNode<Label>(GenerationLabelPath);
        totalEnergyLabel = GetNode<CellStatsIndicator>(TotalEnergyLabelPath);
        autoEvoPredictionFailedLabel = GetNode<Label>(AutoEvoPredictionFailedLabelPath);
        worstPatchLabel = GetNode<Label>(WorstPatchLabelPath);
        bestPatchLabel = GetNode<Label>(BestPatchLabelPath);

        autoEvoPredictionPanel = GetNode<Control>(AutoEvoPredictionPanelPath);

        rigiditySlider = GetNode<Slider>(RigiditySliderPath);
        membraneColorPicker = GetNode<TweakedColourPicker>(MembraneColorPickerPath);

        atpBalancePanel = GetNode<Control>(ATPBalancePanelPath);
        atpProductionLabel = GetNode<Label>(ATPProductionLabelPath);
        atpConsumptionLabel = GetNode<Label>(ATPConsumptionLabelPath);
        atpProductionBar = GetNode<SegmentedBar>(ATPProductionBarPath);
        atpConsumptionBar = GetNode<SegmentedBar>(ATPConsumptionBarPath);

        negativeAtpPopup = GetNode<CustomConfirmationDialog>(NegativeAtpPopupPath);
        organelleMenu = GetNode<OrganellePopupMenu>(OrganelleMenuPath);
        organelleUpgradeGUI = GetNode<OrganelleUpgradeGUI>(OrganelleUpgradeGUIPath);

        rightPanelScrollContainer = GetNode<ScrollContainer>(RightPanelScrollContainerPath);

        autoEvoPredictionExplanationPopup = GetNode<CustomWindow>(AutoEvoPredictionExplanationPopupPath);
        autoEvoPredictionExplanationLabel = GetNode<CustomRichTextLabel>(AutoEvoPredictionExplanationLabelPath);
    }

    public override void Init(ICellEditorData owningEditor, bool fresh)
    {
        base.Init(owningEditor, fresh);

        if (!IsMulticellularEditor)
        {
            behaviourEditor.Init(owningEditor, fresh);
        }
        else
        {
            // Endosymbiosis is not managed through this component in multicellular
            endosymbiosisButton.Visible = false;
        }

        // Visual simulation is needed very early when loading a save
        previewSimulation = new MicrobeVisualOnlySimulation();

        cellPreviewVisualsRoot = new Node3D
        {
            Name = "CellPreviewVisuals",
        };

        Editor.RootOfDynamicallySpawned.AddChild(cellPreviewVisualsRoot);

        previewSimulation.Init(cellPreviewVisualsRoot);

        var newLayout = new OrganelleLayout<OrganelleTemplate>(OnOrganelleAdded, OnOrganelleRemoved);

        if (fresh)
        {
            editedMicrobeOrganelles = newLayout;
        }
        else
        {
            // We assume that the loaded save layout did not have anything weird set for the callbacks as we
            // do this rather than use SaveApplyHelpers
            foreach (var editedMicrobeOrganelle in editedMicrobeOrganelles)
            {
                newLayout.AddFast(editedMicrobeOrganelle, hexTemporaryMemory, hexTemporaryMemory2);
            }

            editedMicrobeOrganelles = newLayout;

            if (Editor.EditedCellProperties != null)
            {
                UpdateGUIAfterLoadingSpecies(Editor.EditedBaseSpecies);
                CreateUndiscoveredOrganellesButtons();
                CreatePreviewMicrobeIfNeeded();
                UpdateArrow(false);
            }
            else
            {
                GD.Print("Loaded cell editor with no cell to edit set");
            }
        }

        // Send info to the GUI about the organelle effectiveness in the current patch
        CalculateOrganelleEffectivenessInCurrentPatch();

        if (IsMulticellularEditor)
        {
            componentBottomLeftButtons.HandleRandomSpeciesName = false;
            componentBottomLeftButtons.UseSpeciesNameValidation = false;

            // TODO: implement random cell type name generator
            componentBottomLeftButtons.ShowRandomizeButton = false;

            componentBottomLeftButtons.SetNamePlaceholder(Localization.Translate("CELL_TYPE_NAME"));

            autoEvoPredictionPanel.Visible = false;

            // In multicellular the body plan editor handles this
            behaviourTabButton.Visible = false;
            behaviourEditor.Visible = false;
        }

        UpdateMicrobePartSelections();

        // After the "if multicellular check" so the tooltip cost factors are correct
        // on changing editor types, as tooltip manager is persistent while the game is running
        UpdateMPCost();

        // Do this here as we know the editor and hence world settings have been initialised by now
        UpdateOrganelleLAWKSettings();

        UpdateLightSelectionPanelVisibility();

        ApplySymmetryForCurrentOrganelle();
    }

    public override void _Process(double delta)
    {
        if (cellPreviewVisualsRoot == null)
            throw new InvalidOperationException("This editor component is not initialized");

        base._Process(delta);

        if (!Visible)
            return;

        var debugOverlay = DebugOverlays.Instance;

        if (debugOverlay.PerformanceMetricsVisible)
        {
            var roughCount = Editor.RootOfDynamicallySpawned.GetChildCount();
            debugOverlay.ReportEntities(roughCount);
        }

        CheckRunningAutoEvoPrediction();

        if (organelleDataDirty)
        {
            OnOrganellesChanged();
            organelleDataDirty = false;
        }

        // Process microbe visuals preview when it is visible
        if (cellPreviewVisualsRoot.Visible)
        {
            // Init being called is checked at the start of this method
            previewSimulation!.ProcessAll((float)delta);
        }

        // Show the organelle that is about to be placed
        if (Editor.ShowHover && !MicrobePreviewMode)
        {
            GetMouseHex(out int q, out int r);

            OrganelleDefinition? shownOrganelle = null;

            var effectiveSymmetry = Symmetry;

            if (!CanCancelAction && ActiveActionName != null)
            {
                // Can place stuff at all?
                isPlacementProbablyValid =
                    IsValidPlacement(new OrganelleTemplate(GetOrganelleDefinition(ActiveActionName), new Hex(q, r),
                        placementRotation), true);

                shownOrganelle = SimulationParameters.Instance.GetOrganelleType(ActiveActionName);
            }
            else if (MovingPlacedHex != null)
            {
                isPlacementProbablyValid = IsMoveTargetValid(new Hex(q, r), placementRotation, MovingPlacedHex);
                shownOrganelle = MovingPlacedHex.Definition;

                if (!Settings.Instance.MoveOrganellesWithSymmetry)
                    effectiveSymmetry = HexEditorSymmetry.None;
            }
            else if (PendingEndosymbiontPlace != null)
            {
                shownOrganelle = PendingEndosymbiontPlace.PlacedOrganelle.Definition;
                isPlacementProbablyValid =
                    IsValidPlacement(new OrganelleTemplate(shownOrganelle, new Hex(q, r), placementRotation), false);

                effectiveSymmetry = HexEditorSymmetry.None;
            }

            if (shownOrganelle != null)
            {
                HashSet<(Hex Hex, int Orientation)> hoveredHexes = new();

                if (!componentBottomLeftButtons.SymmetryEnabled)
                    effectiveSymmetry = HexEditorSymmetry.None;

                RunWithSymmetry(q, r,
                    (finalQ, finalR, rotation) =>
                    {
                        RenderHighlightedOrganelle(finalQ, finalR, rotation, shownOrganelle, MovingPlacedHex?.Upgrades);
                        hoveredHexes.Add((new Hex(finalQ, finalR), rotation));
                    }, effectiveSymmetry);

                MouseHoverPositions = hoveredHexes.ToList();
            }
        }
    }

    [RunOnKeyDown("e_primary")]
    public override bool PerformPrimaryAction()
    {
        if (Visible && PendingEndosymbiontPlace != null)
        {
            GetMouseHex(out var q, out var r);
            return PerformEndosymbiosisPlace(q, r);
        }

        return base.PerformPrimaryAction();
    }

    [RunOnKeyDown("e_cancel_current_action", Priority = 1)]
    public override bool CancelCurrentAction()
    {
        if (Visible && PendingEndosymbiontPlace != null)
        {
            OnCurrentActionCanceled();
            return true;
        }

        return base.CancelCurrentAction();
    }

    public override void OnEditorSpeciesSetup(Species species)
    {
        base.OnEditorSpeciesSetup(species);

        // For multicellular the cell editor is initialized before a cell type to edit is selected so we skip
        // the logic here the first time this is called too early
        var properties = Editor.EditedCellProperties;
        if (properties == null && IsMulticellularEditor)
            return;

        if (IsMulticellularEditor)
        {
            // Prepare for second use in multicellular editor
            editedMicrobeOrganelles.Clear();
        }
        else if (editedMicrobeOrganelles.Count > 0)
        {
            GD.PrintErr("Reusing cell editor without marking it for multicellular is not meant to be done");
        }

        // We set these here to make sure these are ready in the organelle add callbacks (even though currently
        // that just marks things dirty, and we update our stats on the next _Process call)
        Membrane = properties!.MembraneType;
        Rigidity = properties.MembraneRigidity;
        Colour = properties.Colour;

        if (!IsMulticellularEditor)
            behaviourEditor.OnEditorSpeciesSetup(species);

        // Get the species organelles to be edited. This also updates the placeholder hexes
        foreach (var organelle in properties.Organelles.Organelles)
        {
            editedMicrobeOrganelles.AddFast((OrganelleTemplate)organelle.Clone(), hexTemporaryMemory,
                hexTemporaryMemory2);
        }

        newName = properties.FormattedName;

        // This needs to be calculated here, otherwise ATP related unlock conditions would
        // get null as the ATP balance
        CalculateEnergyAndCompoundBalance(properties.Organelles.Organelles, properties.MembraneType,
            Editor.CurrentPatch.Biome);

        UpdateOrganelleUnlockTooltips(true);

        UpdateGUIAfterLoadingSpecies(Editor.EditedBaseSpecies);

        // Set up the display cell
        CreatePreviewMicrobeIfNeeded();

        UpdateArrow(false);
    }

    public override void OnFinishEditing()
    {
        OnFinishEditing(true);
    }

    public void OnFinishEditing(bool shouldUpdatePosition)
    {
        var editedSpecies = Editor.EditedBaseSpecies;
        var editedProperties = Editor.EditedCellProperties;

        if (editedProperties == null)
        {
            GD.Print("Cell editor skip applying changes as no target cell properties set");
            return;
        }

        // Apply changes to the species organelles
        // It is easiest to just replace all
        editedProperties.Organelles.Clear();

        foreach (var organelle in editedMicrobeOrganelles.Organelles)
        {
            var organelleToAdd = (OrganelleTemplate)organelle.Clone();
            editedProperties.Organelles.AddFast(organelleToAdd, hexTemporaryMemory, hexTemporaryMemory2);
        }

        if (shouldUpdatePosition)
            editedProperties.RepositionToOrigin();

        // Update bacteria status
        editedProperties.IsBacteria = !HasNucleus;

        editedProperties.UpdateNameIfValid(newName);

        // Update membrane
        editedProperties.MembraneType = Membrane;
        editedProperties.Colour = Colour;
        editedProperties.MembraneRigidity = Rigidity;

        if (!IsMulticellularEditor)
        {
            GD.Print("MicrobeEditor: updated organelles for species: ", editedSpecies.FormattedName);

            behaviourEditor.OnFinishEditing();

            // When this is the primary editor of the species data, this must refresh the species data properties that
            // depend on being edited
            editedSpecies.OnEdited();
        }
        else
        {
            GD.Print("MicrobeEditor: updated organelles for cell: ", editedProperties.FormattedName);
        }
    }

    public override void SetEditorWorldTabSpecificObjectVisibility(bool shown)
    {
        if (cellPreviewVisualsRoot == null)
            throw new InvalidOperationException("This component is not initialized yet");

        base.SetEditorWorldTabSpecificObjectVisibility(shown && !MicrobePreviewMode);

        cellPreviewVisualsRoot.Visible = shown && MicrobePreviewMode;
    }

    public override bool CanFinishEditing(IEnumerable<EditorUserOverride> userOverrides)
    {
        var editorUserOverrides = userOverrides.ToList();
        if (!base.CanFinishEditing(editorUserOverrides))
            return false;

        // Show warning if the editor has an endosymbiosis that should be finished
        if (HasFinishedPendingEndosymbiosis && !editorUserOverrides.Contains(EditorUserOverride.EndosymbiosisPending))
        {
            pendingEndosymbiosisPopup.PopupCenteredShrink();
            return false;
        }

        // Show warning popup if trying to exit with negative atp production
        // Not shown in multicellular as the popup would happen in kind of weird place
        if (!IsMulticellularEditor && IsNegativeAtpProduction() &&
            !editorUserOverrides.Contains(EditorUserOverride.NotProducingEnoughATP))
        {
            negativeAtpPopup.PopupCenteredShrink();
            return false;
        }

        // This is triggered when no changes have been made. A more accurate way would be to check the action history
        // for any undoable action, but that isn't accessible here currently so this is probably good enough.
        if (Editor.MutationPoints == Constants.BASE_MUTATION_POINTS)
        {
            var tutorialState = Editor.CurrentGame.TutorialState;

            // In the multicellular editor the cell editor might not be visible so preventing exiting the editor
            // without explanation is not a good idea so that's why this check is here
            if (tutorialState.Enabled && !IsMulticellularEditor)
            {
                tutorialState.SendEvent(TutorialEventType.MicrobeEditorNoChangesMade, EventArgs.Empty, this);

                if (tutorialState.TutorialActive())
                    return false;
            }
        }

        return true;
    }

    /// <summary>
    ///   Report that the current patch used in the editor has changed
    /// </summary>
    /// <param name="patch">The patch that is set</param>
    public void OnCurrentPatchUpdated(Patch patch)
    {
        _ = patch;

        ApplyLightLevelOption();
        CalculateOrganelleEffectivenessInCurrentPatch();
        UpdatePatchDependentBalanceData();
    }

    public void UpdatePatchDependentBalanceData()
    {
        // Skip if opened in the multicellular editor
        if (IsMulticellularEditor && editedMicrobeOrganelles.Organelles.Count < 1)
            return;

        UpdateLightSelectionPanelVisibility();

        // Calculate and send energy balance and compound balance to the GUI
        CalculateEnergyAndCompoundBalance(editedMicrobeOrganelles.Organelles, Membrane);

        UpdateOrganelleUnlockTooltips(false);
    }

    /// <summary>
    ///   Calculates the effectiveness of organelles in the current patch (actually the editor biome conditions which
    ///   may have additional modifiers applied)
    /// </summary>
    public void CalculateOrganelleEffectivenessInCurrentPatch()
    {
        var organelles = SimulationParameters.Instance.GetAllOrganelles();

        var result =
            ProcessSystem.ComputeOrganelleProcessEfficiencies(organelles, Editor.CurrentPatch.Biome,
                CompoundAmountType.Current);

        UpdateOrganelleEfficiencies(result);
    }

    /// <summary>
    ///   Wipes clean the current cell.
    /// </summary>
    public void CreateNewMicrobe()
    {
        if (!Editor.FreeBuilding)
            throw new InvalidOperationException("can't reset cell when not freebuilding");

        var oldEditedMicrobeOrganelles = new OrganelleLayout<OrganelleTemplate>();
        var oldMembrane = Membrane;

        foreach (var organelle in editedMicrobeOrganelles)
        {
            oldEditedMicrobeOrganelles.AddFast(organelle, hexTemporaryMemory, hexTemporaryMemory2);
        }

        NewMicrobeActionData data;
        if (IsMulticellularEditor)
        {
            // Behaviour editor is not used in multicellular
            data = new NewMicrobeActionData(oldEditedMicrobeOrganelles, oldMembrane, Rigidity, Colour, null);
        }
        else
        {
            data = new NewMicrobeActionData(oldEditedMicrobeOrganelles, oldMembrane, Rigidity, Colour,
                behaviourEditor.Behaviour ?? throw new Exception("Behaviour not initialized"));
        }

        var action =
            new SingleEditorAction<NewMicrobeActionData>(DoNewMicrobeAction, UndoNewMicrobeAction, data);

        Editor.EnqueueAction(action);
    }

    public void OnMembraneSelected(string membraneName)
    {
        var membrane = SimulationParameters.Instance.GetMembrane(membraneName);

        if (Membrane.Equals(membrane))
            return;

        var action = new SingleEditorAction<MembraneActionData>(DoMembraneChangeAction, UndoMembraneChangeAction,
            new MembraneActionData(Membrane, membrane)
            {
                CostMultiplier = CostMultiplier,
            });

        Editor.EnqueueAction(action);

        // In case the action failed, we need to make sure the membrane buttons are updated properly
        UpdateMembraneButtons(Membrane.InternalName);
    }

    public void OnRigidityChanged(int desiredRigidity)
    {
        int previousRigidity = (int)Math.Round(Rigidity * Constants.MEMBRANE_RIGIDITY_SLIDER_TO_VALUE_RATIO);

        if (CanCancelAction)
        {
            Editor.OnActionBlockedWhileMoving();
            UpdateRigiditySlider(previousRigidity);
            return;
        }

        if (previousRigidity == desiredRigidity)
            return;

        int costPerStep = (int)Math.Min(Constants.MEMBRANE_RIGIDITY_COST_PER_STEP * CostMultiplier, 100);

        var data = new RigidityActionData(desiredRigidity / Constants.MEMBRANE_RIGIDITY_SLIDER_TO_VALUE_RATIO, Rigidity)
        {
            CostMultiplier = CostMultiplier,
        };

        var cost = Editor.WhatWouldActionsCost(new[] { data });

        if (cost > Editor.MutationPoints)
        {
            int stepsToCutOff = (int)Math.Ceiling((float)(cost - Editor.MutationPoints) / costPerStep);
            data.NewRigidity -= (desiredRigidity - previousRigidity > 0 ? 1 : -1) * stepsToCutOff /
                Constants.MEMBRANE_RIGIDITY_SLIDER_TO_VALUE_RATIO;

            // Action is enqueued or canceled here, so we don't need to go on.
            UpdateRigiditySlider((int)Math.Round(data.NewRigidity * Constants.MEMBRANE_RIGIDITY_SLIDER_TO_VALUE_RATIO));
            return;
        }

        var action = new SingleEditorAction<RigidityActionData>(DoRigidityChangeAction, UndoRigidityChangeAction, data);

        Editor.EnqueueAction(action);
    }

    /// <summary>
    ///   Show options for the organelle under the cursor
    /// </summary>
    /// <returns>True when this was able to do something and consume the keypress</returns>
    [RunOnKeyDown("e_secondary")]
    public bool ShowOrganelleOptions()
    {
        // Need to prevent this from running when not visible to not conflict in an editor with multiple tabs
        if (MicrobePreviewMode || !Visible)
            return false;

        // Can't open organelle popup menu while moving something
        if (CanCancelAction)
        {
            Editor.OnActionBlockedWhileMoving();
            return true;
        }

        GetMouseHex(out int q, out int r);

        // This is a list to preserve order, Distinct is used later to ensure no duplicate organelles are added
        var organelles = new List<OrganelleTemplate>();

        RunWithSymmetry(q, r, (symmetryQ, symmetryR, _) =>
        {
            var organelle = editedMicrobeOrganelles.GetElementAt(new Hex(symmetryQ, symmetryR), hexTemporaryMemory);

            if (organelle != null)
                organelles.Add(organelle);
        });

        if (organelles.Count < 1)
            return true;

        ShowOrganelleMenu(organelles.Distinct());
        return true;
    }

    public override void OnValidAction(IEnumerable<CombinableActionData> actions)
    {
        var endosymbiontPlace = typeof(EndosymbiontPlaceActionData);

        // Most likely better to enumerate multiple times rather than allocate temporary memory
        // ReSharper disable PossibleMultipleEnumeration
        foreach (var data in actions)
        {
            var type = data.GetType();
            if (type.IsAssignableToGenericType(endosymbiontPlace))
            {
                PlayHexPlacementSound();
                break;
            }
        }

        base.OnValidAction(actions);

        // ReSharper restore PossibleMultipleEnumeration
    }

    public float CalculateSpeed()
    {
        return MicrobeInternalCalculations.CalculateSpeed(editedMicrobeOrganelles.Organelles, Membrane, Rigidity,
            !HasNucleus);
    }

    public float CalculateRotationSpeed()
    {
        return MicrobeInternalCalculations.CalculateRotationSpeed(editedMicrobeOrganelles.Organelles);
    }

    public float CalculateHitpoints()
    {
        var maxHitpoints = Membrane.Hitpoints + Rigidity * Constants.MEMBRANE_RIGIDITY_HITPOINTS_MODIFIER;

        return maxHitpoints;
    }

    public float GetNominalCapacity()
    {
        return MicrobeInternalCalculations.GetTotalNominalCapacity(editedMicrobeOrganelles);
    }

    public Dictionary<Compound, float> GetAdditionalCapacities()
    {
        // TODO: merge this with nominal get to make this more efficient
        return MicrobeInternalCalculations.GetTotalSpecificCapacity(editedMicrobeOrganelles, out _);
    }

    public float CalculateTotalDigestionSpeed()
    {
        return MicrobeInternalCalculations.CalculateTotalDigestionSpeed(editedMicrobeOrganelles);
    }

    public Dictionary<Enzyme, float> CalculateDigestionEfficiencies()
    {
        return MicrobeInternalCalculations.CalculateDigestionEfficiencies(editedMicrobeOrganelles);
    }

    public override void OnLightLevelChanged(float dayLightFraction)
    {
        var maxLightLevel = Editor.CurrentPatch.Biome.GetCompound(sunlight, CompoundAmountType.Biome).Ambient;
        var templateMaxLightLevel =
            Editor.CurrentPatch.GetCompoundAmountForDisplay(sunlight, CompoundAmountType.Template);

        // Currently, patches whose templates have zero sunlight can be given non-zero sunlight as an instance. But
        // nighttime shaders haven't been created for these patches (specifically the sea floor) so for now we can't
        // reduce light level in such patches without things looking bad. So we have to check the template light level
        // is non-zero too.
        if (maxLightLevel > 0.0f && templateMaxLightLevel > 0.0f)
        {
            camera!.LightLevel = dayLightFraction;
        }
        else
        {
            // Don't change lighting for patches without day/night effects
            camera!.LightLevel = 1.0f;
        }

        CalculateOrganelleEffectivenessInCurrentPatch();
        UpdatePatchDependentBalanceData();
    }

    public bool ApplyOrganelleUpgrade(OrganelleUpgradeActionData actionData)
    {
        actionData.CostMultiplier = CostMultiplier;

        return EnqueueAction(new CombinedEditorAction(new SingleEditorAction<OrganelleUpgradeActionData>(
            DoOrganelleUpgradeAction, UndoOrganelleUpgradeAction,
            actionData)));
    }

    protected override int CalculateCurrentActionCost()
    {
        if (string.IsNullOrEmpty(ActiveActionName) || !Editor.ShowHover)
            return 0;

        // Endosymbiosis placement is free
        if (PendingEndosymbiontPlace != null)
            return 0;

        var organelleDefinition = SimulationParameters.Instance.GetOrganelleType(ActiveActionName!);

        // Calculated in this order to be consistent with placing unique organelles
        var cost = (int)Math.Min(organelleDefinition.MPCost * CostMultiplier, 100);

        if (MouseHoverPositions == null)
            return cost * Symmetry.PositionCount();

        var positions = MouseHoverPositions.ToList();

        var organelleTemplates = positions
            .Select(h => new OrganelleTemplate(organelleDefinition, h.Hex, h.Orientation)).ToList();

        CombinedEditorAction moveOccupancies;

        if (MovingPlacedHex == null)
        {
            moveOccupancies = GetMultiActionWithOccupancies(positions, organelleTemplates, false);
        }
        else
        {
            moveOccupancies =
                GetMultiActionWithOccupancies(positions.Take(1).ToList(),
                    new List<OrganelleTemplate> { MovingPlacedHex }, true);
        }

        return Editor.WhatWouldActionsCost(moveOccupancies.Data);
    }

    protected override void PerformActiveAction()
    {
        var organelle = ActiveActionName!;

        if (AddOrganelle(organelle))
        {
            // Only trigger tutorial if an organelle was really placed
            TutorialState?.SendEvent(TutorialEventType.MicrobeEditorOrganellePlaced,
                new OrganellePlacedEventArgs(GetOrganelleDefinition(organelle)), this);
        }
    }

    protected override void PerformMove(int q, int r)
    {
        if (!MoveOrganelle(MovingPlacedHex!, new Hex(q, r),
                placementRotation))
        {
            Editor.OnInvalidAction();
        }
    }

    protected override void OnPendingActionWillSucceed()
    {
        PendingEndosymbiontPlace = null;

        base.OnPendingActionWillSucceed();

        // Update rigidity slider in case it was disabled
        // TODO: could come up with a bit nicer design here
        int intRigidity = (int)Math.Round(Rigidity * Constants.MEMBRANE_RIGIDITY_SLIDER_TO_VALUE_RATIO);
        UpdateRigiditySlider(intRigidity);
    }

    protected override bool IsMoveTargetValid(Hex position, int rotation, OrganelleTemplate organelle)
    {
        return editedMicrobeOrganelles.CanPlace(organelle.Definition, position, rotation, hexTemporaryMemory, false);
    }

    protected override void OnCurrentActionCanceled()
    {
        if (MovingPlacedHex != null)
        {
            editedMicrobeOrganelles.AddFast(MovingPlacedHex, hexTemporaryMemory, hexTemporaryMemory2);
            MovingPlacedHex = null;
        }

        PendingEndosymbiontPlace = null;

        base.OnCurrentActionCanceled();
    }

    protected override bool DoesActionEndInProgressAction(CombinedEditorAction action)
    {
        if (PendingEndosymbiontPlace != null)
        {
            return action.Data.Any(d => d is EndosymbiontPlaceActionData);
        }

        return base.DoesActionEndInProgressAction(action);
    }

    protected override void OnMoveActionStarted()
    {
        editedMicrobeOrganelles.Remove(MovingPlacedHex!);
    }

    protected override OrganelleTemplate? GetHexAt(Hex position)
    {
        return editedMicrobeOrganelles.GetElementAt(position, hexTemporaryMemory);
    }

    protected override EditorAction? TryCreateRemoveHexAtAction(Hex location, ref int alreadyDeleted)
    {
        var organelleHere = editedMicrobeOrganelles.GetElementAt(location, hexTemporaryMemory);
        if (organelleHere == null)
            return null;

        // Don't allow deletion of nucleus or the last organelle
        if (organelleHere.Definition == nucleus || MicrobeSize - alreadyDeleted < 2)
            return null;

        // In multicellular binding agents can't be removed
        if (IsMulticellularEditor && organelleHere.Definition == bindingAgent)
            return null;

        ++alreadyDeleted;
        return new SingleEditorAction<OrganelleRemoveActionData>(DoOrganelleRemoveAction, UndoOrganelleRemoveAction,
            new OrganelleRemoveActionData(organelleHere)
            {
                CostMultiplier = CostMultiplier,
            });
    }

    protected void UpdateOrganellePlaceHolderScene(SceneDisplayer organelleModel,
        LoadedSceneWithModelInfo displayScene, int renderPriority)
    {
        UpdateOrganellePlaceHolderScene(organelleModel, displayScene, renderPriority, temporaryDisplayerFetchList);
    }

    protected override float CalculateEditorArrowZPosition()
    {
        // The calculation falls back to 0 if there are no hexes found in the middle 3 rows
        var highestPointInMiddleRows = 0.0f;

        // Iterate through all organelles
        foreach (var organelle in editedMicrobeOrganelles)
        {
            // Iterate through all hexes
            foreach (var relativeHex in organelle.Definition.Hexes)
            {
                var absoluteHex = relativeHex + organelle.Position;

                // Only consider the middle 3 rows
                if (absoluteHex.Q is < -1 or > 1)
                    continue;

                var cartesian = Hex.AxialToCartesian(absoluteHex);

                // Get the min z-axis (highest point in the editor)
                highestPointInMiddleRows = Mathf.Min(highestPointInMiddleRows, cartesian.Z);
            }
        }

        return highestPointInMiddleRows;
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            if (TopPanelPath != null)
            {
                TopPanelPath.Dispose();
                DayButtonPath.Dispose();
                NightButtonPath.Dispose();
                AverageLightButtonPath.Dispose();
                CurrentLightButtonPath.Dispose();
                TabButtonsPath.Dispose();
                StructureTabButtonPath.Dispose();
                AppearanceTabButtonPath.Dispose();
                BehaviourTabButtonPath.Dispose();
                StructureTabPath.Dispose();
                AppearanceTabPath.Dispose();
                BehaviourTabPath.Dispose();
                PartsSelectionContainerPath.Dispose();
                MembraneTypeSelectionPath.Dispose();
                SizeLabelPath.Dispose();
                OrganismStatisticsPath.Dispose();
                SpeedLabelPath.Dispose();
                RotationSpeedLabelPath.Dispose();
                HpLabelPath.Dispose();
                StorageLabelPath.Dispose();
                DigestionSpeedLabelPath.Dispose();
                DigestionEfficiencyLabelPath.Dispose();
                GenerationLabelPath.Dispose();
                AutoEvoPredictionPanelPath.Dispose();
                TotalEnergyLabelPath.Dispose();
                AutoEvoPredictionFailedLabelPath.Dispose();
                WorstPatchLabelPath.Dispose();
                BestPatchLabelPath.Dispose();
                MembraneColorPickerPath.Dispose();
                ATPBalancePanelPath.Dispose();
                ATPProductionLabelPath.Dispose();
                ATPConsumptionLabelPath.Dispose();
                ATPProductionBarPath.Dispose();
                ATPConsumptionBarPath.Dispose();
                RigiditySliderPath.Dispose();
                NegativeAtpPopupPath.Dispose();
                OrganelleMenuPath.Dispose();
                AutoEvoPredictionExplanationPopupPath.Dispose();
                AutoEvoPredictionExplanationLabelPath.Dispose();
                OrganelleUpgradeGUIPath.Dispose();
                RightPanelScrollContainerPath.Dispose();
            }

            previewSimulation?.Dispose();
        }

        base.Dispose(disposing);
    }

    private bool PerformEndosymbiosisPlace(int q, int r)
    {
        if (PendingEndosymbiontPlace == null)
        {
            GD.PrintErr("No endosymbiosis place in progress, there should be at this point");
            Editor.OnInvalidAction();
            return false;
        }

        PendingEndosymbiontPlace.PlacementLocation = new Hex(q, r);
        PendingEndosymbiontPlace.PlacementRotation = placementRotation;

        PendingEndosymbiontPlace.PlacedOrganelle.Orientation = placementRotation;
        PendingEndosymbiontPlace.PlacedOrganelle.Position = new Hex(q, r);

        // Before finalizing the data, make sure it can be placed at the current position
        if (!IsValidPlacement(PendingEndosymbiontPlace.PlacedOrganelle, false))
        {
            Editor.OnInvalidAction();
            return false;
        }

        var action = new CombinedEditorAction(new SingleEditorAction<EndosymbiontPlaceActionData>(
            DoEndosymbiontPlaceAction, UndoEndosymbiontPlaceAction, PendingEndosymbiontPlace));

        EnqueueAction(action);

        return true;
    }

    private bool CreatePreviewMicrobeIfNeeded()
    {
        if (previewSimulation == null)
            throw new InvalidOperationException("Component needs to be initialized first");

        if (previewMicrobe.IsAlive && previewMicrobeSpecies != null)
            return false;

        if (cellPreviewVisualsRoot == null)
        {
            throw new InvalidOperationException("Editor component not initialized yet (cell visuals root missing)");
        }

        previewMicrobeSpecies = new MicrobeSpecies(Editor.EditedBaseSpecies,
            Editor.EditedCellProperties ??
            throw new InvalidOperationException("can't setup preview before cell properties are known"),
            hexTemporaryMemory, hexTemporaryMemory2)
        {
            // Force large normal size (instead of showing bacteria as smaller scale than the editor hexes)
            IsBacteria = false,
        };

        previewMicrobe = previewSimulation.CreateVisualisationMicrobe(previewMicrobeSpecies);

        // Set its initial visibility
        cellPreviewVisualsRoot.Visible = MicrobePreviewMode;

        return true;
    }

    /// <summary>
    ///   Updates the membrane and organelle placement of the preview cell.
    /// </summary>
    private void UpdateCellVisualization()
    {
        if (previewMicrobeSpecies == null)
            return;

        // Don't redo the preview cell when not in the preview mode to avoid unnecessary lags
        if (!MicrobePreviewMode || !microbeVisualizationOrganellePositionsAreDirty)
            return;

        CopyEditedPropertiesToSpecies(previewMicrobeSpecies);

        // Intentionally force it to not be bacteria to show it at full size
        previewMicrobeSpecies.IsBacteria = false;

        // This is now just for applying changes in the species to the preview cell
        previewSimulation!.ApplyNewVisualisationMicrobeSpecies(previewMicrobe, previewMicrobeSpecies);

        microbeVisualizationOrganellePositionsAreDirty = false;
    }

    private bool HasOrganelle(OrganelleDefinition organelleDefinition)
    {
        return editedMicrobeOrganelles.Organelles.Any(o => o.Definition == organelleDefinition);
    }

    private void UpdateRigiditySlider(int value)
    {
        rigiditySlider.Value = value;
        SetRigiditySliderTooltip(value);
    }

    private void ShowOrganelleMenu(IEnumerable<OrganelleTemplate> selectedOrganelles)
    {
        var organelles = selectedOrganelles.ToList();
        organelleMenu.SelectedOrganelles = organelles;
        organelleMenu.CostMultiplier = CostMultiplier;
        organelleMenu.GetActionPrice = Editor.WhatWouldActionsCost;
        organelleMenu.ShowPopup = true;

        var count = organelles.Count;

        // Disable delete for nucleus or the last organelle.
        bool attemptingNucleusDelete = organelles.Any(o => o.Definition == nucleus);
        if (MicrobeSize <= count || attemptingNucleusDelete)
        {
            organelleMenu.EnableDeleteOption = false;

            organelleMenu.DeleteOptionTooltip = attemptingNucleusDelete ?
                Localization.Translate("NUCLEUS_DELETE_OPTION_DISABLED_TOOLTIP") :
                Localization.Translate("LAST_ORGANELLE_DELETE_OPTION_DISABLED_TOOLTIP");
        }
        else
        {
            // Additionally in multicellular binding agents can't be removed
            if (IsMulticellularEditor && organelles.Any(o => o.Definition == bindingAgent))
            {
                organelleMenu.EnableDeleteOption = false;
            }
            else
            {
                organelleMenu.EnableDeleteOption = true;
            }

            organelleMenu.DeleteOptionTooltip = string.Empty;
        }

        // Move enabled only when microbe has more than one organelle
        organelleMenu.EnableMoveOption = MicrobeSize > 1;

        // Modify / upgrade possible when defined on the primary organelle definition
        if (count > 0 && IsUpgradingPossibleFor(organelles.First().Definition))
        {
            organelleMenu.EnableModifyOption = true;
        }
        else
        {
            organelleMenu.EnableModifyOption = false;
        }
    }

    private bool IsUpgradingPossibleFor(OrganelleDefinition organelleDefinition)
    {
        return !string.IsNullOrEmpty(organelleDefinition.UpgradeGUI) || organelleDefinition.AvailableUpgrades.Count > 0;
    }

    /// <summary>
    ///   Returns a list with hex, orientation, the organelle and whether or not this hex is already occupied by a
    ///   higher-ranked organelle.
    /// </summary>
    /// <remarks>
    ///   <para>
    ///     An organelle is ranked higher if it costs more MP.
    ///   </para>
    ///   <para>
    ///     TODO: figure out why the OrganelleTemplate in the tuples used here can be null and simplify the logic if
    ///     it's possible
    ///   </para>
    /// </remarks>
    private IEnumerable<(Hex Hex, OrganelleTemplate Organelle, int Orientation, bool Occupied)> GetOccupancies(
        List<(Hex Hex, int Orientation)> hexes, List<OrganelleTemplate> organelles)
    {
        var organellePositions = new List<(Hex Hex, OrganelleTemplate? Organelle, int Orientation, bool Occupied)>();
        for (var i = 0; i < hexes.Count; i++)
        {
            var (hex, orientation) = hexes[i];
            var organelle = organelles[i];
            var oldOrganelle = organellePositions.FirstOrDefault(p => p.Hex == hex);
            var occupied = false;
            if (oldOrganelle != default)
            {
                if (organelle.Definition.MPCost > oldOrganelle.Organelle?.Definition.MPCost)
                {
                    organellePositions.Remove(oldOrganelle);
                    oldOrganelle.Occupied = true;
                    organellePositions.Add(oldOrganelle);
                }
                else
                {
                    occupied = true;
                }
            }

            organellePositions.Add((hex, organelle, orientation, occupied));
        }

        return organellePositions.Where(t => t.Organelle != null)!;
    }

    private CombinedEditorAction GetMultiActionWithOccupancies(List<(Hex Hex, int Orientation)> hexes,
        List<OrganelleTemplate> organelles, bool moving)
    {
        var actions = new List<EditorAction>();
        foreach (var (hex, organelle, orientation, occupied) in GetOccupancies(hexes, organelles))
        {
            EditorAction action;
            if (occupied)
            {
                var data = new OrganelleRemoveActionData(organelle)
                {
                    GotReplaced = organelle.Definition.InternalName == cytoplasm.InternalName,
                    CostMultiplier = CostMultiplier,
                };
                action = new SingleEditorAction<OrganelleRemoveActionData>(DoOrganelleRemoveAction,
                    UndoOrganelleRemoveAction, data);
            }
            else
            {
                if (moving)
                {
                    var data = new OrganelleMoveActionData(organelle, organelle.Position, hex, organelle.Orientation,
                        orientation)
                    {
                        CostMultiplier = CostMultiplier,
                    };
                    action = new SingleEditorAction<OrganelleMoveActionData>(DoOrganelleMoveAction,
                        UndoOrganelleMoveAction, data);
                }
                else
                {
                    var data = new OrganellePlacementActionData(organelle, hex, orientation)
                    {
                        CostMultiplier = CostMultiplier,
                    };

                    var replacedHexes = organelle.RotatedHexes
                        .Select(h => editedMicrobeOrganelles.GetElementAt(hex + h, hexTemporaryMemory)).WhereNotNull()
                        .ToList();

                    if (replacedHexes.Count > 0)
                        data.ReplacedCytoplasm = replacedHexes;

                    action = new SingleEditorAction<OrganellePlacementActionData>(DoOrganellePlaceAction,
                        UndoOrganellePlaceAction, data);
                }
            }

            actions.Add(action);
        }

        return new CombinedEditorAction(actions);
    }

    private IEnumerable<OrganelleTemplate> GetReplacedCytoplasm(IEnumerable<OrganelleTemplate> organelles)
    {
        foreach (var templateHex in organelles
                     .Where(o => o.Definition.InternalName != cytoplasm.InternalName)
                     .SelectMany(o => o.RotatedHexes.Select(h => h + o.Position)))
        {
            var existingOrganelle = editedMicrobeOrganelles.GetElementAt(templateHex, hexTemporaryMemory);

            if (existingOrganelle != null && existingOrganelle.Definition.InternalName == cytoplasm.InternalName)
            {
                yield return existingOrganelle;
            }
        }
    }

    private IEnumerable<OrganelleRemoveActionData> GetReplacedCytoplasmRemoveActionData(
        IEnumerable<OrganelleTemplate> organelles)
    {
        return GetReplacedCytoplasm(organelles)
            .Select(o => new OrganelleRemoveActionData(o)
            {
                GotReplaced = true,
                CostMultiplier = CostMultiplier,
            });
    }

    private IEnumerable<SingleEditorAction<OrganelleRemoveActionData>> GetReplacedCytoplasmRemoveAction(
        IEnumerable<OrganelleTemplate> organelles)
    {
        var replacedCytoplasmData = GetReplacedCytoplasmRemoveActionData(organelles);
        return replacedCytoplasmData.Select(o =>
            new SingleEditorAction<OrganelleRemoveActionData>(DoOrganelleRemoveAction, UndoOrganelleRemoveAction, o));
    }

    private void StartAutoEvoPrediction()
    {
        // For now disabled in the multicellular editor as the microbe logic being used there doesn't make sense
        if (IsMulticellularEditor)
            return;

        // First prediction can be made only after population numbers from previous run are applied
        // so this is just here to guard against that potential programming mistake that may happen when code is
        // changed
        if (!Editor.EditorReady)
        {
            GD.PrintErr("Can't start auto-evo prediction before editor is ready");
            return;
        }

        // Note that in rare cases the auto-evo run doesn't manage to stop before we edit the cached species object
        // which may cause occasional background task errors
        CancelPreviousAutoEvoPrediction();

        cachedAutoEvoPredictionSpecies ??= new MicrobeSpecies(Editor.EditedBaseSpecies,
            Editor.EditedCellProperties ??
            throw new InvalidOperationException("can't start auto-evo prediction without current cell properties"),
            hexTemporaryMemory, hexTemporaryMemory2);

        CopyEditedPropertiesToSpecies(cachedAutoEvoPredictionSpecies);

        var run = new EditorAutoEvoRun(Editor.CurrentGame.GameWorld, Editor.EditedBaseSpecies,
            cachedAutoEvoPredictionSpecies);
        run.Start();

        UpdateAutoEvoPrediction(run, Editor.EditedBaseSpecies, cachedAutoEvoPredictionSpecies);
    }

    /// <summary>
    ///   Calculates the energy balance and compound balance for a cell with the given organelles and membrane
    /// </summary>
    private void CalculateEnergyAndCompoundBalance(IReadOnlyList<OrganelleTemplate> organelles,
        MembraneType membrane, BiomeConditions? biome = null)
    {
        biome ??= Editor.CurrentPatch.Biome;

        bool moving = calculateBalancesWhenMoving.ButtonPressed;

        // TODO: pass moving variable
        var energyBalance = ProcessSystem.ComputeEnergyBalance(organelles, biome, membrane, moving, true,
            Editor.CurrentGame.GameWorld.WorldSettings,
            calculateBalancesAsIfDay.ButtonPressed ? CompoundAmountType.Biome : CompoundAmountType.Current);

        UpdateEnergyBalance(energyBalance);

        float nominalStorage = 0;
        Dictionary<Compound, float>? specificStorages = null;

        var compoundBalanceData =
            CalculateCompoundBalanceWithMethod(compoundBalance.CurrentDisplayType,
                calculateBalancesAsIfDay.ButtonPressed ? CompoundAmountType.Biome : CompoundAmountType.Current,
                organelles, biome, energyBalance,
                ref specificStorages, ref nominalStorage);

        UpdateCompoundBalances(compoundBalanceData);

        var nightBalanceData = CalculateCompoundBalanceWithMethod(compoundBalance.CurrentDisplayType,
            CompoundAmountType.Minimum, organelles, biome, energyBalance, ref specificStorages, ref nominalStorage);

        UpdateCompoundLastingTimes(compoundBalanceData, nightBalanceData, nominalStorage,
            specificStorages ?? throw new Exception("Special storages should have been calculated"));

        // Handle process list
        HandleProcessList(energyBalance, biome);
    }

    private Dictionary<Compound, CompoundBalance> CalculateCompoundBalanceWithMethod(BalanceDisplayType calculationType,
        CompoundAmountType amountType, IReadOnlyList<OrganelleTemplate> organelles,
        BiomeConditions biome, EnergyBalanceInfo energyBalance, ref Dictionary<Compound, float>? specificStorages,
        ref float nominalStorage)
    {
        Dictionary<Compound, CompoundBalance> compoundBalanceData;
        switch (calculationType)
        {
            case BalanceDisplayType.MaxSpeed:
                compoundBalanceData =
                    ProcessSystem.ComputeCompoundBalance(organelles, biome, amountType);
                break;
            case BalanceDisplayType.EnergyEquilibrium:
                compoundBalanceData = ProcessSystem.ComputeCompoundBalanceAtEquilibrium(organelles, biome,
                    amountType, energyBalance);
                break;
            default:
                GD.PrintErr("Unknown compound balance type: ", compoundBalance.CurrentDisplayType);
                goto case BalanceDisplayType.EnergyEquilibrium;
        }

        specificStorages ??= MicrobeInternalCalculations.GetTotalSpecificCapacity(organelles, out nominalStorage);

        return ProcessSystem.ComputeCompoundFillTimes(compoundBalanceData, nominalStorage, specificStorages);
    }

    private void HandleProcessList(EnergyBalanceInfo energyBalance, BiomeConditions biome)
    {
        var processes = new List<TweakedProcess>();

        // Empty list to later fill
        var processStatistics = new List<ProcessSpeedInformation>();

        ProcessSystem.ComputeActiveProcessList(editedMicrobeOrganelles, ref processes);

        float consumptionProductionRatio = energyBalance.TotalConsumption / energyBalance.TotalProduction;

        foreach (var process in processes)
        {
            var singleProcess = ProcessSystem.CalculateProcessMaximumSpeed(process, biome, CompoundAmountType.Current);

            // If produces more ATP than consumes, lower down production for inputs and for outputs,
            // otherwise use maximum production values (this matches the equilibrium display mode and what happens
            // in game once exiting the editor)
            if (consumptionProductionRatio < 1.0f)
            {
                singleProcess.ScaleSpeed(consumptionProductionRatio, processSpeedWorkMemory);
            }

            processStatistics.Add(singleProcess);
        }

        processList.ProcessesToShow = processStatistics;
    }

    /// <summary>
    ///   If not hovering over an organelle, render the to-be-placed organelle
    /// </summary>
    private void RenderHighlightedOrganelle(int q, int r, int rotation, OrganelleDefinition shownOrganelleDefinition,
        OrganelleUpgrades? upgrades)
    {
        RenderHoveredHex(q, r, shownOrganelleDefinition.GetRotatedHexes(rotation), isPlacementProbablyValid,
            out bool hadDuplicate);

        bool showModel = !hadDuplicate;

        // Model
        if (showModel && shownOrganelleDefinition.TryGetGraphicsScene(upgrades, out var modelInfo))
        {
            var cartesianPosition = Hex.AxialToCartesian(new Hex(q, r));

            var organelleModel = hoverModels[usedHoverModel++];

            organelleModel.Transform = new Transform3D(new Basis(MathUtils.CreateRotationForOrganelle(rotation)),
                cartesianPosition + shownOrganelleDefinition.ModelOffset);

            organelleModel.Scale = new Vector3(Constants.DEFAULT_HEX_SIZE, Constants.DEFAULT_HEX_SIZE,
                Constants.DEFAULT_HEX_SIZE);

            organelleModel.Visible = true;

            UpdateOrganellePlaceHolderScene(organelleModel, modelInfo, Hex.GetRenderPriority(new Hex(q, r)));
        }
    }

    /// <summary>
    ///   Places an organelle of the specified type under the cursor and also applies symmetry to
    ///   place multiple at once.
    /// </summary>
    /// <returns>True when at least one organelle got placed</returns>
    private bool AddOrganelle(string organelleType)
    {
        GetMouseHex(out int q, out int r);

        var placementActions = new List<EditorAction>();

        // For multi hex organelles we keep track of positions that got filled in
        var usedHexes = new HashSet<Hex>();

        HexEditorSymmetry? overrideSymmetry =
            componentBottomLeftButtons.SymmetryEnabled ? null : HexEditorSymmetry.None;

        RunWithSymmetry(q, r,
            (attemptQ, attemptR, rotation) =>
            {
                var organelle = new OrganelleTemplate(GetOrganelleDefinition(organelleType),
                    new Hex(attemptQ, attemptR), rotation);

                var hexes = organelle.RotatedHexes.Select(h => h + new Hex(attemptQ, attemptR)).ToList();

                foreach (var hex in hexes)
                {
                    if (usedHexes.Contains(hex))
                    {
                        // Duplicate with already placed
                        return;
                    }
                }

                var placed = CreatePlaceActionIfPossible(organelle);

                if (placed != null)
                {
                    placementActions.Add(placed);

                    foreach (var hex in hexes)
                    {
                        usedHexes.Add(hex);
                    }
                }
            }, overrideSymmetry);

        if (placementActions.Count < 1)
            return false;

        var multiAction = new CombinedEditorAction(placementActions);

        return EnqueueAction(multiAction);
    }

    /// <summary>
    ///   Helper for AddOrganelle
    /// </summary>
    private CombinedEditorAction? CreatePlaceActionIfPossible(OrganelleTemplate organelle)
    {
        if (MicrobePreviewMode)
            return null;

        if (!IsValidPlacement(organelle, true))
        {
            // Play Sound
            Editor.OnInvalidAction();
            return null;
        }

        return CreateAddOrganelleAction(organelle);
    }

    private bool IsValidPlacement(OrganelleTemplate organelle, bool allowOverwritingCytoplasm)
    {
        bool notPlacingCytoplasm = organelle.Definition.InternalName != cytoplasm.InternalName;

        if (!allowOverwritingCytoplasm)
            notPlacingCytoplasm = false;

        return editedMicrobeOrganelles.CanPlaceAndIsTouching(organelle,
            notPlacingCytoplasm, hexTemporaryMemory, hexTemporaryMemory2,
            notPlacingCytoplasm);
    }

    private CombinedEditorAction? CreateAddOrganelleAction(OrganelleTemplate organelle)
    {
        // 1 - you put a unique organelle (means only one instance allowed) but you already have it
        // 2 - you put an organelle that requires nucleus but you don't have one
        if ((organelle.Definition.Unique && HasOrganelle(organelle.Definition)) ||
            (organelle.Definition.RequiresNucleus && !HasNucleus))
        {
            return null;
        }

        if (organelle.Definition.Unique)
            DeselectOrganelleToPlace();

        var replacedCytoplasmActions =
            GetReplacedCytoplasmRemoveAction(new[] { organelle }).Cast<EditorAction>().ToList();

        var action = new SingleEditorAction<OrganellePlacementActionData>(DoOrganellePlaceAction,
            UndoOrganellePlaceAction,
            new OrganellePlacementActionData(organelle, organelle.Position, organelle.Orientation)
            {
                CostMultiplier = CostMultiplier,
            });

        replacedCytoplasmActions.Add(action);
        return new CombinedEditorAction(replacedCytoplasmActions);
    }

    /// <summary>
    ///   Finishes an organelle move
    /// </summary>
    /// <returns>True if the organelle move succeeded.</returns>
    private bool MoveOrganelle(OrganelleTemplate organelle, Hex newLocation, int newRotation)
    {
        // TODO: consider allowing rotation inplace (https://github.com/Revolutionary-Games/Thrive/issues/2993)

        if (MicrobePreviewMode)
            return false;

        // Make sure placement is valid
        if (!IsMoveTargetValid(newLocation, newRotation, organelle))
            return false;

        var multiAction = GetMultiActionWithOccupancies(
            new List<(Hex Hex, int Orientation)> { (newLocation, newRotation) },
            new List<OrganelleTemplate> { organelle }, true);

        // Too low mutation points, cancel move
        if (Editor.MutationPoints < Editor.WhatWouldActionsCost(multiAction.Data))
        {
            CancelCurrentAction();
            Editor.OnInsufficientMP(false);
            return false;
        }

        EnqueueAction(multiAction);

        // It's assumed that the above enqueue can't fail, otherwise the reference to MovingPlacedHex may be
        // permanently lost (as the code that calls this assumes it's safe to set MovingPlacedHex to null
        // when we return true)
        return true;
    }

    private bool IsNegativeAtpProduction()
    {
        return energyBalanceInfo != null &&
            energyBalanceInfo.TotalProduction < energyBalanceInfo.TotalConsumptionStationary;
    }

    private void OnPostNewMicrobeChange()
    {
        UpdateMembraneButtons(Membrane.InternalName);
        UpdateStats();
        OnRigidityChanged();
        OnColourChanged();

        StartAutoEvoPrediction();
    }

    private void OnRigidityChanged()
    {
        UpdateRigiditySlider((int)Math.Round(Rigidity * Constants.MEMBRANE_RIGIDITY_SLIDER_TO_VALUE_RATIO));

        UpdateSpeed(CalculateSpeed());
        UpdateRotationSpeed(CalculateRotationSpeed());
        UpdateHitpoints(CalculateHitpoints());
    }

    private void OnColourChanged()
    {
        membraneColorPicker.SetColour(Colour);
    }

    private void UpdateStats()
    {
        UpdateSpeed(CalculateSpeed());
        UpdateRotationSpeed(CalculateRotationSpeed());
        UpdateHitpoints(CalculateHitpoints());
        UpdateStorage(GetNominalCapacity(), GetAdditionalCapacities());
        UpdateTotalDigestionSpeed(CalculateTotalDigestionSpeed());
        UpdateDigestionEfficiencies(CalculateDigestionEfficiencies());
    }

    /// <summary>
    ///   Lock / unlock the organelles that need a nucleus
    /// </summary>
    private void UpdatePartsAvailability(List<OrganelleDefinition> placedUniqueOrganelleNames)
    {
        foreach (var organelle in placeablePartSelectionElements.Keys)
        {
            UpdatePartAvailability(placedUniqueOrganelleNames, organelle);
        }
    }

    private void OnOrganelleToPlaceSelected(string organelle)
    {
        if (ActiveActionName == organelle)
            return;

        ActiveActionName = organelle;

        ApplySymmetryForCurrentOrganelle();
        UpdateOrganelleButtons(organelle);
    }

    private void DeselectOrganelleToPlace()
    {
        ActiveActionName = null;
        UpdateOrganelleButtons(null);
    }

    private void UpdateOrganelleButtons(string? selectedOrganelle)
    {
        // Update the icon highlightings
        foreach (var selection in placeablePartSelectionElements.Values)
        {
            selection.Selected = selection.Name == selectedOrganelle;
        }
    }

    private void UpdateMembraneButtons(string membrane)
    {
        // Update the icon highlightings
        foreach (var selection in membraneSelectionElements.Values)
        {
            selection.Selected = selection.Name == membrane;
        }
    }

    private void OnOrganellesChanged()
    {
        UpdateAlreadyPlacedVisuals();

        UpdateArrow();

        UpdatePartsAvailability(PlacedUniqueOrganelles.ToList());

        UpdatePatchDependentBalanceData();

        // Send to gui current status of cell
        UpdateSize(MicrobeHexSize);
        UpdateStats();

        UpdateCellVisualization();

        StartAutoEvoPrediction();

        UpdateFinishButtonWarningVisibility();

        // Updated here to make sure everything else has been updated first so tooltips are accurate
        UpdateOrganelleUnlockTooltips(false);
    }

    /// <summary>
    ///   This destroys and creates again entities to represent all the currently placed organelles. Call this whenever
    ///   editedMicrobeOrganelles is changed.
    /// </summary>
    private void UpdateAlreadyPlacedVisuals()
    {
        editedMicrobeOrganelles.GetIslandHexes(islandResults, islandsWorkMemory1, islandsWorkMemory2,
            islandsWorkMemory3);

        // TODO: The code below is partly duplicate to CellHexPhotoBuilder. If this is changed that needs changes too.
        // Build the entities to show the current microbe
        UpdateAlreadyPlacedHexes(editedMicrobeOrganelles.Select(o => (o.Position, o.RotatedHexes,
            Editor.HexPlacedThisSession<OrganelleTemplate, CellType>(o))), islandResults, microbePreviewMode);

        int nextFreeOrganelle = 0;

        foreach (var organelle in editedMicrobeOrganelles)
        {
            // Hexes are handled by UpdateAlreadyPlacedHexes

            // Model of the organelle
            if (organelle.Definition.TryGetGraphicsScene(organelle.Upgrades, out var modelInfo))
            {
                if (nextFreeOrganelle >= placedModels.Count)
                {
                    // New organelle model needed
                    placedModels.Add(CreatePreviewModelHolder());
                }

                var organelleModel = placedModels[nextFreeOrganelle++];

                UpdateOrganelleDisplayerTransform(organelleModel, organelle);

                organelleModel.Visible = !MicrobePreviewMode;

                UpdateOrganellePlaceHolderScene(organelleModel, modelInfo, Hex.GetRenderPriority(organelle.Position));
            }
        }

        while (nextFreeOrganelle < placedModels.Count)
        {
            placedModels[placedModels.Count - 1].DetachAndQueueFree();
            placedModels.RemoveAt(placedModels.Count - 1);
        }
    }

    private void SetSpeciesInfo(string name, MembraneType membrane, Color colour, float rigidity,
        BehaviourDictionary? behaviour)
    {
        componentBottomLeftButtons.SetNewName(name);

        membraneColorPicker.Color = colour;

        UpdateMembraneButtons(membrane.InternalName);
        SetMembraneTooltips(membrane);

        UpdateRigiditySlider((int)Math.Round(rigidity * Constants.MEMBRANE_RIGIDITY_SLIDER_TO_VALUE_RATIO));

        // TODO: put this call in some better place (also in CellBodyPlanEditorComponent)
        if (!IsMulticellularEditor)
        {
            behaviourEditor.UpdateAllBehaviouralSliders(behaviour ??
                throw new ArgumentNullException(nameof(behaviour)));
        }
    }

    private void OnMovePressed()
    {
        if (Settings.Instance.MoveOrganellesWithSymmetry.Value)
        {
            // Start moving the organelles symmetrical to the clicked organelle.
            StartHexMoveWithSymmetry(organelleMenu.SelectedOrganelles);
        }
        else
        {
            StartHexMove(organelleMenu.SelectedOrganelles.First());
        }
    }

    private void OnDeletePressed()
    {
        int alreadyDeleted = 0;
        var action =
            new CombinedEditorAction(organelleMenu.SelectedOrganelles
                .Select(o => TryCreateRemoveHexAtAction(o.Position, ref alreadyDeleted)).WhereNotNull());
        EnqueueAction(action);
    }

    private void OnModifyPressed()
    {
        var targetOrganelle = organelleMenu.SelectedOrganelles.First();
        var upgradeGUI = targetOrganelle.Definition.UpgradeGUI;

        if (!IsUpgradingPossibleFor(targetOrganelle.Definition))
        {
            GD.PrintErr("Attempted to modify an organelle that can't be upgraded");
            return;
        }

        if (TutorialState?.Enabled == true)
        {
            TutorialState.SendEvent(TutorialEventType.MicrobeEditorOrganelleModified, EventArgs.Empty, this);
        }

        organelleUpgradeGUI.OpenForOrganelle(targetOrganelle, upgradeGUI ?? string.Empty, this, Editor, CostMultiplier,
            Editor.CurrentGame);
    }

    /// <summary>
    ///   Lock / unlock organelle buttons that need a nucleus or are already placed (if unique)
    /// </summary>
    private void UpdatePartAvailability(List<OrganelleDefinition> placedUniqueOrganelleNames,
        OrganelleDefinition organelle)
    {
        var item = placeablePartSelectionElements[organelle];

        if (organelle.Unique && placedUniqueOrganelleNames.Contains(organelle))
        {
            item.Locked = true;
        }
        else if (organelle.RequiresNucleus && !placedUniqueOrganelleNames.Contains(nucleus))
        {
            item.Locked = true;
        }
        else
        {
            item.Locked = false;
        }

        item.RecentlyUnlocked = Editor.CurrentGame.GameWorld.UnlockProgress.RecentlyUnlocked(organelle);
    }

    /// <summary>
    ///   Creates part and membrane selection buttons
    /// </summary>
    /// <remarks>
    ///   <para>
    ///     This doesn't multiply the shown MP Cost by the cost factor as this is called much earlier before editor is
    ///     initialized proper, for that use <see cref="UpdateMicrobePartSelections"/> or <see cref="UpdateMPCost"/>.
    ///   </para>
    /// </remarks>
    private void SetupMicrobePartSelections()
    {
        var simulationParameters = SimulationParameters.Instance;

        var organelleButtonGroup = new ButtonGroup();
        var membraneButtonGroup = new ButtonGroup();

        foreach (var organelle in simulationParameters.GetAllOrganelles().OrderBy(o => o.EditorButtonOrder))
        {
            if (organelle.EditorButtonGroup == OrganelleDefinition.OrganelleGroup.Hidden)
                continue;

            var group = partsSelectionContainer.GetNode<CollapsibleList>(organelle.EditorButtonGroup.ToString());

            if (group == null)
            {
                GD.PrintErr("No node found for organelle selection button for ", organelle.InternalName);
                return;
            }

            var control = organelleSelectionButtonScene.Instantiate<MicrobePartSelection>();
            control.Locked = organelle.Unimplemented;
            control.PartIcon = organelle.LoadedIcon ?? throw new Exception("Organelle with no icon");
            control.PartName = organelle.UntranslatedName;
            control.SelectionGroup = organelleButtonGroup;
            control.MPCost = organelle.MPCost;
            control.Name = organelle.InternalName;

            // Special case with registering the tooltip here for item with no associated organelle
            control.RegisterToolTipForControl(organelle.InternalName, "organelleSelection");

            group.AddItem(control);

            allPartSelectionElements.Add(organelle, control);

            if (organelle.Unimplemented)
                continue;

            // Only add items with valid organelles to dictionary
            placeablePartSelectionElements.Add(organelle, control);

            control.Connect(MicrobePartSelection.SignalName.OnPartSelected,
                new Callable(this, nameof(OnOrganelleToPlaceSelected)));
        }

        foreach (var membraneType in simulationParameters.GetAllMembranes().OrderBy(m => m.EditorButtonOrder))
        {
            var control = organelleSelectionButtonScene.Instantiate<MicrobePartSelection>();
            control.PartIcon = membraneType.LoadedIcon;
            control.PartName = membraneType.UntranslatedName;
            control.SelectionGroup = membraneButtonGroup;
            control.MPCost = membraneType.EditorCost;
            control.Name = membraneType.InternalName;

            control.RegisterToolTipForControl(membraneType.InternalName, "membraneSelection");

            membraneTypeSelection.AddItem(control);

            membraneSelectionElements.Add(membraneType, control);

            control.Connect(MicrobePartSelection.SignalName.OnPartSelected,
                new Callable(this, nameof(OnMembraneSelected)));
        }

        // Multicellular parts only available (visible) in multicellular
        partsSelectionContainer.GetNode<CollapsibleList>(OrganelleDefinition.OrganelleGroup.Multicellular.ToString())
            .Visible = IsMulticellularEditor;
    }

    private void OnSpeciesNameChanged(string newText)
    {
        newName = newText;

        if (IsMulticellularEditor)
        {
            // TODO: somehow update the architecture so that we can know here if the name conflicts with another type
            componentBottomLeftButtons.ReportValidityOfName(!string.IsNullOrWhiteSpace(newText));
        }
    }

    private void OnColorChanged(Color color)
    {
        if (MovingPlacedHex != null)
        {
            Editor.OnActionBlockedWhileMoving();
            membraneColorPicker.SetColour(Colour);
            return;
        }

        if (Colour == color)
            return;

        var action = new SingleEditorAction<ColourActionData>(DoColourChangeAction, UndoColourChangeAction,
            new ColourActionData(color, Colour)
            {
                CostMultiplier = CostMultiplier,
            });

        Editor.EnqueueAction(action);
    }

    /// <summary>
    ///   "Searches" an organelle selection button by hiding the ones whose name doesn't include the input substring
    /// </summary>
    /// <remarks>
    ///   <para>
    ///     NOTE: this is not currently used
    ///   </para>
    /// </remarks>
    private void OnSearchBoxTextChanged(string newText)
    {
        var input = newText.ToLower(CultureInfo.InvariantCulture);

        var organelles = SimulationParameters.Instance.GetAllOrganelles().Where(
            o => o.Name.ToLower(CultureInfo.CurrentCulture).Contains(input)).ToList();

        foreach (var node in placeablePartSelectionElements.Values)
        {
            // To show back organelles that simulation parameters didn't include
            if (string.IsNullOrEmpty(input))
            {
                node.Show();
                continue;
            }

            node.Hide();

            foreach (var organelle in organelles)
            {
                if (node.Name == organelle.InternalName)
                {
                    node.Show();
                }
            }
        }
    }

    /// <summary>
    ///   Copies current editor state to a species
    /// </summary>
    /// <param name="target">The species to copy to</param>
    /// <remarks>
    ///   <para>
    ///     TODO: it would be nice to unify this and the final apply properties to the edited species
    ///   </para>
    /// </remarks>
    private void CopyEditedPropertiesToSpecies(MicrobeSpecies target)
    {
        target.Colour = Colour;
        target.MembraneType = Membrane;
        target.MembraneRigidity = Rigidity;
        target.IsBacteria = true;

        target.Organelles.Clear();

        // TODO: if this is too slow to copy each organelle like this, we'll need to find a faster way to get the data
        // in, perhaps by sharing the entire Organelles object
        foreach (var entry in editedMicrobeOrganelles.Organelles)
        {
            if (entry.Definition == nucleus)
                target.IsBacteria = false;

            target.Organelles.AddFast(entry, hexTemporaryMemory, hexTemporaryMemory2);
        }
    }

    private void OnLightLevelButtonPressed(string option)
    {
        GUICommon.Instance.PlayButtonPressSound();

        var selection = (LightLevelOption)Enum.Parse(typeof(LightLevelOption), option);
        SetLightLevelOption(selection);
    }

    private void SetLightLevelOption(LightLevelOption selection)
    {
        selectedLightLevelOption = selection;
        ApplyLightLevelOption();
    }

    private void ApplyLightLevelOption()
    {
        calculateBalancesAsIfDay.Disabled = false;

        // Show selected light level
        switch (selectedLightLevelOption)
        {
            case LightLevelOption.Day:
            {
                dayButton.ButtonPressed = true;
                Editor.DayLightFraction = 1;

                calculateBalancesAsIfDay.ButtonPressed = true;
                calculateBalancesAsIfDay.Disabled = true;
                break;
            }

            case LightLevelOption.Night:
            {
                nightButton.ButtonPressed = true;
                Editor.DayLightFraction = 0;

                calculateBalancesAsIfDay.ButtonPressed = false;
                calculateBalancesAsIfDay.Disabled = true;
                break;
            }

            case LightLevelOption.Average:
            {
                averageLightButton.ButtonPressed = true;
                Editor.DayLightFraction = Editor.CurrentGame.GameWorld.LightCycle.AverageSunlight;

                calculateBalancesAsIfDay.ButtonPressed = false;
                break;
            }

            case LightLevelOption.Current:
            {
                currentLightButton.ButtonPressed = true;
                Editor.DayLightFraction = Editor.CurrentGame.GameWorld.LightCycle.DayLightFraction;
                break;
            }

            default:
                throw new Exception("Invalid light level option");
        }
    }

    private void SetSelectionMenuTab(string tab)
    {
        var selection = (SelectionMenuTab)Enum.Parse(typeof(SelectionMenuTab), tab);

        if (selection == selectedSelectionMenuTab)
            return;

        GUICommon.Instance.PlayButtonPressSound();

        selectedSelectionMenuTab = selection;
        ApplySelectionMenuTab();
    }

    private void ApplySelectionMenuTab()
    {
        // Hide all
        structureTab.Hide();
        appearanceTab.Hide();
        behaviourEditor.Hide();

        // Show selected
        switch (selectedSelectionMenuTab)
        {
            case SelectionMenuTab.Structure:
            {
                structureTab.Show();
                structureTabButton.ButtonPressed = true;
                MicrobePreviewMode = false;
                break;
            }

            case SelectionMenuTab.Membrane:
            {
                appearanceTab.Show();
                appearanceTabButton.ButtonPressed = true;
                MicrobePreviewMode = true;
                break;
            }

            case SelectionMenuTab.Behaviour:
            {
                behaviourEditor.Show();
                behaviourTabButton.ButtonPressed = true;
                MicrobePreviewMode = false;
                break;
            }

            default:
                throw new Exception("Invalid selection menu tab");
        }
    }

    private void UpdateAutoEvoPredictionTranslations()
    {
        if (autoEvoPredictionRunSuccessful == false)
        {
            totalEnergyLabel.Value = float.NaN;
            autoEvoPredictionFailedLabel.Show();
        }
        else
        {
            autoEvoPredictionFailedLabel.Hide();
        }

        var energyFormat = Localization.Translate("ENERGY_IN_PATCH_SHORT");

        if (!string.IsNullOrEmpty(bestPatchName))
        {
            var formatted = StringUtils.ThreeDigitFormat(bestPatchEnergyGathered);

            bestPatchLabel.Text =
                energyFormat.FormatSafe(Localization.Translate(bestPatchName), formatted);
        }
        else
        {
            bestPatchLabel.Text = Localization.Translate("N_A");
        }

        if (!string.IsNullOrEmpty(worstPatchName))
        {
            var formatted = StringUtils.ThreeDigitFormat(worstPatchEnergyGathered);

            worstPatchLabel.Text =
                energyFormat.FormatSafe(Localization.Translate(worstPatchName), formatted);
        }
        else
        {
            worstPatchLabel.Text = Localization.Translate("N_A");
        }
    }

    private void DummyKeepTranslation()
    {
        // This keeps this translation string existing if we ever still want to use worst and best population numbers
        Localization.Translate("POPULATION_IN_PATCH_SHORT");
    }

    private void OpenAutoEvoPredictionDetails()
    {
        GUICommon.Instance.PlayButtonPressSound();

        UpdateAutoEvoPredictionDetailsText();

        autoEvoPredictionExplanationPopup.PopupCenteredShrink();

        TutorialState?.SendEvent(TutorialEventType.MicrobeEditorAutoEvoPredictionOpened, EventArgs.Empty, this);
    }

    private void CloseAutoEvoPrediction()
    {
        GUICommon.Instance.PlayButtonPressSound();
        autoEvoPredictionExplanationPopup.Hide();
    }

    private void OnAutoEvoPredictionComplete(PendingAutoEvoPrediction run)
    {
        if (!run.AutoEvoRun.WasSuccessful)
        {
            GD.PrintErr("Failed to run auto-evo prediction for showing in the editor");
            autoEvoPredictionRunSuccessful = false;
            UpdateAutoEvoPredictionTranslations();
            return;
        }

        var results = run.AutoEvoRun.Results ??
            throw new Exception("Auto evo prediction has no results even though it succeeded");

        // Total population
        var newPopulation = results.GetGlobalPopulation(run.PlayerSpeciesNew);

        autoEvoPredictionRunSuccessful = true;

        // Gather energy details
        float totalEnergy = 0;
        Patch? bestPatch = null;
        float bestPatchEnergy = 0;
        Patch? worstPatch = null;
        float worstPatchEnergy = 0;

        foreach (var entry in results.GetPatchEnergyResults(run.PlayerSpeciesNew))
        {
            // Best
            if (bestPatch == null || bestPatchEnergy < entry.Value.TotalEnergyGathered)
            {
                bestPatchEnergy = entry.Value.TotalEnergyGathered;
                bestPatch = entry.Key;
            }

            if (worstPatch == null || worstPatchEnergy > entry.Value.TotalEnergyGathered)
            {
                worstPatchEnergy = entry.Value.TotalEnergyGathered;
                worstPatch = entry.Key;
            }

            totalEnergy += entry.Value.TotalEnergyGathered;
        }

        // Set the initial value to compare against the original species
        totalEnergyLabel.ResetInitialValue();

        if (PreviousPlayerGatheredEnergy != null)
        {
            totalEnergyLabel.Value = PreviousPlayerGatheredEnergy.Value;
            totalEnergyLabel.TooltipText =
                new LocalizedString("GATHERED_ENERGY_TOOLTIP", PreviousPlayerGatheredEnergy).ToString();
        }
        else
        {
            GD.PrintErr("Previously gathered energy is unknown, can't compare them (this will happen with " +
                "older saves)");
        }

        var formatted = StringUtils.ThreeDigitFormat(totalEnergy);

        totalEnergyLabel.SetMultipartValue($"{formatted} ({newPopulation})", totalEnergy);

        // Set best and worst patch displays
        worstPatchName = worstPatch?.Name.ToString();
        worstPatchEnergyGathered = worstPatchEnergy;

        if (worstPatch != null)
        {
            // For some reason in rare cases the population numbers cannot be found, using FirstOrDefault should ensure
            // here that missing population numbers get assigned 0
            worstPatchPopulation = results.GetPopulationInPatches(run.PlayerSpeciesNew)
                .FirstOrDefault(p => p.Key == worstPatch).Value;
        }

        bestPatchName = bestPatch?.Name.ToString();
        bestPatchEnergyGathered = bestPatchEnergy;

        if (bestPatch != null)
        {
            bestPatchPopulation = results.GetPopulationInPatches(run.PlayerSpeciesNew)
                .FirstOrDefault(p => p.Key == bestPatch).Value;
        }

        CreateAutoEvoPredictionDetailsText(results.GetPatchEnergyResults(run.PlayerSpeciesNew),
            run.PlayerSpeciesOriginal.FormattedNameBbCode);

        UpdateAutoEvoPredictionTranslations();

        if (autoEvoPredictionPanel.Visible)
        {
            UpdateAutoEvoPredictionDetailsText();
        }
    }

    private void CreateAutoEvoPredictionDetailsText(
        Dictionary<Patch, RunResults.SpeciesPatchEnergyResults> energyResults, string playerSpeciesName)
    {
        predictionDetailsText = new LocalizedStringBuilder(300);

        double Round(float value)
        {
            if (value > 0.0005f)
                return Math.Round(value, 3);

            // Small values can get tiny (and still be different from getting 0 energy due to fitness) so
            // this is here for that reason
            return Math.Round(value, 8);
        }

        // This loop shows all the patches the player species is in. Could perhaps just show the current one
        foreach (var energyResult in energyResults)
        {
            predictionDetailsText.Append(new LocalizedString("ENERGY_IN_PATCH_FOR",
                energyResult.Key.Name, playerSpeciesName));
            predictionDetailsText.Append('\n');

            predictionDetailsText.Append(new LocalizedString("ENERGY_SUMMARY_LINE",
                Round(energyResult.Value.TotalEnergyGathered), Round(energyResult.Value.IndividualCost),
                energyResult.Value.UnadjustedPopulation));

            predictionDetailsText.Append('\n');
            predictionDetailsText.Append('\n');

            predictionDetailsText.Append(new LocalizedString("ENERGY_SOURCES"));
            predictionDetailsText.Append('\n');

            foreach (var nicheInfo in energyResult.Value.PerNicheEnergy)
            {
                var data = nicheInfo.Value;
                predictionDetailsText.Append(new LocalizedString("FOOD_SOURCE_ENERGY_INFO", nicheInfo.Key,
                    Round(data.CurrentSpeciesEnergy), Round(data.CurrentSpeciesFitness),
                    Round(data.TotalAvailableEnergy),
                    Round(data.TotalFitness)));
                predictionDetailsText.Append('\n');
            }

            predictionDetailsText.Append('\n');
        }
    }

    private void UpdateAutoEvoPredictionDetailsText()
    {
        autoEvoPredictionExplanationLabel.ExtendedBbcode = predictionDetailsText != null ?
            predictionDetailsText.ToString() :
            Localization.Translate("NO_DATA_TO_SHOW");
    }

    private OrganelleDefinition GetOrganelleDefinition(string name)
    {
        return SimulationParameters.Instance.GetOrganelleType(name);
    }

    private void ApplySymmetryForCurrentOrganelle()
    {
        if (ActiveActionName == null)
            return;

        var organelle = GetOrganelleDefinition(ActiveActionName);
        componentBottomLeftButtons.SymmetryEnabled = !organelle.Unique;
    }

    private void ToggleProcessList()
    {
        processListWindow.Visible = !processListWindow.Visible;
    }

    private class PendingAutoEvoPrediction
    {
        public AutoEvoRun AutoEvoRun;
        public Species PlayerSpeciesOriginal;
        public Species PlayerSpeciesNew;

        public PendingAutoEvoPrediction(AutoEvoRun autoEvoRun, Species playerSpeciesOriginal, Species playerSpeciesNew)
        {
            AutoEvoRun = autoEvoRun;
            PlayerSpeciesOriginal = playerSpeciesOriginal;
            PlayerSpeciesNew = playerSpeciesNew;
        }

        public bool Finished => AutoEvoRun.Finished;
    }
}
