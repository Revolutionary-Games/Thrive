using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using AutoEvo;
using Godot;
using Newtonsoft.Json;

/// <summary>
///   The cell editor component combining the organelle and other editing logic with the GUI for it
/// </summary>
[SceneLoadedClass("res://src/microbe_stage/editor/CellEditorComponent.tscn")]
public partial class CellEditorComponent :
    HexEditorComponentBase<ICellEditorData, CombinedEditorAction, EditorAction, OrganelleTemplate>,
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
    public NodePath DigestionEfficiencyDetailsPath = null!;

    [Export]
    public NodePath GenerationLabelPath = null!;

    [Export]
    public NodePath AutoEvoPredictionPanelPath = null!;

    [Export]
    public NodePath TotalPopulationLabelPath = null!;

    [Export]
    public NodePath AutoEvoPredictionFailedLabelPath = null!;

    [Export]
    public NodePath WorstPatchLabelPath = null!;

    [Export]
    public NodePath BestPatchLabelPath = null!;

    [Export]
    public NodePath MembraneColorPickerPath = null!;

    [Export]
    public NodePath ATPBalanceLabelPath = null!;

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
    public NodePath CompoundBalancePath = null!;

    [Export]
    public NodePath AutoEvoPredictionExplanationPopupPath = null!;

    [Export]
    public NodePath AutoEvoPredictionExplanationLabelPath = null!;

    [Export]
    public NodePath OrganelleUpgradeGUIPath = null!;

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

    private TextureButton digestionEfficiencyDetails = null!;

    private Label generationLabel = null!;

    private CellStatsIndicator totalPopulationLabel = null!;
    private Label autoEvoPredictionFailedLabel = null!;
    private Label bestPatchLabel = null!;
    private Label worstPatchLabel = null!;

    private Control autoEvoPredictionPanel = null!;

    private Slider rigiditySlider = null!;
    private TweakedColourPicker membraneColorPicker = null!;

    private Label atpBalanceLabel = null!;
    private Label atpProductionLabel = null!;
    private Label atpConsumptionLabel = null!;
    private SegmentedBar atpProductionBar = null!;
    private SegmentedBar atpConsumptionBar = null!;

    private CustomConfirmationDialog negativeAtpPopup = null!;

    private OrganellePopupMenu organelleMenu = null!;
    private OrganelleUpgradeGUI organelleUpgradeGUI = null!;

    private CompoundBalanceDisplay compoundBalance = null!;

    private CustomDialog autoEvoPredictionExplanationPopup = null!;
    private CustomRichTextLabel autoEvoPredictionExplanationLabel = null!;

    private PackedScene organelleSelectionButtonScene = null!;

    private PackedScene microbeScene = null!;
#pragma warning restore CA2213

    private OrganelleDefinition protoplasm = null!;
    private OrganelleDefinition nucleus = null!;
    private OrganelleDefinition bindingAgent = null!;

    private Compound sunlight = null!;

    private EnergyBalanceInfo? energyBalanceInfo;

    private string? bestPatchName;

    private long bestPatchPopulation;

    private string? worstPatchName;

    private long worstPatchPopulation;

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

#pragma warning disable CA2213

    /// <summary>
    ///   We're taking advantage of the available membrane and organelle system already present in
    ///   the microbe class for the cell preview.
    /// </summary>
    private Microbe? previewMicrobe;
