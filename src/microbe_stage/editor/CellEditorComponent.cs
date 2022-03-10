using Godot;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using AutoEvo;
using Newtonsoft.Json;

/// <summary>
///   The cell editor component combining the organelle and other editing logic with the GUI for it
/// </summary>
public partial class CellEditorComponent :
    HexEditorComponentBase<MicrobeEditor, MicrobeEditorAction, OrganelleTemplate>,
    IGodotEarlyNodeResolve
{
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
    public NodePath SizeLabelPath = null!;

    [Export]
    public NodePath OrganismStatisticsPath = null!;

    [Export]
    public NodePath SpeedLabelPath = null!;

    [Export]
    public NodePath HpLabelPath = null!;

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

    // TODO: rename this (as this is also used for cell type naming)
    [Export]
    public NodePath SpeciesNameEditPath = null!;

    [Export]
    public NodePath MembraneColorPickerPath = null!;

    [Export]
    public NodePath NewCellButtonPath = null!;

    [Export]
    public NodePath RandomizeSpeciesNameButtonPath = null!;

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

    private Label sizeLabel = null!;
    private Label speedLabel = null!;
    private Label hpLabel = null!;
    private Label generationLabel = null!;
    private Label totalPopulationLabel = null!;
    private Label bestPatchLabel = null!;
    private Label worstPatchLabel = null!;

    private Control autoEvoPredictionPanel = null!;

    private Slider rigiditySlider = null!;
    private TweakedColourPicker membraneColorPicker = null!;

    private TextureButton newCellButton = null!;
    private LineEdit speciesNameEdit = null!;
    private TextureButton randomizeSpeciesNameButton = null!;

    private Label atpBalanceLabel = null!;
    private Label atpProductionLabel = null!;
    private Label atpConsumptionLabel = null!;
    private SegmentedBar atpProductionBar = null!;
    private SegmentedBar atpConsumptionBar = null!;

    private TextureRect speedIndicator = null!;
    private TextureRect hpIndicator = null!;
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

    private OrganelleDefinition protoplasm = null!;
    private OrganelleDefinition nucleus = null!;

    private EnergyBalanceInfo? energyBalanceInfo;

    [JsonProperty]
    private float initialCellSpeed;

    [JsonProperty]
    private int initialCellSize;

    [JsonProperty]
    private float initialCellHp;

    [JsonProperty]
    private string? bestPatchName;

    [JsonProperty]
    private long bestPatchPopulation;

    [JsonProperty]
    private string? worstPatchName;

    [JsonProperty]
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
    public string NewName = "unset";

    /// <summary>
    ///   We're taking advantage of the available membrane and organelle system already present in
    ///   the microbe class for the cell preview.
    /// </summary>
    private Microbe? previewMicrobe;

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

            if (previewMicrobe?.Species != null)
            {
                previewMicrobe.Species.MembraneRigidity = value;
                previewMicrobe.ApplyMembraneWigglyness();
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
    public bool HasNucleus => PlacedUniqueOrganelles.Any(d => d.InternalName == "nucleus");

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

        // Hidden in the editor to make selecting other things easier
        organelleUpgradeGUI.Visible = true;

        atpProductionBar.SelectedType = SegmentedBar.Type.ATP;
        atpProductionBar.IsProduction = true;
        atpConsumptionBar.SelectedType = SegmentedBar.Type.ATP;

        protoplasm = SimulationParameters.Instance.GetOrganelleType("protoplasm");
        nucleus = SimulationParameters.Instance.GetOrganelleType("nucleus");
        questionIcon = GD.Load<Texture>("res://assets/textures/gui/bevel/helpButton.png");
        increaseIcon = GD.Load<Texture>("res://assets/textures/gui/bevel/increase.png");
        decreaseIcon = GD.Load<Texture>("res://assets/textures/gui/bevel/decrease.png");

        SetupMicrobePartSelections();
        UpdateMicrobePartSelections();

        ApplySelectionMenuTab();
        RegisterTooltips();
    }

    public override void Init(MicrobeEditor owningEditor, bool fresh)
    {
        base.Init(owningEditor, fresh);
        behaviourEditor.Init(owningEditor, fresh);

        if (fresh)
        {
            editedMicrobeOrganelles = new OrganelleLayout<OrganelleTemplate>(
                OnOrganelleAdded, OnOrganelleRemoved);
        }
        else
        {
            UpdateGUIAfterLoadingSpecies(Editor.EditedSpecies);
            UpdateArrow(false);
        }

        UpdateMutationPointsBar(false);

        // Send info to the GUI about the organelle effectiveness in the current patch
        CalculateOrganelleEffectivenessInPatch(Editor.CurrentPatch);

        UpdateRigiditySliderState(Editor.MutationPoints);

        UpdateCancelButtonVisibility();
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

        sizeLabel = GetNode<Label>(SizeLabelPath);
        speedLabel = GetNode<Label>(SpeedLabelPath);
        hpLabel = GetNode<Label>(HpLabelPath);
        generationLabel = GetNode<Label>(GenerationLabelPath);
        totalPopulationLabel = GetNode<Label>(TotalPopulationLabelPath);
        worstPatchLabel = GetNode<Label>(WorstPatchLabelPath);
        bestPatchLabel = GetNode<Label>(BestPatchLabelPath);

        autoEvoPredictionPanel = GetNode<Control>(AutoEvoPredictionPanelPath);

        rigiditySlider = GetNode<Slider>(RigiditySliderPath);
        membraneColorPicker = GetNode<TweakedColourPicker>(MembraneColorPickerPath);

        newCellButton = GetNode<TextureButton>(NewCellButtonPath);
        speciesNameEdit = GetNode<LineEdit>(SpeciesNameEditPath);
        randomizeSpeciesNameButton = GetNode<TextureButton>(RandomizeSpeciesNameButtonPath);

        atpBalanceLabel = GetNode<Label>(ATPBalanceLabelPath);
        atpProductionLabel = GetNode<Label>(ATPProductionLabelPath);
        atpConsumptionLabel = GetNode<Label>(ATPConsumptionLabelPath);
        atpProductionBar = GetNode<SegmentedBar>(ATPProductionBarPath);
        atpConsumptionBar = GetNode<SegmentedBar>(ATPConsumptionBarPath);

        speedIndicator = GetNode<TextureRect>(SpeedIndicatorPath);
        hpIndicator = GetNode<TextureRect>(HpIndicatorPath);
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

        // We set these here to make sure these are ready in the organelle add callbacks (even though currently
        // that just marks things dirty and we update our stats on the next _Process call)
        Membrane = Editor.EditedSpecies.MembraneType;
        Rigidity = Editor.EditedSpecies.MembraneRigidity;
        Colour = Editor.EditedSpecies.Colour;

        behaviourEditor.OnEditorSpeciesSetup(species);

        // Get the species organelles to be edited. This also updates the placeholder hexes
        foreach (var organelle in Editor.EditedSpecies.Organelles.Organelles)
        {
            editedMicrobeOrganelles.Add((OrganelleTemplate)organelle.Clone());
        }

        NewName = species.FormattedName;

        // Only when not loaded from save are these properties fetched (otherwise it won't display changes correctly)
        SetInitialCellStats();

        UpdateGUIAfterLoadingSpecies(Editor.EditedSpecies);

        // Setup the display cell
        previewMicrobe = (Microbe)microbeScene.Instance();
        previewMicrobe.IsForPreviewOnly = true;
        Editor.RootOfDynamicallySpawned.AddChild(previewMicrobe);
        previewMicrobe.ApplySpecies((MicrobeSpecies)Editor.EditedSpecies.Clone());

        // Set its initial visibility
        previewMicrobe.Visible = MicrobePreviewMode;

        UpdateArrow(false);
    }

    public override void OnFinishEditing()
    {
        var editedSpecies = Editor.EditedSpecies;

        // Apply changes to the species organelles
        // It is easiest to just replace all
        editedSpecies.Organelles.Clear();

        foreach (var organelle in editedMicrobeOrganelles.Organelles)
        {
            var organelleToAdd = (OrganelleTemplate)organelle.Clone();
            organelleToAdd.PlacedThisSession = false;
            organelleToAdd.NumberOfTimesMoved = 0;
            editedSpecies.Organelles.Add(organelleToAdd);
        }

        editedSpecies.RepositionToOrigin();

        // Update bacteria status
        editedSpecies.IsBacteria = !HasNucleus;

        editedSpecies.UpdateInitialCompounds();

        GD.Print("MicrobeEditor: updated organelles for species: ", editedSpecies.FormattedName);

        // Update name, if valid
        var match = Regex.Match(NewName, Constants.SPECIES_NAME_REGEX);
        if (match.Success)
        {
            editedSpecies.Genus = match.Groups["genus"].Value;
            editedSpecies.Epithet = match.Groups["epithet"].Value;

            GD.Print("MicrobeEditor: edited species name is now ", editedSpecies.FormattedName);
        }
        else
        {
            GD.PrintErr("MicrobeEditor: Invalid newName: ", NewName);
        }

        // Update membrane
        editedSpecies.MembraneType = Membrane;
        editedSpecies.Colour = Colour;
        editedSpecies.MembraneRigidity = Rigidity;

        behaviourEditor.OnFinishEditing();
    }

    public override void _Process(float delta)
    {
        base._Process(delta);

        if (!Visible)
            return;

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

            switch (effectiveSymmetry)
            {
                case HexEditorSymmetry.None:
                {
                    RenderHighlightedOrganelle(q, r, organelleRot, shownOrganelle);
                    break;
                }

                case HexEditorSymmetry.XAxisSymmetry:
                {
                    RenderHighlightedOrganelle(q, r, organelleRot, shownOrganelle);
                    RenderHighlightedOrganelle(-1 * q, r + q, 6 + (-1 * organelleRot), shownOrganelle);
                    break;
                }

                case HexEditorSymmetry.FourWaySymmetry:
                {
                    RenderHighlightedOrganelle(q, r, organelleRot, shownOrganelle);
                    RenderHighlightedOrganelle(-1 * q, r + q, 6 + (-1 * organelleRot), shownOrganelle);
                    RenderHighlightedOrganelle(-1 * q, -1 * r, (organelleRot + 3) % 6, shownOrganelle);
                    RenderHighlightedOrganelle(q, -1 * (r + q), 9 + (-1 * organelleRot) % 6, shownOrganelle);
                    break;
                }

                case HexEditorSymmetry.SixWaySymmetry:
                {
                    RenderHighlightedOrganelle(q, r, organelleRot, shownOrganelle);
                    RenderHighlightedOrganelle(-1 * r, r + q, (organelleRot + 1) % 6, shownOrganelle);
                    RenderHighlightedOrganelle(-1 * (r + q), q, (organelleRot + 2) % 6, shownOrganelle);
                    RenderHighlightedOrganelle(-1 * q, -1 * r, (organelleRot + 3) % 6, shownOrganelle);
                    RenderHighlightedOrganelle(r, -1 * (r + q), (organelleRot + 4) % 6, shownOrganelle);
                    RenderHighlightedOrganelle(r + q, -1 * q, (organelleRot + 5) % 6, shownOrganelle);
                    break;
                }
            }
        }
    }

    public override void OnMutationPointsChanged(int mutationPoints)
    {
        UpdateMutationPointsBar();
        UpdateRigiditySliderState(mutationPoints);
    }

    public void SetSpeciesInfo(string name, MembraneType membrane, Color colour, float rigidity,
        BehaviourDictionary behaviour)
    {
        speciesNameEdit.Text = name;
        membraneColorPicker.Color = colour;

        // Callback is manually called because the function isn't called automatically here
        OnSpeciesNameTextChanged(name);

        UpdateMembraneButtons(membrane.InternalName);
        SetMembraneTooltips(membrane);

        UpdateRigiditySlider((int)Math.Round(rigidity * Constants.MEMBRANE_RIGIDITY_SLIDER_TO_VALUE_RATIO));

        // TODO: put this call in some better place
        behaviourEditor.UpdateAllBehaviouralSliders(behaviour);
    }

    /// <summary>
    ///   Wipes clean the current cell.
    /// </summary>
    public void CreateNewMicrobe()
    {
        if (!Editor.FreeBuilding)
            throw new InvalidOperationException("can't reset cell when not freebuilding");

        var previousMP = Editor.MutationPoints;
        var oldEditedMicrobeOrganelles = new OrganelleLayout<OrganelleTemplate>();
        var oldMembrane = Membrane;

        foreach (var organelle in editedMicrobeOrganelles)
        {
            oldEditedMicrobeOrganelles.Add(organelle);
        }

        var data = new NewMicrobeActionData(oldEditedMicrobeOrganelles, previousMP, oldMembrane);

        var action = new MicrobeEditorAction(Editor, 0, DoNewMicrobeAction, UndoNewMicrobeAction, data);

        Editor.EnqueueAction(action);
    }

    public void OnMembraneSelected(string membraneName)
    {
        var membrane = SimulationParameters.Instance.GetMembrane(membraneName);

        if (Membrane.Equals(membrane))
            return;

        var action = new MicrobeEditorAction(Editor, membrane.EditorCost, DoMembraneChangeAction,
            UndoMembraneChangeAction, new MembraneActionData(Membrane, membrane));

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

        int cost = Math.Abs(rigidity - intRigidity) * Constants.MEMBRANE_RIGIDITY_COST_PER_STEP;

        if (cost > Editor.MutationPoints)
        {
            int stepsLeft = Editor.MutationPoints / Constants.MEMBRANE_RIGIDITY_COST_PER_STEP;
            if (stepsLeft < 1)
            {
                UpdateRigiditySlider(intRigidity);
                return;
            }

            rigidity = intRigidity > rigidity ? intRigidity - stepsLeft : intRigidity + stepsLeft;
            cost = stepsLeft * Constants.MEMBRANE_RIGIDITY_COST_PER_STEP;
        }

        var newRigidity = rigidity / Constants.MEMBRANE_RIGIDITY_SLIDER_TO_VALUE_RATIO;
        var prevRigidity = Rigidity;

        var action = new MicrobeEditorAction(Editor, cost, DoRigidityChangeAction, UndoRigidityChangeAction,
            new RigidityChangeActionData(newRigidity, prevRigidity));

        Editor.EnqueueAction(action);
    }

    /// <summary>
    ///   Show options for the organelle under the cursor
    /// </summary>
    [RunOnKeyDown("e_secondary")]
    public void ShowOrganelleOptions()
    {
        if (MicrobePreviewMode)
            return;

        // Can't open organelle popup menu while moving something
        if (MovingPlacedHex != null)
        {
            Editor.OnActionBlockedWhileMoving();
            return;
        }

        GetMouseHex(out int q, out int r);

        var organelle = editedMicrobeOrganelles.GetOrganelleAt(new Hex(q, r));

        if (organelle == null)
            return;

        ShowOrganelleMenu(organelle);
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

    /// <summary>
    ///   Returns the cost of the organelle that is about to be placed
    /// </summary>
    protected override int CalculateCurrentActionCost()
    {
        if (string.IsNullOrEmpty(ActiveActionName) || !Editor.ShowHover)
            return 0;

        var cost = SimulationParameters.Instance.GetOrganelleType(ActiveActionName!).MPCost;

        switch (Symmetry)
        {
            case HexEditorSymmetry.XAxisSymmetry:
                cost *= 2;
                break;
            case HexEditorSymmetry.FourWaySymmetry:
                cost *= 4;
                break;
            case HexEditorSymmetry.SixWaySymmetry:
                cost *= 6;
                break;
        }

        return cost;
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

    protected override bool IsMoveTargetValid(Hex position, int rotation, OrganelleTemplate organelle)
    {
        return editedMicrobeOrganelles.CanPlace(organelle.Definition, position, rotation, false);
    }

    protected override bool DoesActionEndInProgressAction(MicrobeEditorAction action)
    {
        // Allow only move actions with an in-progress move
        return action.IsMoveAction;
    }

    protected override void OnCurrentActionCanceled()
    {
        editedMicrobeOrganelles.Add(MovingPlacedHex!);
        UpdateCancelButtonVisibility();
    }

    protected override void OnMoveActionStarted()
    {
        editedMicrobeOrganelles.Remove(MovingPlacedHex!);
    }

    protected override OrganelleTemplate? GetHexAt(Hex position)
    {
        return editedMicrobeOrganelles.GetOrganelleAt(position);
    }

    protected override void TryRemoveHexAt(Hex location)
    {
        var organelleHere = editedMicrobeOrganelles.GetOrganelleAt(location);
        if (organelleHere == null)
            return;

        // Dont allow deletion of nucleus or the last organelle
        if (organelleHere.Definition.InternalName == "nucleus" || MicrobeSize < 2)
            return;

        // If it was placed this session, just refund the cost of adding it.
        int cost = organelleHere.PlacedThisSession ?
            -organelleHere.Definition.MPCost :
            Constants.ORGANELLE_REMOVE_COST;

        var action = new MicrobeEditorAction(Editor, cost,
            DoOrganelleRemoveAction, UndoOrganelleRemoveAction, new RemoveActionData(organelleHere));

        EnqueueAction(action);
    }

    public override void UpdateUndoRedoButtons(bool canUndo, bool canRedo)
    {
        SetUndoButtonStatus(canUndo && MovingPlacedHex == null);
        SetRedoButtonStatus(canRedo && MovingPlacedHex == null);
    }

    protected override void UpdateCancelState()
    {
        UpdateCancelButtonVisibility();
    }

    private bool HasOrganelle(OrganelleDefinition organelleDefinition)
    {
        return editedMicrobeOrganelles.Organelles.Any(o => o.Definition == organelleDefinition);
    }

    /// <summary>
    ///   Updates the arrowPosition variable to the top most point of the middle 3 rows
    ///   Should be called on any layout change
    /// </summary>
    private void UpdateArrow(bool animateMovement = true)
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

        if (animateMovement)
        {
            GUICommon.Instance.Tween.InterpolateProperty(editorArrow, "translation:z", editorArrow.Translation.z,
                highestPointInMiddleRows - Constants.EDITOR_ARROW_OFFSET, Constants.EDITOR_ARROW_INTERPOLATE_SPEED,
                Tween.TransitionType.Expo, Tween.EaseType.Out);
            GUICommon.Instance.Tween.Start();
        }
        else
        {
            editorArrow.Translation = new Vector3(0, 0, highestPointInMiddleRows - Constants.EDITOR_ARROW_OFFSET);
        }
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

    private void StartAutoEvoPrediction()
    {
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

        cachedAutoEvoPredictionSpecies ??= (MicrobeSpecies)Editor.EditedSpecies.Clone();

        CopyEditedPropertiesToSpecies(cachedAutoEvoPredictionSpecies);

        var run = new EditorAutoEvoRun(Editor.CurrentGame.GameWorld, Editor.EditedSpecies,
            cachedAutoEvoPredictionSpecies);
        run.Start();

        UpdateAutoEvoPrediction(run, Editor.EditedSpecies, cachedAutoEvoPredictionSpecies);
    }

    /// <summary>
    ///   Calculates the energy balance for a cell with the given organelles
    /// </summary>
    private void CalculateEnergyBalanceWithOrganellesAndMembraneType(List<OrganelleTemplate> organelles,
        MembraneType membrane, Patch? patch = null)
    {
        patch ??= Editor.CurrentPatch;

        UpdateEnergyBalance(ProcessSystem.ComputeEnergyBalance(organelles, patch.Biome, membrane));
    }

    private void CalculateCompoundBalanceInPatch(List<OrganelleTemplate> organelles, Patch? patch = null)
    {
        patch ??= Editor.CurrentPatch;

        var result = ProcessSystem
            .ComputeCompoundBalance(organelles, patch.Biome);

        UpdateCompoundBalances(result);
    }

    /// <summary>
    ///   If not hovering over an organelle, render the to-be-placed organelle
    /// </summary>
    private void RenderHighlightedOrganelle(int q, int r, int rotation, OrganelleDefinition shownOrganelle)
    {
        if (MovingPlacedHex == null && ActiveActionName == null)
            return;

        bool showModel = true;

        foreach (var hex in shownOrganelle.GetRotatedHexes(rotation))
        {
            int posQ = hex.Q + q;
            int posR = hex.R + r;

            var pos = Hex.AxialToCartesian(new Hex(posQ, posR));

            // Detect can it be placed there
            bool canPlace = isPlacementProbablyValid;

            bool duplicate = false;

            // Skip if there is a placed organelle here already
            foreach (var placed in placedHexes)
            {
                if ((pos - placed.Translation).LengthSquared() < 0.001f)
                {
                    duplicate = true;

                    if (!canPlace)
                    {
                        // This check is here so that if there are multiple hover hexes overlapping this hex, then
                        // we do actually remember the original material
                        if (!hoverOverriddenMaterials.ContainsKey(placed))
                        {
                            // Store the material to put it back later
                            hoverOverriddenMaterials[placed] = placed.MaterialOverride;
                        }

                        // Mark as invalid
                        placed.MaterialOverride = invalidMaterial;

                        showModel = false;
                    }

                    break;
                }
            }

            // Or if there is already a hover hex at this position
            for (int i = 0; i < usedHoverHex; ++i)
            {
                if ((pos - hoverHexes[i].Translation).LengthSquared() < 0.001f)
                {
                    duplicate = true;
                    break;
                }
            }

            if (duplicate)
                continue;

            var hoverHex = hoverHexes[usedHoverHex++];

            hoverHex.Translation = pos;
            hoverHex.Visible = true;

            hoverHex.MaterialOverride = canPlace ? validMaterial : invalidMaterial;
        }

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

        CopyEditedPropertiesToSpecies(previewMicrobe.Species);

        // Intentionally force it to not be bacteria to show it at full size
        previewMicrobe.Species.IsBacteria = false;

        // This is now just for applying changes in the species to the preview cell
        previewMicrobe.ApplySpecies(previewMicrobe.Species);

        membraneOrganellePositionsAreDirty = false;
    }

    /// <summary>
    ///   Places an organelle of the specified type under the cursor and also applies symmetry to place multiple at once.
    /// </summary>
    /// <returns>True when at least one organelle got placed</returns>
    private bool AddOrganelle(string organelleType)
    {
        GetMouseHex(out int q, out int r);

        bool placedSomething = false;

        switch (Symmetry)
        {
            case HexEditorSymmetry.None:
            {
                PlaceIfPossible(organelleType, q, r, organelleRot, ref placedSomething);
                break;
            }

            case HexEditorSymmetry.XAxisSymmetry:
            {
                PlaceIfPossible(organelleType, q, r, organelleRot, ref placedSomething);

                if (q != -1 * q || r != r + q)
                {
                    PlaceIfPossible(organelleType, -1 * q, r + q, 6 + (-1 * organelleRot), ref placedSomething);
                }

                break;
            }

            case HexEditorSymmetry.FourWaySymmetry:
            {
                PlaceIfPossible(organelleType, q, r, organelleRot, ref placedSomething);

                if (q != -1 * q || r != r + q)
                {
                    PlaceIfPossible(organelleType, -1 * q, r + q, 6 + (-1 * organelleRot), ref placedSomething);
                    PlaceIfPossible(organelleType, -1 * q, -1 * r, (organelleRot + 3) % 6, ref placedSomething);
                    PlaceIfPossible(organelleType, q, -1 * (r + q), (9 + (-1 * organelleRot)) % 6, ref placedSomething);
                }
                else
                {
                    PlaceIfPossible(organelleType, -1 * q, -1 * r, (organelleRot + 3) % 6, ref placedSomething);
                }

                break;
            }

            case HexEditorSymmetry.SixWaySymmetry:
            {
                PlaceIfPossible(organelleType, q, r, organelleRot, ref placedSomething);

                PlaceIfPossible(organelleType, -1 * r, r + q, (organelleRot + 1) % 6, ref placedSomething);
                PlaceIfPossible(organelleType, -1 * (r + q), q,
                    (organelleRot + 2) % 6, ref placedSomething);
                PlaceIfPossible(organelleType, -1 * q, -1 * r, (organelleRot + 3) % 6, ref placedSomething);
                PlaceIfPossible(organelleType, r, -1 * (r + q),
                    (organelleRot + 4) % 6, ref placedSomething);
                PlaceIfPossible(organelleType, r + q, -1 * q, (organelleRot + 5) % 6, ref placedSomething);

                break;
            }

            default:
            {
                throw new Exception("unimplemented symmetry in AddOrganelle");
            }
        }

        return placedSomething;
    }

    /// <summary>
    ///   Helper for AddOrganelle
    /// </summary>
    private void PlaceIfPossible(string organelleType, int q, int r, int rotation, ref bool placed)
    {
        if (MicrobePreviewMode)
            return;

        var organelle = new OrganelleTemplate(GetOrganelleDefinition(organelleType),
            new Hex(q, r), rotation);

        if (!IsValidPlacement(organelle))
        {
            // Play Sound
            Editor.OnInvalidAction();
            return;
        }

        if (AddOrganelle(organelle))
        {
            placed = true;
        }
    }

    private bool IsValidPlacement(OrganelleTemplate organelle)
    {
        bool notPlacingCytoplasm = organelle.Definition.InternalName != "cytoplasm";

        return editedMicrobeOrganelles.CanPlaceAndIsTouching(
            organelle,
            notPlacingCytoplasm,
            notPlacingCytoplasm);
    }

    private bool AddOrganelle(OrganelleTemplate organelle)
    {
        // 1 - you put a unique organelle (means only one instance allowed) but you already have it
        // 2 - you put an organelle that requires nucleus but you don't have one
        if ((organelle.Definition.Unique && HasOrganelle(organelle.Definition)) ||
            (organelle.Definition.RequiresNucleus && !HasNucleus))
            return false;

        organelle.PlacedThisSession = true;

        var action = new MicrobeEditorAction(Editor, organelle.Definition.MPCost,
            DoOrganellePlaceAction, UndoOrganellePlaceAction, new PlacementActionData(organelle));

        EnqueueAction(action);
        return true;
    }

    /// <summary>
    ///   Finishes an organelle move
    /// </summary>
    /// <returns>True if the organelle move succeeded.</returns>
    private bool MoveOrganelle(OrganelleTemplate organelle, Hex oldLocation, Hex newLocation, int oldRotation,
        int newRotation)
    {
        // Make sure placement is valid
        if (!IsMoveTargetValid(newLocation, newRotation, organelle))
            return false;

        // If the organelle was already moved this session, added (placed) this session,
        // or not moved (but can be rotated), then moving it is free
        bool isFreeToMove = organelle.MovedThisSession || oldLocation == newLocation || organelle.PlacedThisSession;
        int cost = isFreeToMove ? 0 : Constants.ORGANELLE_MOVE_COST;

        // Too low mutation points, cancel move
        if (!isFreeToMove && Editor.MutationPoints < Constants.ORGANELLE_MOVE_COST)
        {
            CancelCurrentAction();
            Editor.OnInsufficientMP(false);
            return false;
        }

        // Don't register the action if the final location is the same as previous. This is so the player can't exploit
        // the MovedThisSession flag allowing them to freely move an organelle that was placed in another session
        // while on zero mutation points. Also it makes more sense to not count that organelle as moved either way.
        if (oldLocation == newLocation)
        {
            CancelCurrentAction();

            // Assume this is a successful move (some operation in the above call may be repeated)
            return true;
        }

        var action = new MicrobeEditorAction(Editor, cost,
            DoOrganelleMoveAction, UndoOrganelleMoveAction,
            new MoveActionData(organelle, oldLocation, newLocation, oldRotation, newRotation));

        EnqueueAction(action);

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

        StartAutoEvoPrediction();
    }

    private void OnRigidityChanged()
    {
        UpdateRigiditySlider((int)Math.Round(Rigidity * Constants.MEMBRANE_RIGIDITY_SLIDER_TO_VALUE_RATIO));

        UpdateSpeed(CalculateSpeed());
        UpdateHitpoints(CalculateHitpoints());
    }

    public void ShowOrganelleMenu(OrganelleTemplate selectedOrganelle)
    {
        organelleMenu.SelectedOrganelle = selectedOrganelle;
        organelleMenu.ShowPopup = true;

        // Disable delete for nucleus or the last organelle.
        if (MicrobeSize < 2 || selectedOrganelle.Definition == nucleus)
        {
            organelleMenu.EnableDeleteOption = false;
        }
        else
        {
            organelleMenu.EnableDeleteOption = true;
        }

        // Move enabled only when microbe has more than one organelle
        organelleMenu.EnableMoveOption = MicrobeSize > 1;

        // Modify / upgrade possible when defined on the organelle definition
        organelleMenu.EnableModifyOption = !string.IsNullOrEmpty(selectedOrganelle.Definition.UpgradeGUI);
    }

    public override void NotifyFreebuild(bool freebuilding)
    {
        newCellButton.Disabled = !freebuilding;
    }

    /// <summary>
    ///   Lock / unlock the organelles that need a nucleus
    /// </summary>
    internal void UpdatePartsAvailability(List<OrganelleDefinition> placedUniqueOrganelleNames)
    {
        foreach (var organelle in placeablePartSelectionElements.Keys)
        {
            UpdatePartAvailability(placedUniqueOrganelleNames, organelle);
        }
    }

    internal void OnOrganelleToPlaceSelected(string organelle)
    {
        ActiveActionName = organelle;

        // Update the icon highlightings
        foreach (var element in placeablePartSelectionElements.Values)
        {
            element.Selected = element.Name == organelle;
        }
    }

    internal void UpdateMembraneButtons(string membrane)
    {
        // Update the icon highlightings
        foreach (var selection in membraneSelectionElements.Values)
        {
            selection.Selected = selection.Name == membrane;
        }
    }

    [DeserializedCallbackAllowed]
    private void OnOrganelleAdded(OrganelleTemplate organelle)
    {
        organelleDataDirty = true;
        membraneOrganellePositionsAreDirty = true;
    }

    [DeserializedCallbackAllowed]
    private void OnOrganelleRemoved(OrganelleTemplate organelle)
    {
        organelleDataDirty = true;
        membraneOrganellePositionsAreDirty = true;
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

        UpdateCellVisualization();

        StartAutoEvoPrediction();
    }

    /// <summary>
    ///   This destroys and creates again entities to represent all the currently placed organelles. Call this whenever
    ///   editedMicrobeOrganelles is changed.
    /// </summary>
    private void UpdateAlreadyPlacedVisuals()
    {
        int nextFreeHex = 0;
        int nextFreeOrganelle = 0;

        var islands = editedMicrobeOrganelles.GetIslandHexes();

        // Build the entities to show the current microbe
        foreach (var organelle in editedMicrobeOrganelles.Organelles)
        {
            foreach (var hex in organelle.RotatedHexes)
            {
                var pos = Hex.AxialToCartesian(hex + organelle.Position);

                if (nextFreeHex >= placedHexes.Count)
                {
                    // New hex needed
                    placedHexes.Add(CreateEditorHex());
                }

                var hexNode = placedHexes[nextFreeHex++];

                if (islands.Contains(organelle.Position))
                {
                    hexNode.MaterialOverride = islandMaterial;
                }
                else if (organelle.PlacedThisSession)
                {
                    hexNode.MaterialOverride = validMaterial;
                }
                else
                {
                    hexNode.MaterialOverride = oldMaterial;
                }

                // As we set the correct material, we don't need to remember to restore it anymore
                hoverOverriddenMaterials.Remove(hexNode);

                hexNode.Translation = pos;

                hexNode.Visible = !MicrobePreviewMode;
            }

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

        // Delete excess entities
        while (nextFreeHex < placedHexes.Count)
        {
            placedHexes[placedHexes.Count - 1].DetachAndQueueFree();
            placedHexes.RemoveAt(placedHexes.Count - 1);
        }

        while (nextFreeOrganelle < placedModels.Count)
        {
            placedModels[placedModels.Count - 1].DetachAndQueueFree();
            placedModels.RemoveAt(placedModels.Count - 1);
        }
    }

    private void OnMovePressed()
    {
        StartHexMove(organelleMenu.SelectedOrganelle);

        // Once an organelle move has begun, the button visibility should be updated so it becomes visible
        UpdateCancelButtonVisibility();
    }

    private void OnDeletePressed()
    {
        RemoveHex(organelleMenu.SelectedOrganelle.Position);
    }

    private void OnModifyPressed()
    {
        var upgradeGUI = organelleMenu.SelectedOrganelle.Definition.UpgradeGUI;

        if (string.IsNullOrEmpty(upgradeGUI))
        {
            GD.PrintErr("Attempted to modify an organelle with no upgrade GUI known");
            return;
        }

        organelleUpgradeGUI.OpenForOrganelle(organelleMenu.SelectedOrganelle, upgradeGUI!, Editor);
    }

    private void OnNewCellClicked()
    {
        GUICommon.Instance.PlayButtonPressSound();

        CreateNewMicrobe();
    }

    internal void UpdateRigiditySlider(int value)
    {
        rigiditySlider.Value = value;
        SetRigiditySliderTooltip(value);
    }

    public override bool CanFinishEditing(IEnumerable<EditorUserOverride> userOverrides)
    {
        var editorUserOverrides = userOverrides.ToList();
        if (!base.CanFinishEditing(editorUserOverrides))
            return false;

        if (editorUserOverrides.Contains(EditorUserOverride.NotProducingEnoughATP))
            return true;

        // Show warning popup if trying to exit with negative atp production
        if (energyBalanceInfo != null &&
            energyBalanceInfo.TotalProduction < energyBalanceInfo.TotalConsumptionStationary)
        {
            negativeAtpPopup.PopupCenteredShrink();
            return false;
        }

        return true;
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
    ///   Associates all existing cell part selections with their respective part types based on their Node names.
    /// </summary>
    private void SetupMicrobePartSelections()
    {
        var organelleSelections = GetTree().GetNodesInGroup(
            "PlaceablePartSelectionElement").Cast<MicrobePartSelection>().ToList();
        var membraneSelections = GetTree().GetNodesInGroup(
            "MembraneSelectionElement").Cast<MicrobePartSelection>().ToList();

        foreach (var entry in organelleSelections)
        {
            // Special case with registering the tooltip here for item with no associated organelle
            entry.RegisterToolTipForControl(entry.Name, "organelleSelection");

            if (!SimulationParameters.Instance.DoesOrganelleExist(entry.Name))
            {
                entry.Locked = true;
                continue;
            }

            var organelle = SimulationParameters.Instance.GetOrganelleType(entry.Name);

            // Only add items with valid organelles to dictionary
            placeablePartSelectionElements.Add(organelle, entry);

            entry.Connect(
                nameof(MicrobePartSelection.OnPartSelected), this, nameof(OnOrganelleToPlaceSelected));
        }

        foreach (var entry in membraneSelections)
        {
            // Special case with registering the tooltip here for item with no associated membrane
            entry.RegisterToolTipForControl(entry.Name, "membraneSelection");

            if (!SimulationParameters.Instance.DoesMembraneExist(entry.Name))
            {
                entry.Locked = true;
                continue;
            }

            var membrane = SimulationParameters.Instance.GetMembrane(entry.Name);

            // Only add items with valid membranes to dictionary
            membraneSelectionElements.Add(membrane, entry);

            entry.Connect(
                nameof(MicrobePartSelection.OnPartSelected), this, nameof(OnMembraneSelected));
        }
    }

    private void OnSpeciesNameTextChanged(string newText)
    {
        if (!Regex.IsMatch(newText, Constants.SPECIES_NAME_REGEX))
        {
            speciesNameEdit.Set("custom_colors/font_color", new Color(1.0f, 0.3f, 0.3f));
        }
        else
        {
            speciesNameEdit.Set("custom_colors/font_color", new Color(1, 1, 1));
        }

        NewName = newText;
    }

    private void OnSpeciesNameTextEntered(string newText)
    {
        // In case the text is not stored
        NewName = newText;

        // Only defocus if the name is valid to indicate invalid namings to the player
        if (Regex.IsMatch(newText, Constants.SPECIES_NAME_REGEX))
        {
            speciesNameEdit.ReleaseFocus();
        }
        else
        {
            // TODO: Make the popup appear at the top of the line edit instead of at the last mouse position
            ToolTipManager.Instance.ShowPopup(TranslationServer.Translate("INVALID_SPECIES_NAME_POPUP"), 2.5f);

            speciesNameEdit.GetNode<AnimationPlayer>("AnimationPlayer").Play("invalidSpeciesNameFlash");
        }
    }

    private void OnRandomizeSpeciesNamePressed()
    {
        GUICommon.Instance.PlayButtonPressSound();

        var nameGenerator = SimulationParameters.Instance.NameGenerator;
        var randomizedName = nameGenerator.GenerateNameSection() + " " + nameGenerator.GenerateNameSection();

        speciesNameEdit.Text = randomizedName;
        OnSpeciesNameTextChanged(randomizedName);
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
            if (entry.Definition.InternalName == "nucleus")
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
    }

    private void UpdateAutoEvoPredictionTranslations()
    {
        if (autoEvoPredictionRunSuccessful.HasValue && autoEvoPredictionRunSuccessful.Value == false)
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
            bestPatchName = patch.Key.Name;
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
            worstPatchName = patch.Key.Name;
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
                new LocalizedString(energyResult.Key.Name), playerSpeciesName));
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

    public void UpdatePatchDependentBalanceData()
    {
        // Calculate and send energy balance to the GUI
        CalculateEnergyBalanceWithOrganellesAndMembraneType(
            editedMicrobeOrganelles.Organelles, Membrane, Editor.SelectedPatch);

        CalculateCompoundBalanceInPatch(editedMicrobeOrganelles.Organelles, Editor.SelectedPatch);
    }
}
