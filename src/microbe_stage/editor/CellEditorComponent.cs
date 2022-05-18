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
    HexEditorComponentBase<ICellEditorData, CombinedMicrobeEditorAction, CellEditorAction, OrganelleTemplate>,
    IGodotEarlyNodeResolve
{
    [Export]
    public bool IsMulticellularEditor;

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
    public NodePath HpLabelPath = null!;

    [Export]
    public NodePath StorageLabelPath = null!;

    [Export]
    public NodePath GenerationLabelPath = null!;

    [Export]
    public NodePath AutoEvoPredictionPanelPath = null!;

    [Export]
    public NodePath TotalPopulationLabelPath = null!;

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
    public NodePath SpeedIndicatorPath = null!;

    [Export]
    public NodePath HpIndicatorPath = null!;

    [Export]
    public NodePath StorageIndicatorPath = null!;

    [Export]
    public NodePath SizeIndicatorPath = null!;

    [Export]
    public NodePath TotalPopulationIndicatorPath = null!;

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

    private Label sizeLabel = null!;
    private Label speedLabel = null!;
    private Label hpLabel = null!;
    private Label storageLabel = null!;
    private Label generationLabel = null!;
    private Label totalPopulationLabel = null!;
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

    private TextureRect speedIndicator = null!;
    private TextureRect hpIndicator = null!;
    private TextureRect storageIndicator = null!;
    private TextureRect sizeIndicator = null!;
    private TextureRect totalPopulationIndicator = null!;

    private CustomConfirmationDialog negativeAtpPopup = null!;

    private OrganellePopupMenu organelleMenu = null!;
    private OrganelleUpgradeGUI organelleUpgradeGUI = null!;

    private CompoundBalanceDisplay compoundBalance = null!;

    private CustomDialog autoEvoPredictionExplanationPopup = null!;
    private Label autoEvoPredictionExplanationLabel = null!;

    private Texture increaseIcon = null!;
    private Texture decreaseIcon = null!;

    private PackedScene organelleSelectionButtonScene = null!;

    private OrganelleDefinition protoplasm = null!;
    private OrganelleDefinition nucleus = null!;
    private OrganelleDefinition bindingAgent = null!;

    /// <summary>
    ///   Controls MP discounts (for multicellular)
    /// </summary>
    private float editorCostFactor = 1f;

    private EnergyBalanceInfo? energyBalanceInfo;

    [JsonProperty]
    private float initialCellSpeed;

    [JsonProperty]
    private int initialCellSize;

    [JsonProperty]
    private float initialCellHp;

    [JsonProperty]
    private float initialCellStorage;

    private string? bestPatchName;

    private long bestPatchPopulation;

    private string? worstPatchName;

    private long worstPatchPopulation;

    private Dictionary<OrganelleDefinition, MicrobePartSelection> placeablePartSelectionElements = new();

    private Dictionary<MembraneType, MicrobePartSelection> membraneSelectionElements = new();

    [JsonProperty]
    private SelectionMenuTab selectedSelectionMenuTab = SelectionMenuTab.Structure;

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
    ///   We're taking advantage of the available membrane and organelle system already present in
    ///   the microbe class for the cell preview.
    /// </summary>
    private Microbe? previewMicrobe;

    private MicrobeSpecies? previewMicrobeSpecies;

    private PackedScene microbeScene = null!;

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

    public enum SelectionMenuTab
    {
        Structure,
        Membrane,
        Behaviour,
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
    public bool NodeReferencesResolved { get; private set; }

    /// <summary>
    ///   If true an editor action is active and can be cancelled. Currently only checks for organelle move.
    /// </summary>
    [JsonIgnore]
    public bool CanCancelAction => CanCancelMove;

    protected override bool ForceHideHover => MicrobePreviewMode;

    public override void _Ready()
    {
        base._Ready();

        ResolveNodeReferences();

        // GUI setup part

        // Hidden in the Godot editor to make selecting other things easier
        organelleUpgradeGUI.Visible = true;

        atpProductionBar.SelectedType = SegmentedBar.Type.ATP;
        atpProductionBar.IsProduction = true;
        atpConsumptionBar.SelectedType = SegmentedBar.Type.ATP;

        protoplasm = SimulationParameters.Instance.GetOrganelleType("protoplasm");
        nucleus = SimulationParameters.Instance.GetOrganelleType("nucleus");
        bindingAgent = SimulationParameters.Instance.GetOrganelleType("bindingAgent");
        questionIcon = GD.Load<Texture>("res://assets/textures/gui/bevel/helpButton.png");
        increaseIcon = GD.Load<Texture>("res://assets/textures/gui/bevel/increase.png");
        decreaseIcon = GD.Load<Texture>("res://assets/textures/gui/bevel/decrease.png");

        organelleSelectionButtonScene =
            GD.Load<PackedScene>("res://src/microbe_stage/editor/MicrobePartSelection.tscn");

        SetupMicrobePartSelections();
        UpdateMicrobePartSelections();

        ApplySelectionMenuTab();
        RegisterTooltips();
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
        CalculateOrganelleEffectivenessInPatch(Editor.CurrentPatch);

        UpdateRigiditySliderState(Editor.MutationPoints);

        UpdateCancelButtonVisibility();

        if (IsMulticellularEditor)
        {
            editorCostFactor = Constants.MULTICELLULAR_EDITOR_COST_FACTOR;
            organelleMenu.EditorCostFactor = editorCostFactor;
            UpdateMicrobePartSelections();

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

        // After the if multicellular check so the tooltip cost factors are correct
        // on changing editor types, as tooltip manager is persistent while the game is running
        UpdateTooltipMPCostFactors();
    }

    public override void ResolveNodeReferences()
    {
        if (NodeReferencesResolved)
            return;

        base.ResolveNodeReferences();

        NodeReferencesResolved = true;

        structureTab = GetNode<PanelContainer>(StructureTabPath);
        structureTabButton = GetNode<Button>(StructureTabButtonPath);

        appearanceTab = GetNode<PanelContainer>(AppearanceTabPath);
        appearanceTabButton = GetNode<Button>(AppearanceTabButtonPath);

        behaviourTabButton = GetNode<Button>(BehaviourTabButtonPath);
        behaviourEditor = GetNode<BehaviourEditorSubComponent>(BehaviourTabPath);

        partsSelectionContainer = GetNode<VBoxContainer>(PartsSelectionContainerPath);
        membraneTypeSelection = GetNode<CollapsibleList>(MembraneTypeSelectionPath);

        sizeLabel = GetNode<Label>(SizeLabelPath);
        speedLabel = GetNode<Label>(SpeedLabelPath);
        hpLabel = GetNode<Label>(HpLabelPath);
        storageLabel = GetNode<Label>(StorageLabelPath);
        generationLabel = GetNode<Label>(GenerationLabelPath);
        totalPopulationLabel = GetNode<Label>(TotalPopulationLabelPath);
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

        speedIndicator = GetNode<TextureRect>(SpeedIndicatorPath);
        hpIndicator = GetNode<TextureRect>(HpIndicatorPath);
        storageIndicator = GetNode<TextureRect>(StorageIndicatorPath);
        sizeIndicator = GetNode<TextureRect>(SizeIndicatorPath);
        totalPopulationIndicator = GetNode<TextureRect>(TotalPopulationIndicatorPath);

        negativeAtpPopup = GetNode<CustomConfirmationDialog>(NegativeAtpPopupPath);
        organelleMenu = GetNode<OrganellePopupMenu>(OrganelleMenuPath);
        organelleUpgradeGUI = GetNode<OrganelleUpgradeGUI>(OrganelleUpgradeGUIPath);

        compoundBalance = GetNode<CompoundBalanceDisplay>(CompoundBalancePath);

        autoEvoPredictionExplanationPopup = GetNode<CustomDialog>(AutoEvoPredictionExplanationPopupPath);
        autoEvoPredictionExplanationLabel = GetNode<Label>(AutoEvoPredictionExplanationLabelPath);
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

        // Only when not loaded from save are these properties fetched (otherwise it won't display changes correctly)
        SetInitialCellStats();

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

    public override void _Process(float delta)
    {
        base._Process(delta);

        if (!Visible)
            return;

        var metrics = PerformanceMetrics.Instance;

        if (metrics.Visible)
        {
            var roughCount = Editor.RootOfDynamicallySpawned.GetChildCount();
            metrics.ReportEntities(roughCount, 0);
        }

        CheckRunningAutoEvoPrediction();

        if (organelleDataDirty)
        {
            OnOrganellesChanged();
            organelleDataDirty = false;
        }

        // Show the organelle that is about to be placed
        if (ActiveActionName != null && Editor.ShowHover && !MicrobePreviewMode)
        {
            GetMouseHex(out int q, out int r);

            OrganelleDefinition shownOrganelle;

            var effectiveSymmetry = Symmetry;

            if (MovingPlacedHex == null)
            {
                // Can place stuff at all?
                isPlacementProbablyValid = IsValidPlacement(new OrganelleTemplate(
                    GetOrganelleDefinition(ActiveActionName), new Hex(q, r), organelleRot));

                shownOrganelle = SimulationParameters.Instance.GetOrganelleType(ActiveActionName);
            }
            else
            {
                isPlacementProbablyValid = IsMoveTargetValid(new Hex(q, r), organelleRot, MovingPlacedHex);
                shownOrganelle = MovingPlacedHex.Definition;
                effectiveSymmetry = HexEditorSymmetry.None;
            }

            HashSet<(Hex Hex, int Orientation)> hoveredHexes = new();

            RunWithSymmetry(q, r,
                (finalQ, finalR, rotation) =>
                {
                    RenderHighlightedOrganelle(finalQ, finalR, rotation, shownOrganelle);
                    hoveredHexes.Add((new Hex(finalQ, finalR), rotation));
                }, effectiveSymmetry);

            MouseHoverHexes = hoveredHexes.ToList();
        }
    }

    public override void SetEditorWorldTabSpecificObjectVisibility(bool shown)
    {
        base.SetEditorWorldTabSpecificObjectVisibility(shown);

        if (previewMicrobe != null)
        {
            previewMicrobe.Visible = shown && MicrobePreviewMode;
        }
    }

    public override void OnMutationPointsChanged(int mutationPoints)
    {
        base.OnMutationPointsChanged(mutationPoints);
        UpdateRigiditySliderState(mutationPoints);
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
        if (!IsMulticellularEditor && energyBalanceInfo != null &&
            energyBalanceInfo.TotalProduction < energyBalanceInfo.TotalConsumptionStationary)
        {
            negativeAtpPopup.PopupCenteredShrink();
            return false;
        }

        return true;
    }

    public void UpdatePatchDependentBalanceData()
    {
        // Skip if not opened in the multicellular editor
        if (IsMulticellularEditor && editedMicrobeOrganelles.Organelles.Count < 1)
            return;

        // Calculate and send energy balance to the GUI
        CalculateEnergyBalanceWithOrganellesAndMembraneType(
            editedMicrobeOrganelles.Organelles, Membrane, Editor.CurrentPatch);

        CalculateCompoundBalanceInPatch(editedMicrobeOrganelles.Organelles, Editor.CurrentPatch);
    }

    /// <summary>
    ///   Calculates the effectiveness of organelles in the current or given patch
    /// </summary>
    public void CalculateOrganelleEffectivenessInPatch(Patch patch)
    {
        var organelles = SimulationParameters.Instance.GetAllOrganelles();

        var result = ProcessSystem.ComputeOrganelleProcessEfficiencies(organelles, patch.Biome);

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

        var data = new NewMicrobeActionData(oldEditedMicrobeOrganelles, oldMembrane);

        var action =
            new SingleCellEditorAction<NewMicrobeActionData>(DoNewMicrobeAction, UndoNewMicrobeAction, data);

        Editor.EnqueueAction(action);
    }

    public void OnMembraneSelected(string membraneName)
    {
        var membrane = SimulationParameters.Instance.GetMembrane(membraneName);

        if (Membrane.Equals(membrane))
            return;

        var action = new SingleCellEditorAction<MembraneActionData>(DoMembraneChangeAction, UndoMembraneChangeAction,
            new MembraneActionData(Membrane, membrane));

        Editor.EnqueueAction(action);

        // In case the action failed, we need to make sure the membrane buttons are updated properly
        UpdateMembraneButtons(Membrane.InternalName);
    }

    public void OnRigidityChanged(int rigidity)
    {
        int intRigidity = (int)Math.Round(Rigidity * Constants.MEMBRANE_RIGIDITY_SLIDER_TO_VALUE_RATIO);

        if (MovingPlacedHex != null)
        {
            Editor.OnActionBlockedWhileMoving();
            UpdateRigiditySlider(intRigidity);
            return;
        }

        if (intRigidity == rigidity)
            return;

        int costPerStep = (int)(Constants.MEMBRANE_RIGIDITY_COST_PER_STEP * editorCostFactor);
        int cost = Math.Abs(rigidity - intRigidity) * costPerStep;

        if (cost > Editor.MutationPoints)
        {
            int stepsLeft = Editor.MutationPoints / costPerStep;
            if (stepsLeft < 1)
            {
                UpdateRigiditySlider(intRigidity);
                return;
            }

            rigidity = intRigidity > rigidity ? intRigidity - stepsLeft : intRigidity + stepsLeft;
        }

        var newRigidity = rigidity / Constants.MEMBRANE_RIGIDITY_SLIDER_TO_VALUE_RATIO;
        var prevRigidity = Rigidity;

        var action = new SingleCellEditorAction<RigidityActionData>(DoRigidityChangeAction, UndoRigidityChangeAction,
            new RigidityActionData(newRigidity, prevRigidity));

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

    public float CalculateHitpoints()
    {
        var maxHitpoints = Membrane.Hitpoints +
            (Rigidity * Constants.MEMBRANE_RIGIDITY_HITPOINTS_MODIFIER);

        return maxHitpoints;
    }

    public float CalculateStorage()
    {
        var totalStorage = 0f;
        foreach (var organelle in editedMicrobeOrganelles)
        {
            if (organelle.Definition.Components.Storage != null)
            {
                totalStorage += organelle.Definition.Components.Storage.Capacity;
            }
        }

        return totalStorage;
    }

    protected override int CalculateCurrentActionCost()
    {
        if (string.IsNullOrEmpty(ActiveActionName) || !Editor.ShowHover)
            return 0;

        var organelleDefinition = SimulationParameters.Instance.GetOrganelleType(ActiveActionName!);

        // Calculated in this order to be consistent with placing unique organelles
        var cost = (int)(organelleDefinition.MPCost * editorCostFactor);

        if (MouseHoverHexes == null)
            return cost * Symmetry.PositionCount();

        var positions = MouseHoverHexes.ToList();

        var organelleTemplates = positions
            .Select(h => new OrganelleTemplate(organelleDefinition, h.Hex, h.Orientation)).ToList();

        CombinedMicrobeEditorAction moveOccupancies;

        if (MovingPlacedHex == null)
        {
            moveOccupancies = GetMultiActionWithOccupancies(positions, organelleTemplates, true);
        }
        else
        {
            moveOccupancies =
                GetMultiActionWithOccupancies(positions, new List<OrganelleTemplate> { MovingPlacedHex }, false);
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
        if (MoveOrganelle(MovingPlacedHex!, MovingPlacedHex!.Position, new Hex(q, r), MovingPlacedHex.Orientation,
                organelleRot))
        {
            // Move succeeded; Update the cancel button visibility so it's hidden because the move has completed
            MovingPlacedHex = null;

            // TODO: should this call be made through Editor here?
            UpdateCancelButtonVisibility();

            // Update rigidity slider in case it was disabled
            // TODO: could come up with a bit nicer design here
            int intRigidity = (int)Math.Round(Rigidity * Constants.MEMBRANE_RIGIDITY_SLIDER_TO_VALUE_RATIO);
            UpdateRigiditySlider(intRigidity);

            // Re-enable undo/redo button
            Editor.NotifyUndoRedoStateChanged();
        }
        else
        {
            Editor.OnInvalidAction();
        }
    }

    protected override CombinedMicrobeEditorAction CreateCombinedAction(IEnumerable<CellEditorAction> actions)
    {
        throw new NotImplementedException();
    }

    protected override bool IsMoveTargetValid(Hex position, int rotation, OrganelleTemplate organelle)
    {
        return editedMicrobeOrganelles.CanPlace(organelle.Definition, position, rotation, false);
    }

    protected override bool DoesActionEndInProgressAction(CombinedMicrobeEditorAction action)
    {
        // Allow only move actions with an in-progress move
        return action.Data.Any(d => d is MoveActionData);
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

    protected override CellEditorAction? TryRemoveHexAt(Hex location)
    {
        var organelleHere = editedMicrobeOrganelles.GetElementAt(location);
        if (organelleHere == null)
            return null;

        // Dont allow deletion of nucleus or the last organelle
        if (organelleHere.Definition == nucleus || MicrobeSize < 2)
            return null;

        // In multicellular binding agents can't be removed
        if (IsMulticellularEditor && organelleHere.Definition == bindingAgent)
            return null;

        return new SingleCellEditorAction<RemoveActionData>(DoOrganelleRemoveAction, UndoOrganelleRemoveAction,
            new RemoveActionData(organelleHere, organelleHere.Position, organelleHere.Orientation));
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

        return highestPointInMiddleRows - Constants.EDITOR_ARROW_OFFSET;
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
        organelleMenu.GetActionPrice = Editor.WhatWouldActionsCost;
        organelleMenu.ShowPopup = true;

        var count = organelles.Count;

        // Disable delete for nucleus or the last organelle.
        if (MicrobeSize <= count || organelles.Any(o => o.Definition == nucleus))
        {
            organelleMenu.EnableDeleteOption = false;
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
        }

        // Move enabled only when microbe has more than one organelle
        organelleMenu.EnableMoveOption = MicrobeSize > 1;

        // Modify / upgrade possible when defined on the primary organelle definition
        if (count > 0 && !string.IsNullOrEmpty(organelles.First().Definition.UpgradeGUI))
        {
            organelleMenu.EnableModifyOption = true;
        }
        else
        {
            organelleMenu.EnableModifyOption = false;
        }
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

    private CombinedMicrobeEditorAction GetMultiActionWithOccupancies(List<(Hex Hex, int Orientation)> hexes,
        List<OrganelleTemplate> organelles, bool moving)
    {
        var moveActionData = new List<CellEditorAction>();
        foreach (var (hex, organelle, orientation, occupied) in GetOccupancies(hexes, organelles))
        {
            CellEditorAction action;
            if (occupied)
            {
                var data = new RemoveActionData(organelle, organelle.Position, organelle.Orientation)
                {
                    GotReplaced = organelle.Definition.InternalName == "cytoplasm",
                };
                action = new SingleCellEditorAction<RemoveActionData>(DoOrganelleRemoveAction,
                    UndoOrganelleRemoveAction, data);
            }
            else
            {
                if (moving)
                {
                    var data = new MoveActionData(organelle, organelle.Position, hex, organelle.Orientation,
                        orientation);
                    action = new SingleCellEditorAction<MoveActionData>(DoOrganelleMoveAction,
                        UndoOrganelleMoveAction, data);
                }
                else
                {
                    var replacedHex = editedMicrobeOrganelles.GetElementAt(hex);
                    var data = new PlacementActionData(organelle, hex, orientation);
                    if (replacedHex != null)
                        data.ReplacedCytoplasm = new List<OrganelleTemplate> { replacedHex };

                    action = new SingleCellEditorAction<PlacementActionData>(DoOrganellePlaceAction,
                        UndoOrganellePlaceAction, data);
                }
            }

            moveActionData.Add(action);
        }

        return new CombinedMicrobeEditorAction(moveActionData.ToArray());
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

    private IEnumerable<RemoveActionData> GetReplacedCytoplasmRemoveActionData(
        IEnumerable<OrganelleTemplate> organelles)
    {
        return GetReplacedCytoplasm(organelles)
            .Select(o => new RemoveActionData(o, o.Position, o.Orientation)
            {
                GotReplaced = true,
            });
    }

    private IEnumerable<SingleCellEditorAction<RemoveActionData>> GetReplacedCytoplasmRemoveAction(
        IEnumerable<OrganelleTemplate> organelles)
    {
        var replacedCytoplasmData = GetReplacedCytoplasmRemoveActionData(organelles);
        return replacedCytoplasmData.Select(o =>
            new SingleCellEditorAction<RemoveActionData>(DoOrganelleRemoveAction, UndoOrganelleRemoveAction, o));
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
    ///   Calculates the energy balance for a cell with the given organelles
    /// </summary>
    private void CalculateEnergyBalanceWithOrganellesAndMembraneType(IReadOnlyCollection<OrganelleTemplate> organelles,
        MembraneType membrane, Patch? patch = null)
    {
        patch ??= Editor.CurrentPatch;

        UpdateEnergyBalance(ProcessSystem.ComputeEnergyBalance(organelles, patch.Biome, membrane));
    }

    private void CalculateCompoundBalanceInPatch(IReadOnlyCollection<OrganelleTemplate> organelles, Patch? patch = null)
    {
        patch ??= Editor.CurrentPatch;

        var result = ProcessSystem.ComputeCompoundBalance(organelles, patch.Biome);

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

        var placementActions = new List<CellEditorAction>();

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

                var placed = PlaceIfPossible(organelle);

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

        var multiAction = new CombinedMicrobeEditorAction(placementActions.ToArray());

        return EnqueueAction(multiAction);
    }

    /// <summary>
    ///   Helper for AddOrganelle
    /// </summary>
    private CombinedMicrobeEditorAction? PlaceIfPossible(OrganelleTemplate organelle)
    {
        if (MicrobePreviewMode)
            return null;

        if (!IsValidPlacement(organelle))
        {
            // Play Sound
            Editor.OnInvalidAction();
            return null;
        }

        return AddOrganelle(organelle);
    }

    private bool IsValidPlacement(OrganelleTemplate organelle)
    {
        bool notPlacingCytoplasm = organelle.Definition.InternalName != "cytoplasm";

        return editedMicrobeOrganelles.CanPlaceAndIsTouching(
            organelle,
            notPlacingCytoplasm,
            notPlacingCytoplasm);
    }

    private CombinedMicrobeEditorAction? AddOrganelle(OrganelleTemplate organelle)
    {
        // 1 - you put a unique organelle (means only one instance allowed) but you already have it
        // 2 - you put an organelle that requires nucleus but you don't have one
        if ((organelle.Definition.Unique && HasOrganelle(organelle.Definition)) ||
            (organelle.Definition.RequiresNucleus && !HasNucleus))
            return null;

        if (organelle.Definition.Unique)
            DeselectOrganelleToPlace();

        var replacedCytoplasmActions =
            GetReplacedCytoplasmRemoveAction(new[] { organelle }).Cast<CellEditorAction>().ToList();

        var action = new SingleCellEditorAction<PlacementActionData>(
            DoOrganellePlaceAction, UndoOrganellePlaceAction,
            new PlacementActionData(organelle, organelle.Position, organelle.Orientation));

        replacedCytoplasmActions.Add(action);
        return new CombinedMicrobeEditorAction(replacedCytoplasmActions.ToArray());
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

    private void OnPostNewMicrobeChange()
    {
        UpdateMembraneButtons(Membrane.InternalName);
        UpdateSpeed(CalculateSpeed());
        UpdateHitpoints(CalculateHitpoints());
        UpdateStorage(CalculateStorage());

        StartAutoEvoPrediction();
    }

    private void OnRigidityChanged()
    {
        UpdateRigiditySlider((int)Math.Round(Rigidity * Constants.MEMBRANE_RIGIDITY_SLIDER_TO_VALUE_RATIO));

        UpdateSpeed(CalculateSpeed());
        UpdateHitpoints(CalculateHitpoints());
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

    private void UpdateOrganelleButtons(string? organelle)
    {
        // Update the icon highlightings
        foreach (var selection in placeablePartSelectionElements.Values)
        {
            selection.Selected = selection.Name == organelle;
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

        // Send to gui current status of cell
        UpdateSize(MicrobeHexSize);

        UpdatePartsAvailability(PlacedUniqueOrganelles.ToList());

        UpdatePatchDependentBalanceData();

        UpdateSpeed(CalculateSpeed());

        UpdateStorage(CalculateStorage());

        UpdateCellVisualization();

        StartAutoEvoPrediction();
    }

    /// <summary>
    ///   This destroys and creates again entities to represent all the currently placed organelles. Call this whenever
    ///   editedMicrobeOrganelles is changed.
    /// </summary>
    private void UpdateAlreadyPlacedVisuals()
    {
        var islands = editedMicrobeOrganelles.GetIslandHexes();

        // Build the entities to show the current microbe
        UpdateAlreadyPlacedHexes(
            editedMicrobeOrganelles.Select(o => (o.Position, o.RotatedHexes, PlacedThisSession(o))), islands,
            microbePreviewMode);

        int nextFreeOrganelle = 0;

        foreach (var organelle in editedMicrobeOrganelles)
        {
            // Hexes are handled by UpdateAlreadyPlacedHexes

            // Model of the organelle
            if (organelle.Definition.DisplayScene != null)
            {
                var pos = Hex.AxialToCartesian(organelle.Position) +
                    organelle.Definition.CalculateModelOffset();

                if (nextFreeOrganelle >= placedModels.Count)
                {
                    // New organelle model needed
                    placedModels.Add(CreatePreviewModelHolder());
                }

                var organelleModel = placedModels[nextFreeOrganelle++];

                organelleModel.Transform = new Transform(
                    MathUtils.CreateRotationForOrganelle(1 * organelle.Orientation), pos);

                organelleModel.Scale = new Vector3(Constants.DEFAULT_HEX_SIZE, Constants.DEFAULT_HEX_SIZE,
                    Constants.DEFAULT_HEX_SIZE);

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

    private bool PlacedThisSession(OrganelleTemplate organelle)
    {
        return Editor.OrganellePlacedThisSession(organelle);
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
        var action =
            new CombinedMicrobeEditorAction(organelleMenu.SelectedOrganelles
                .Select(o => TryRemoveHexAt(o.Position)).WhereNotNull().ToArray());
        EnqueueAction(action);
    }

    private void OnModifyPressed()
    {
        var targetOrganelle = organelleMenu.SelectedOrganelles.First();
        var upgradeGUI = targetOrganelle.Definition.UpgradeGUI;

        if (string.IsNullOrEmpty(upgradeGUI))
        {
            GD.PrintErr("Attempted to modify an organelle with no upgrade GUI known");
            return;
        }

        organelleUpgradeGUI.OpenForOrganelle(targetOrganelle, upgradeGUI!, Editor);
    }

    /// <summary>
    ///   Updates the organelle model displayer to have the specified scene in it
    /// </summary>
    private void UpdateOrganellePlaceHolderScene(SceneDisplayer organelleModel,
        string displayScene, OrganelleDefinition definition, int renderPriority)
    {
        organelleModel.Scene = displayScene;
        var material = organelleModel.GetMaterial(definition.DisplaySceneModelPath);
        if (material != null)
        {
            material.RenderPriority = renderPriority;
        }
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
            control.MPCost = (int)(organelle.MPCost * editorCostFactor);
            control.Name = organelle.InternalName;

            // Special case with registering the tooltip here for item with no associated organelle
            control.RegisterToolTipForControl(organelle.InternalName, "organelleSelection");

            group.AddItem(control);

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
            control.MPCost = (int)(membraneType.EditorCost * editorCostFactor);
            control.Name = membraneType.InternalName;

            control.RegisterToolTipForControl(membraneType.InternalName, "membraneSelection");

            membraneTypeSelection.AddItem(control);

            membraneSelectionElements.Add(membraneType, control);

            control.Connect(nameof(MicrobePartSelection.OnPartSelected), this, nameof(OnMembraneSelected));
        }
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
        Colour = color;
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

    private void UpdateCellStatsIndicators()
    {
        sizeIndicator.Show();

        if (MicrobeHexSize > initialCellSize)
        {
            sizeIndicator.Texture = increaseIcon;
        }
        else if (MicrobeHexSize < initialCellSize)
        {
            sizeIndicator.Texture = decreaseIcon;
        }
        else
        {
            sizeIndicator.Hide();
        }

        speedIndicator.Show();

        if (CalculateSpeed() > initialCellSpeed)
        {
            speedIndicator.Texture = increaseIcon;
        }
        else if (CalculateSpeed() < initialCellSpeed)
        {
            speedIndicator.Texture = decreaseIcon;
        }
        else
        {
            speedIndicator.Hide();
        }

        hpIndicator.Show();

        if (CalculateHitpoints() > initialCellHp)
        {
            hpIndicator.Texture = increaseIcon;
        }
        else if (CalculateHitpoints() < initialCellHp)
        {
            hpIndicator.Texture = decreaseIcon;
        }
        else
        {
            hpIndicator.Hide();
        }

        storageIndicator.Show();

        if (CalculateStorage() > initialCellStorage)
        {
            storageIndicator.Texture = increaseIcon;
        }
        else if (CalculateStorage() < initialCellStorage)
        {
            storageIndicator.Texture = decreaseIcon;
        }
        else
        {
            storageIndicator.Hide();
        }
    }

    private void UpdateAutoEvoPredictionTranslations()
    {
        if (autoEvoPredictionRunSuccessful is false)
        {
            totalPopulationLabel.Text = TranslationServer.Translate("FAILED");
        }

        if (!string.IsNullOrEmpty(bestPatchName))
        {
            bestPatchLabel.Text = string.Format(CultureInfo.CurrentCulture,
                TranslationServer.Translate("POPULATION_IN_PATCH_SHORT"),
                TranslationServer.Translate(bestPatchName),
                bestPatchPopulation);
        }
        else
        {
            bestPatchLabel.Text = TranslationServer.Translate("N_A");
        }

        if (!string.IsNullOrEmpty(worstPatchName))
        {
            worstPatchLabel.Text = string.Format(CultureInfo.CurrentCulture,
                TranslationServer.Translate("POPULATION_IN_PATCH_SHORT"),
                TranslationServer.Translate(worstPatchName),
                worstPatchPopulation);
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

        if (newPopulation > run.PlayerSpeciesOriginal.Population)
        {
            totalPopulationIndicator.Texture = increaseIcon;
        }
        else if (newPopulation < run.PlayerSpeciesOriginal.Population)
        {
            totalPopulationIndicator.Texture = decreaseIcon;
        }
        else
        {
            totalPopulationIndicator.Texture = null;
        }

        autoEvoPredictionRunSuccessful = true;
        totalPopulationLabel.Text = newPopulation.ToString(CultureInfo.CurrentCulture);

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
            run.PlayerSpeciesOriginal.FormattedName);

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
        autoEvoPredictionExplanationLabel.Text = predictionDetailsText != null ?
            predictionDetailsText.ToString() :
            TranslationServer.Translate("NO_DATA_TO_SHOW");
    }

    private void SetInitialCellStats()
    {
        initialCellSpeed = CalculateSpeed();
        initialCellHp = CalculateHitpoints();
        initialCellStorage = CalculateStorage();
        initialCellSize = MicrobeHexSize;
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