#pragma warning restore CA2213

    private MicrobeSpecies? previewMicrobeSpecies;

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
    private bool membraneOrganellePositionsAreDirty = true;

    private bool microbePreviewMode;

    /// <summary>
    ///   The light-level from before entering the editor or when moving to a patch with light from one without.
    /// </summary>
    [JsonProperty]
    private float? originalLightLevel;

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

            if (previewMicrobeSpecies != null)
            {
                previewMicrobeSpecies.MembraneRigidity = value;
                previewMicrobe!.ApplyMembraneWigglyness();
            }
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

            if (previewMicrobe?.Species != null)
            {
                previewMicrobe.Species.Colour = value;
                previewMicrobe.Membrane.Tint = value;
                previewMicrobe.ApplyPreviewOrganelleColours();
            }
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

    /// <summary>
    ///   If this is enabled the editor will show how the edited cell would look like in the environment with
    ///   parameters set in the editor. Editing hexes is disabled during this (with the exception of undo/redo).
    /// </summary>
    public bool MicrobePreviewMode
    {
        get => microbePreviewMode;
        set
        {
            microbePreviewMode = value;

            UpdateCellVisualization();

            if (previewMicrobe != null)
                previewMicrobe.Visible = value;

            placedHexes.ForEach(entry => entry.Visible = !MicrobePreviewMode);
            placedModels.ForEach(entry => entry.Visible = !MicrobePreviewMode);
        }
    }

    [JsonIgnore]
    public bool HasNucleus => PlacedUniqueOrganelles.Any(d => d == nucleus);

    [JsonIgnore]
    public override bool HasIslands => editedMicrobeOrganelles.GetIslandHexes().Count > 0;

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

            return false;
        }
    }

    [JsonIgnore]
    public bool NodeReferencesResolved { get; private set; }

    protected override bool ForceHideHover => MicrobePreviewMode;

    private float CostMultiplier =>
        (IsMulticellularEditor ? Constants.MULTICELLULAR_EDITOR_COST_FACTOR : 1.0f) *
        Editor.CurrentGame.GameWorld.WorldSettings.MPMultiplier;

    public static void UpdateOrganelleDisplayerTransform(SceneDisplayer organelleModel, OrganelleTemplate organelle)
    {
        organelleModel.Transform = new Transform(
            MathUtils.CreateRotationForOrganelle(1 * organelle.Orientation), organelle.OrganelleModelPosition);

        organelleModel.Scale = new Vector3(Constants.DEFAULT_HEX_SIZE, Constants.DEFAULT_HEX_SIZE,
            Constants.DEFAULT_HEX_SIZE);
    }

    /// <summary>
    ///   Updates the organelle model displayer to have the specified scene in it
    /// </summary>
    public static void UpdateOrganellePlaceHolderScene(SceneDisplayer organelleModel,
        string displayScene, OrganelleDefinition definition, int renderPriority)
    {
        organelleModel.Scene = displayScene;
        var material = organelleModel.GetMaterial(definition.DisplaySceneModelPath);
        if (material != null)
        {
            material.RenderPriority = renderPriority;
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

        protoplasm = SimulationParameters.Instance.GetOrganelleType("protoplasm");
        nucleus = SimulationParameters.Instance.GetOrganelleType("nucleus");
        bindingAgent = SimulationParameters.Instance.GetOrganelleType("bindingAgent");

        organelleSelectionButtonScene =
            GD.Load<PackedScene>("res://src/microbe_stage/editor/MicrobePartSelection.tscn");

        sunlight = SimulationParameters.Instance.GetCompound("sunlight");

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
        digestionEfficiencyDetails = GetNode<TextureButton>(DigestionEfficiencyDetailsPath);
        generationLabel = GetNode<Label>(GenerationLabelPath);
        totalPopulationLabel = GetNode<CellStatsIndicator>(TotalPopulationLabelPath);
        autoEvoPredictionFailedLabel = GetNode<Label>(AutoEvoPredictionFailedLabelPath);
        worstPatchLabel = GetNode<Label>(WorstPatchLabelPath);
        bestPatchLabel = GetNode<Label>(BestPatchLabelPath);

        autoEvoPredictionPanel = GetNode<Control>(AutoEvoPredictionPanelPath);

        rigiditySlider = GetNode<Slider>(RigiditySliderPath);
        membraneColorPicker = GetNode<TweakedColourPicker>(MembraneColorPickerPath);

        atpBalanceLabel = GetNode<Label>(ATPBalanceLabelPath);
        atpProductionLabel = GetNode<Label>(ATPProductionLabelPath);
        atpConsumptionLabel = GetNode<Label>(ATPConsumptionLabelPath);
        atpProductionBar = GetNode<SegmentedBar>(ATPProductionBarPath);
        atpConsumptionBar = GetNode<SegmentedBar>(ATPConsumptionBarPath);

        negativeAtpPopup = GetNode<CustomConfirmationDialog>(NegativeAtpPopupPath);
        organelleMenu = GetNode<OrganellePopupMenu>(OrganelleMenuPath);
        organelleUpgradeGUI = GetNode<OrganelleUpgradeGUI>(OrganelleUpgradeGUIPath);

        compoundBalance = GetNode<CompoundBalanceDisplay>(CompoundBalancePath);

        autoEvoPredictionExplanationPopup = GetNode<CustomDialog>(AutoEvoPredictionExplanationPopupPath);
        autoEvoPredictionExplanationLabel = GetNode<CustomRichTextLabel>(AutoEvoPredictionExplanationLabelPath);
    }

    public override void Init(ICellEditorData owningEditor, bool fresh)
    {
        base.Init(owningEditor, fresh);

        if (!IsMulticellularEditor)
        {
            behaviourEditor.Init(owningEditor, fresh);
        }

        var newLayout = new OrganelleLayout<OrganelleTemplate>(
            OnOrganelleAdded, OnOrganelleRemoved);

        UpdateOriginalLightLevel(Editor.CurrentPatch);

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
                newLayout.Add(editedMicrobeOrganelle);
            }

            editedMicrobeOrganelles = newLayout;

            if (Editor.EditedCellProperties != null)
            {
                UpdateGUIAfterLoadingSpecies(Editor.EditedBaseSpecies, Editor.EditedCellProperties);
                SetupPreviewMicrobe();
                UpdateArrow(false);
            }
            else
            {
                GD.Print("Loaded cell editor with no cell to edit set");
            }
        }

        // Send info to the GUI about the organelle effectiveness in the current patch
        CalculateOrganelleEffectivenessInCurrentPatch();

        UpdateCancelButtonVisibility();

        if (IsMulticellularEditor)
        {
            componentBottomLeftButtons.HandleRandomSpeciesName = false;
            componentBottomLeftButtons.UseSpeciesNameValidation = false;

            // TODO: implement random cell type name generator
            componentBottomLeftButtons.ShowRandomizeButton = false;

            componentBottomLeftButtons.SetNamePlaceholder(TranslationServer.Translate("CELL_TYPE_NAME"));

            autoEvoPredictionPanel.Visible = false;

            // In multicellular the body plan editor handles this
            behaviourTabButton.Visible = false;
            behaviourEditor.Visible = false;
        }

        UpdateMicrobePartSelections();

        // After the if multicellular check so the tooltip cost factors are correct
        // on changing editor types, as tooltip manager is persistent while the game is running
        UpdateMPCost();

        UpdateOrganelleUnlockTooltips();

        // Do this here as we know the editor and hence world settings have been initialised by now
        UpdateOrganelleLAWKSettings();

        topPanel.Visible = Editor.CurrentGame.GameWorld.WorldSettings.DayNightCycleEnabled &&
            Editor.CurrentPatch.GetCompoundAmount(sunlight, CompoundAmountType.Maximum) > 0.0f;
    }

    public override void _Process(float delta)
    {
        base._Process(delta);

        if (!Visible)
            return;

        var debugOverlay = DebugOverlays.Instance;

        if (debugOverlay.PerformanceMetricsVisible)
        {
            var roughCount = Editor.RootOfDynamicallySpawned.GetChildCount();
            debugOverlay.ReportEntities(roughCount, 0);
        }

        CheckRunningAutoEvoPrediction();

        if (organelleDataDirty)
        {
            OnOrganellesChanged();
            organelleDataDirty = false;
        }

        // Show the organelle that is about to be placed
        if (Editor.ShowHover && !MicrobePreviewMode)
        {
            GetMouseHex(out int q, out int r);

            OrganelleDefinition? shownOrganelle = null;

            var effectiveSymmetry = Symmetry;

            if (MovingPlacedHex == null && ActiveActionName != null)
            {
                // Can place stuff at all?
                isPlacementProbablyValid = IsValidPlacement(new OrganelleTemplate(
                    GetOrganelleDefinition(ActiveActionName), new Hex(q, r), placementRotation));

                shownOrganelle = SimulationParameters.Instance.GetOrganelleType(ActiveActionName);
            }
            else if (MovingPlacedHex != null)
            {
                isPlacementProbablyValid = IsMoveTargetValid(new Hex(q, r), placementRotation, MovingPlacedHex);
                shownOrganelle = MovingPlacedHex.Definition;

                if (!Settings.Instance.MoveOrganellesWithSymmetry)
                    effectiveSymmetry = HexEditorSymmetry.None;
            }

            if (shownOrganelle != null)
            {
                HashSet<(Hex Hex, int Orientation)> hoveredHexes = new();

                RunWithSymmetry(q, r,
                    (finalQ, finalR, rotation) =>
                    {
                        RenderHighlightedOrganelle(finalQ, finalR, rotation, shownOrganelle);
                        hoveredHexes.Add((new Hex(finalQ, finalR), rotation));
                    }, effectiveSymmetry);

                MouseHoverPositions = hoveredHexes.ToList();
            }
        }
    }

    public override void OnEditorSpeciesSetup(Species species)
    {
        base.OnEditorSpeciesSetup(species);

        // For multicellular the cell editor is initialized before a cell type to edit is selected so we skip
        // the logic here the first time this is called too early
        if (Editor.EditedCellProperties == null && IsMulticellularEditor)
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
        // that just marks things dirty and we update our stats on the next _Process call)
        Membrane = Editor.EditedCellProperties!.MembraneType;
        Rigidity = Editor.EditedCellProperties.MembraneRigidity;
        Colour = Editor.EditedCellProperties.Colour;

        if (!IsMulticellularEditor)
            behaviourEditor.OnEditorSpeciesSetup(species);

        // Get the species organelles to be edited. This also updates the placeholder hexes
        foreach (var organelle in Editor.EditedCellProperties.Organelles.Organelles)
        {
            editedMicrobeOrganelles.Add((OrganelleTemplate)organelle.Clone());
        }

        newName = Editor.EditedCellProperties.FormattedName;

        UpdateGUIAfterLoadingSpecies(Editor.EditedBaseSpecies, Editor.EditedCellProperties);

        // Setup the display cell
        SetupPreviewMicrobe();

        UpdateArrow(false);
    }

    public override void OnFinishEditing()
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
            editedProperties.Organelles.Add(organelleToAdd);
        }

        editedProperties.RepositionToOrigin();
        editedProperties.CalculateRotationSpeed();

        // Update bacteria status
        editedProperties.IsBacteria = !HasNucleus;

        editedProperties.UpdateNameIfValid(newName);

        if (!IsMulticellularEditor)
        {
            editedSpecies.UpdateInitialCompounds();

            GD.Print("MicrobeEditor: updated organelles for species: ", editedSpecies.FormattedName);

            behaviourEditor.OnFinishEditing();
        }
        else
        {
            GD.Print("MicrobeEditor: updated organelles for cell: ", editedProperties.FormattedName);
        }

        // Update membrane
        editedProperties.MembraneType = Membrane;
        editedProperties.Colour = Colour;
        editedProperties.MembraneRigidity = Rigidity;
    }

    public override void SetEditorWorldTabSpecificObjectVisibility(bool shown)
    {
        base.SetEditorWorldTabSpecificObjectVisibility(shown && !MicrobePreviewMode);

        if (previewMicrobe != null)
        {
            previewMicrobe.Visible = shown && MicrobePreviewMode;
        }
    }

    public override bool CanFinishEditing(IEnumerable<EditorUserOverride> userOverrides)
    {
        var editorUserOverrides = userOverrides.ToList();
        if (!base.CanFinishEditing(editorUserOverrides))
            return false;

        if (editorUserOverrides.Contains(EditorUserOverride.NotProducingEnoughATP))
            return true;

        // Show warning popup if trying to exit with negative atp production
        // Not shown in multicellular as the popup happens in kind of a weird place
        if (!IsMulticellularEditor && IsNegativeAtpProduction())
        {
            negativeAtpPopup.PopupCenteredShrink();
            return false;
        }

        return true;
    }

    /// <summary>
    ///   Report that the current patch used in the editor has changed
    /// </summary>
    /// <param name="patch">The patch that is set</param>
    public void OnCurrentPatchUpdated(Patch patch)
    {
        UpdateOriginalLightLevel(patch);
        CalculateOrganelleEffectivenessInCurrentPatch();
        UpdatePatchDependentBalanceData();
    }

    public void UpdatePatchDependentBalanceData()
    {
        // Skip if not opened in the multicellular editor
        if (IsMulticellularEditor && editedMicrobeOrganelles.Organelles.Count < 1)
            return;

        topPanel.Visible = Editor.CurrentGame.GameWorld.WorldSettings.DayNightCycleEnabled &&
            Editor.CurrentPatch.GetCompoundAmount("sunlight", CompoundAmountType.Maximum) > 0.0f;

        // Calculate and send energy balance to the GUI
        CalculateEnergyBalanceWithOrganellesAndMembraneType(editedMicrobeOrganelles.Organelles, Membrane);

        CalculateCompoundBalanceInPatch(editedMicrobeOrganelles.Organelles);
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
            oldEditedMicrobeOrganelles.Add(organelle);
        }

        var data = new NewMicrobeActionData(oldEditedMicrobeOrganelles, oldMembrane, Rigidity, Colour,
            behaviourEditor.Behaviour ?? throw new Exception("Behaviour not initialized"));

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

        if (MovingPlacedHex != null)
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
    [RunOnKeyDown("e_secondary")]
    public bool ShowOrganelleOptions()
    {
        // Need to prevent this from running when not visible to not conflict in an editor with multiple tabs
        if (MicrobePreviewMode || !Visible)
            return false;

        // Can't open organelle popup menu while moving something
        if (MovingPlacedHex != null)
        {
            Editor.OnActionBlockedWhileMoving();
            return true;
        }

        GetMouseHex(out int q, out int r);

        // This is a list to preserve order, Distinct is used later to ensure no duplicate organelles are added
        var organelles = new List<OrganelleTemplate>();

        RunWithSymmetry(q, r, (symmetryQ, symmetryR, _) =>
        {
            var organelle = editedMicrobeOrganelles.GetElementAt(new Hex(symmetryQ, symmetryR));

            if (organelle != null)
                organelles.Add(organelle);
        });

        if (organelles.Count < 1)
            return true;

        ShowOrganelleMenu(organelles.Distinct());
        return true;
    }

    public float CalculateSpeed()
    {
        return MicrobeInternalCalculations.CalculateSpeed(editedMicrobeOrganelles, Membrane, Rigidity);
    }

    public float CalculateRotationSpeed()
    {
        return MicrobeInternalCalculations.CalculateRotationSpeed(editedMicrobeOrganelles);
    }

    public float CalculateHitpoints()
    {
        var maxHitpoints = Membrane.Hitpoints +
            (Rigidity * Constants.MEMBRANE_RIGIDITY_HITPOINTS_MODIFIER);

        return maxHitpoints;
    }

    public float CalculateStorage()
    {
        return MicrobeInternalCalculations.CalculateCapacity(editedMicrobeOrganelles);
    }

    public float CalculateTotalDigestionSpeed()
    {
        return MicrobeInternalCalculations.CalculateTotalDigestionSpeed(editedMicrobeOrganelles);
    }

    public Dictionary<Enzyme, float> CalculateDigestionEfficiencies()
    {
        return MicrobeInternalCalculations.CalculateDigestionEfficiencies(editedMicrobeOrganelles);
    }

    public override void OnLightLevelChanged(float lightLevel)
    {
        var maxLightLevel = Editor.CurrentPatch.Biome.Compounds[sunlight].Ambient;
        var minLightLevel = Editor.CurrentPatch.Biome.MinimumCompounds[sunlight].Ambient;
        var templateMaxLightLevel = Editor.CurrentPatch.Biome.Compounds[sunlight].Ambient;

        // Currently, patches whose templates have zero sunlight can be given non-zero sunlight as an instance. But
        // nighttime shaders haven't been created for these patches (specifically the sea floor) so for now we can't
        // reduce light level in such patches without things looking bad. So we have to check the template light level
        // is non-zero too.
        if (maxLightLevel > 0.0f && templateMaxLightLevel > 0.0f)
        {
            // Normalise by maximum light level in the patch
            camera!.LightLevel = lightLevel / maxLightLevel;

            foreach (var patch in Editor.CurrentGame.GameWorld.Map.Patches.Values)
            {
                var targetMaxLightLevel = patch.Biome.MaximumCompounds[sunlight].Ambient;
                var targetMinLightLevel = patch.Biome.MinimumCompounds[sunlight].Ambient;

                // Figure out the daylight in all patches relative to the player's current patch
                var relativeLightLevel = (lightLevel - minLightLevel) / (maxLightLevel - minLightLevel) *
                    (targetMaxLightLevel - targetMinLightLevel) + targetMinLightLevel;

                var lightLevelAmount = new BiomeCompoundProperties { Ambient = relativeLightLevel };

                patch.Biome.CurrentCompoundAmounts[sunlight] = lightLevelAmount;
            }
        }
        else
        {
            // Don't change lighting for patches without day/night effects
            camera!.LightLevel = 1.0f;
        }

        // TODO: isn't this entirely logically wrong? See the comment in PatchManager about needing to set average
        // light levels on editor entry. This seems wrong because the average light amount is *not* the current light
        // level, meaning that auto-evo prediction would be incorrect (if these numbers were used there, but aren't
        // currently, see the documentation on previewBiomeConditions)
        // // Need to set average to be the same as ambient so Auto-Evo updates correctly
        // previewBiomeConditions.AverageCompounds[sunlight] = lightLevelAmount;

        CalculateOrganelleEffectivenessInCurrentPatch();
        UpdatePatchDependentBalanceData();
    }

    public bool ApplyOrganelleUpgrade(OrganelleUpgradeActionData actionData)
    {
        actionData.CostMultiplier = CostMultiplier;

        return EnqueueAction(new CombinedEditorAction(
            new SingleEditorAction<OrganelleUpgradeActionData>(DoOrganelleUpgradeAction, UndoOrganelleUpgradeAction,
                actionData)));
    }

    protected override int CalculateCurrentActionCost()
    {
        if (string.IsNullOrEmpty(ActiveActionName) || !Editor.ShowHover)
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

    protected override void LoadScenes()
    {
        base.LoadScenes();

        microbeScene = GD.Load<PackedScene>("res://src/microbe_stage/Microbe.tscn");
    }

    protected override void PerformActiveAction()
    {
        if (AddOrganelle(ActiveActionName!))
        {
            // Only trigger tutorial if an organelle was really placed
            TutorialState?.SendEvent(TutorialEventType.MicrobeEditorOrganellePlaced, EventArgs.Empty, this);
        }
    }

    protected override void PerformMove(int q, int r)
    {
        if (!MoveOrganelle(MovingPlacedHex!, MovingPlacedHex!.Position, new Hex(q, r), MovingPlacedHex.Orientation,
                placementRotation))
        {
            Editor.OnInvalidAction();
        }
    }

    protected override void OnMoveWillSucceed()
    {
        base.OnMoveWillSucceed();

        // Update rigidity slider in case it was disabled
        // TODO: could come up with a bit nicer design here
        int intRigidity = (int)Math.Round(Rigidity * Constants.MEMBRANE_RIGIDITY_SLIDER_TO_VALUE_RATIO);
        UpdateRigiditySlider(intRigidity);
    }

    protected override bool IsMoveTargetValid(Hex position, int rotation, OrganelleTemplate organelle)
    {
        return editedMicrobeOrganelles.CanPlace(organelle.Definition, position, rotation, false);
    }

    protected override void OnCurrentActionCanceled()
    {
        editedMicrobeOrganelles.Add(MovingPlacedHex!);
        MovingPlacedHex = null;
        base.OnCurrentActionCanceled();
    }

    protected override void OnMoveActionStarted()
    {
        editedMicrobeOrganelles.Remove(MovingPlacedHex!);
    }

    protected override OrganelleTemplate? GetHexAt(Hex position)
    {
        return editedMicrobeOrganelles.GetElementAt(position);
    }

    protected override EditorAction? TryCreateRemoveHexAtAction(Hex location, ref int alreadyDeleted)
    {
        var organelleHere = editedMicrobeOrganelles.GetElementAt(location);
        if (organelleHere == null)
            return null;

        // Dont allow deletion of nucleus or the last organelle
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
                highestPointInMiddleRows = Mathf.Min(highestPointInMiddleRows, cartesian.z);
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
                DigestionEfficiencyDetailsPath.Dispose();
                GenerationLabelPath.Dispose();
                AutoEvoPredictionPanelPath.Dispose();
                TotalPopulationLabelPath.Dispose();
                AutoEvoPredictionFailedLabelPath.Dispose();
                WorstPatchLabelPath.Dispose();
                BestPatchLabelPath.Dispose();
                MembraneColorPickerPath.Dispose();
                ATPBalanceLabelPath.Dispose();
                ATPProductionLabelPath.Dispose();
                ATPConsumptionLabelPath.Dispose();
                ATPProductionBarPath.Dispose();
                ATPConsumptionBarPath.Dispose();
                RigiditySliderPath.Dispose();
                NegativeAtpPopupPath.Dispose();
                OrganelleMenuPath.Dispose();
                CompoundBalancePath.Dispose();
                AutoEvoPredictionExplanationPopupPath.Dispose();
                AutoEvoPredictionExplanationLabelPath.Dispose();
                OrganelleUpgradeGUIPath.Dispose();
            }
        }

        base.Dispose(disposing);
    }

    private void SetupPreviewMicrobe()
    {
        if (previewMicrobe != null)
        {
            GD.Print("Preview microbe already setup");
            previewMicrobe.Visible = MicrobePreviewMode;
            return;
        }

        previewMicrobe = (Microbe)microbeScene.Instance();
        previewMicrobe.IsForPreviewOnly = true;
        Editor.RootOfDynamicallySpawned.AddChild(previewMicrobe);
        previewMicrobeSpecies = new MicrobeSpecies(Editor.EditedBaseSpecies,
            Editor.EditedCellProperties ??
            throw new InvalidOperationException("can't setup preview before cell properties are known"));
        previewMicrobe.ApplySpecies(previewMicrobeSpecies);

        // Set its initial visibility
        previewMicrobe.Visible = MicrobePreviewMode;
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
                TranslationServer.Translate(
                    "NUCLEUS_DELETE_OPTION_DISABLED_TOOLTIP") :
                TranslationServer.Translate(
                    "LAST_ORGANELLE_DELETE_OPTION_DISABLED_TOOLTIP");
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
            if (oldOrganelle != default && organelle != null)
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
                    GotReplaced = organelle.Definition.InternalName == "cytoplasm",
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
                        .Select(h => editedMicrobeOrganelles.GetElementAt(hex + h)).WhereNotNull().ToList();

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
                     .Where(o => o.Definition.InternalName != "cytoplasm")
                     .SelectMany(o => o.RotatedHexes.Select(hex => hex + o.Position)))
        {
            var existingOrganelle = editedMicrobeOrganelles.GetElementAt(templateHex);

            if (existingOrganelle != null && existingOrganelle.Definition.InternalName == "cytoplasm")
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
        if (!Editor.Ready)
        {
            GD.PrintErr("Can't start auto-evo prediction before editor is ready");
            return;
        }

        // Note that in rare cases the auto-evo run doesn't manage to stop before we edit the cached species object
        // which may cause occasional background task errors
        CancelPreviousAutoEvoPrediction();

        cachedAutoEvoPredictionSpecies ??= new MicrobeSpecies(Editor.EditedBaseSpecies,
            Editor.EditedCellProperties ??
            throw new InvalidOperationException("can't start auto-evo prediction without current cell properties"));

        CopyEditedPropertiesToSpecies(cachedAutoEvoPredictionSpecies);

        var run = new EditorAutoEvoRun(Editor.CurrentGame.GameWorld, Editor.EditedBaseSpecies,
            cachedAutoEvoPredictionSpecies);
        run.Start();

        UpdateAutoEvoPrediction(run, Editor.EditedBaseSpecies, cachedAutoEvoPredictionSpecies);
    }

    /// <summary>
    ///   Stores the actual patch light level (outside of the editor). This should only be called right
    ///   after entering the editor or when moving to a patch with light from one without.
    /// </summary>
    private void UpdateOriginalLightLevel(Patch patch)
    {
        // Only in patch with sunlight
        if (patch.Biome.MaximumCompounds[sunlight].Ambient > 0)
            originalLightLevel ??= patch.Biome.CurrentCompoundAmounts[sunlight].Ambient;
    }

    /// <summary>
    ///   Calculates the energy balance for a cell with the given organelles
    /// </summary>
    private void CalculateEnergyBalanceWithOrganellesAndMembraneType(IReadOnlyCollection<OrganelleTemplate> organelles,
        MembraneType membrane, BiomeConditions? biome = null)
    {
        biome ??= Editor.CurrentPatch.Biome;

        UpdateEnergyBalance(ProcessSystem.ComputeEnergyBalance(organelles, biome, membrane, true,
            Editor.CurrentGame.GameWorld.WorldSettings, CompoundAmountType.Current));
    }

    private void CalculateCompoundBalanceInPatch(IReadOnlyCollection<OrganelleTemplate> organelles,
        BiomeConditions? biome = null)
    {
        biome ??= Editor.CurrentPatch.Biome;

        var result = ProcessSystem.ComputeCompoundBalance(organelles, biome, CompoundAmountType.Current);

        UpdateCompoundBalances(result);
    }

    /// <summary>
    ///   If not hovering over an organelle, render the to-be-placed organelle
    /// </summary>
    private void RenderHighlightedOrganelle(int q, int r, int rotation, OrganelleDefinition shownOrganelle)
    {
        if (MovingPlacedHex == null && ActiveActionName == null)
            return;

        RenderHoveredHex(q, r, shownOrganelle.GetRotatedHexes(rotation), isPlacementProbablyValid,
            out bool hadDuplicate);

        bool showModel = !hadDuplicate;

        // Model
        if (!string.IsNullOrEmpty(shownOrganelle.DisplayScene) && showModel)
        {
            var cartesianPosition = Hex.AxialToCartesian(new Hex(q, r));

            var organelleModel = hoverModels[usedHoverModel++];

            organelleModel.Transform = new Transform(
                MathUtils.CreateRotationForOrganelle(rotation),
                cartesianPosition + shownOrganelle.CalculateModelOffset());

            organelleModel.Scale = new Vector3(Constants.DEFAULT_HEX_SIZE, Constants.DEFAULT_HEX_SIZE,
                Constants.DEFAULT_HEX_SIZE);

            organelleModel.Visible = true;

            UpdateOrganellePlaceHolderScene(organelleModel, shownOrganelle.DisplayScene!,
                shownOrganelle, Hex.GetRenderPriority(new Hex(q, r)));
        }
    }

    /// <summary>
    ///   Updates the membrane and organelle placement of the preview cell.
    /// </summary>
    private void UpdateCellVisualization()
    {
        if (previewMicrobe == null)
            return;

        // Don't redo the preview cell when not in the preview mode to avoid unnecessary lags
        if (!MicrobePreviewMode || !membraneOrganellePositionsAreDirty)
            return;

        CopyEditedPropertiesToSpecies(previewMicrobeSpecies!);

        // Intentionally force it to not be bacteria to show it at full size
        previewMicrobeSpecies!.IsBacteria = false;

        // This is now just for applying changes in the species to the preview cell
        previewMicrobe.ApplySpecies(previewMicrobeSpecies);

        membraneOrganellePositionsAreDirty = false;
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
            });

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

        if (!IsValidPlacement(organelle))
        {
            // Play Sound
            Editor.OnInvalidAction();
            return null;
        }

        return CreateAddOrganelleAction(organelle);
    }

    private bool IsValidPlacement(OrganelleTemplate organelle)
    {
        bool notPlacingCytoplasm = organelle.Definition.InternalName != "cytoplasm";

        return editedMicrobeOrganelles.CanPlaceAndIsTouching(
            organelle,
            notPlacingCytoplasm,
            notPlacingCytoplasm);
    }

    private CombinedEditorAction? CreateAddOrganelleAction(OrganelleTemplate organelle)
    {
        // 1 - you put a unique organelle (means only one instance allowed) but you already have it
        // 2 - you put an organelle that requires nucleus but you don't have one
        if ((organelle.Definition.Unique && HasOrganelle(organelle.Definition)) ||
            (organelle.Definition.RequiresNucleus && !HasNucleus))
            return null;

        if (organelle.Definition.Unique)
            DeselectOrganelleToPlace();

        var replacedCytoplasmActions =
            GetReplacedCytoplasmRemoveAction(new[] { organelle }).Cast<EditorAction>().ToList();

        var action = new SingleEditorAction<OrganellePlacementActionData>(
            DoOrganellePlaceAction, UndoOrganellePlaceAction,
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
    private bool MoveOrganelle(OrganelleTemplate organelle, Hex oldLocation, Hex newLocation, int oldRotation,
        int newRotation)
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
        UpdateStorage(CalculateStorage());
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
        UpdateOrganelleUnlockTooltips();

        UpdatePatchDependentBalanceData();

        // Send to gui current status of cell
        UpdateSize(MicrobeHexSize);
        UpdateStats();

        UpdateCellVisualization();

        StartAutoEvoPrediction();

        UpdateFinishButtonWarningVisibility();
    }

    /// <summary>
    ///   This destroys and creates again entities to represent all the currently placed organelles. Call this whenever
    ///   editedMicrobeOrganelles is changed.
    /// </summary>
    private void UpdateAlreadyPlacedVisuals()
    {
        var islands = editedMicrobeOrganelles.GetIslandHexes();

        // TODO: The code below is partly duplicate to CellHexPhotoBuilder. If this is changed that needs changes too.
        // Build the entities to show the current microbe
        UpdateAlreadyPlacedHexes(
            editedMicrobeOrganelles.Select(o => (o.Position, o.RotatedHexes, Editor.HexPlacedThisSession(o))), islands,
            microbePreviewMode);

        int nextFreeOrganelle = 0;

        foreach (var organelle in editedMicrobeOrganelles)
        {
            // Hexes are handled by UpdateAlreadyPlacedHexes

            // Model of the organelle
            if (organelle.Definition.DisplayScene != null)
            {
                if (nextFreeOrganelle >= placedModels.Count)
                {
                    // New organelle model needed
                    placedModels.Add(CreatePreviewModelHolder());
                }

                var organelleModel = placedModels[nextFreeOrganelle++];

                UpdateOrganelleDisplayerTransform(organelleModel, organelle);

                organelleModel.Visible = !MicrobePreviewMode;

                UpdateOrganellePlaceHolderScene(organelleModel,
                    organelle.Definition.DisplayScene, organelle.Definition, Hex.GetRenderPriority(organelle.Position));
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

        // Once an organelle move has begun, the button visibility should be updated so it becomes visible
        UpdateCancelButtonVisibility();
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

        organelleUpgradeGUI.OpenForOrganelle(targetOrganelle, upgradeGUI ?? string.Empty, this);
    }

    /// <summary>
    ///   Lock / unlock a single organelle that need a nucleus
    /// </summary>
    private void UpdatePartAvailability(List<OrganelleDefinition> placedUniqueOrganelleNames,
        OrganelleDefinition organelle)
    {
        var item = placeablePartSelectionElements[organelle];

        if (organelle.Unique && placedUniqueOrganelleNames.Contains(organelle))
        {
            item.Locked = true;
        }
        else if (organelle.RequiresNucleus)
        {
            var hasNucleus = placedUniqueOrganelleNames.Contains(nucleus);
            item.Locked = !hasNucleus;
        }
        else
        {
            item.Locked = false;
        }
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

            var control = (MicrobePartSelection)organelleSelectionButtonScene.Instance();
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

            control.Connect(nameof(MicrobePartSelection.OnPartSelected), this, nameof(OnOrganelleToPlaceSelected));
        }

        foreach (var membraneType in simulationParameters.GetAllMembranes().OrderBy(m => m.EditorButtonOrder))
        {
            var control = (MicrobePartSelection)organelleSelectionButtonScene.Instance();
            control.PartIcon = membraneType.LoadedIcon;
            control.PartName = membraneType.UntranslatedName;
            control.SelectionGroup = membraneButtonGroup;
            control.MPCost = membraneType.EditorCost;
            control.Name = membraneType.InternalName;

            control.RegisterToolTipForControl(membraneType.InternalName, "membraneSelection");

            membraneTypeSelection.AddItem(control);

            membraneSelectionElements.Add(membraneType, control);

            control.Connect(nameof(MicrobePartSelection.OnPartSelected), this, nameof(OnMembraneSelected));
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
            organelle => organelle.Name.ToLower(CultureInfo.CurrentCulture).Contains(input)).ToList();

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

            target.Organelles.Add(entry);
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
        // Show selected light level
        switch (selectedLightLevelOption)
        {
            case LightLevelOption.Day:
            {
                dayButton.Pressed = true;
                Editor.LightLevel = Editor.CurrentPatch.Biome.MaximumCompounds[sunlight].Ambient;
                break;
            }

            case LightLevelOption.Night:
            {
                nightButton.Pressed = true;
                Editor.LightLevel = Editor.CurrentPatch.Biome.MinimumCompounds[sunlight].Ambient;
                break;
            }

            case LightLevelOption.Average:
            {
                averageLightButton.Pressed = true;
                Editor.LightLevel = Editor.CurrentPatch.Biome.AverageCompounds[sunlight].Ambient;
                break;
            }

            case LightLevelOption.Current:
            {
                currentLightButton.Pressed = true;
                Editor.LightLevel = originalLightLevel.GetValueOrDefault();
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
                structureTabButton.Pressed = true;
                MicrobePreviewMode = false;
                break;
            }

            case SelectionMenuTab.Membrane:
            {
                appearanceTab.Show();
                appearanceTabButton.Pressed = true;
                MicrobePreviewMode = true;
                break;
            }

            case SelectionMenuTab.Behaviour:
            {
                behaviourEditor.Show();
                behaviourTabButton.Pressed = true;
                MicrobePreviewMode = false;
                break;
            }

            default:
                throw new Exception("Invalid selection menu tab");
        }
    }

    private void UpdateAutoEvoPredictionTranslations()
    {
        if (autoEvoPredictionRunSuccessful is false)
        {
            totalPopulationLabel.Value = float.NaN;
            autoEvoPredictionFailedLabel.Show();
        }
        else
        {
            autoEvoPredictionFailedLabel.Hide();
        }

        var populationFormat = TranslationServer.Translate("POPULATION_IN_PATCH_SHORT");

        if (!string.IsNullOrEmpty(bestPatchName))
        {
            bestPatchLabel.Text =
                populationFormat.FormatSafe(TranslationServer.Translate(bestPatchName), bestPatchPopulation);
        }
        else
        {
            bestPatchLabel.Text = TranslationServer.Translate("N_A");
        }

        if (!string.IsNullOrEmpty(worstPatchName))
        {
            worstPatchLabel.Text =
                populationFormat.FormatSafe(TranslationServer.Translate(worstPatchName), worstPatchPopulation);
        }
        else
        {
            worstPatchLabel.Text = TranslationServer.Translate("N_A");
        }
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

        // Set the initial value
        totalPopulationLabel.ResetInitialValue();
        totalPopulationLabel.Value = run.PlayerSpeciesOriginal.Population;

        totalPopulationLabel.Value = newPopulation;

        var sorted = results.GetPopulationInPatches(run.PlayerSpeciesNew).OrderByDescending(p => p.Value).ToList();

        // Best
        if (sorted.Count > 0)
        {
            var patch = sorted[0];
            bestPatchName = patch.Key.Name.ToString();
            bestPatchPopulation = patch.Value;
        }
        else
        {
            bestPatchName = null;
        }

        // And worst patch
        if (sorted.Count > 1)
        {
            var patch = sorted[sorted.Count - 1];
            worstPatchName = patch.Key.Name.ToString();
            worstPatchPopulation = patch.Value;
        }
        else
        {
            worstPatchName = null;
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

            // Small values can get really small (and still be different from getting 0 energy due to fitness) so
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
            TranslationServer.Translate("NO_DATA_TO_SHOW");
    }

    private OrganelleDefinition GetOrganelleDefinition(string name)
    {
        return SimulationParameters.Instance.GetOrganelleType(name);
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
