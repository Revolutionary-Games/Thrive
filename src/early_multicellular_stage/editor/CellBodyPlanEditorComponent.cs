using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using Newtonsoft.Json;

/// <summary>
///   Body plan editor component for making body plans from hexes (that represent cells)
/// </summary>
[SceneLoadedClass("res://src/early_multicellular_stage/editor/CellBodyPlanEditorComponent.tscn")]
public partial class CellBodyPlanEditorComponent :
    HexEditorComponentBase<EarlyMulticellularEditor, CombinedEditorAction, EditorAction, CellTemplate>,
    IGodotEarlyNodeResolve
{
    [Export]
    public NodePath StructureTabButtonPath = null!;

    [Export]
    public NodePath ReproductionTabButtonPath = null!;

    [Export]
    public NodePath BehaviourTabButtonPath = null!;

    [Export]
    public NodePath StructureTabPath = null!;

    [Export]
    public NodePath ReproductionTabPath = null!;

    [Export]
    public NodePath BehaviourTabPath = null!;

    [Export]
    public NodePath CellTypeSelectionListPath = null!;

    [Export]
    public NodePath ModifyTypeButtonPath = null!;

    [Export]
    public NodePath DeleteTypeButtonPath = null!;

    [Export]
    public NodePath DuplicateTypeButtonPath = null!;

    [Export]
    public NodePath CannotDeleteInUseTypeDialogPath = null!;

    [Export]
    public NodePath DuplicateCellTypeDialogPath = null!;

    [Export]
    public NodePath DuplicateCellTypeNamePath = null!;

    private static Vector3 microbeModelOffset = new(0, -0.1f, 0);

    private readonly Dictionary<string, CellTypeSelection> cellTypeSelectionButtons = new();

    // Selection menu tab selector buttons
    private Button structureTabButton = null!;
    private Button reproductionTabButton = null!;
    private Button behaviourTabButton = null!;

    private PanelContainer structureTab = null!;
    private PanelContainer reproductionTab = null!;

    [JsonProperty]
    [AssignOnlyChildItemsOnDeserialize]
    private BehaviourEditorSubComponent behaviourEditor = null!;

    private CollapsibleList cellTypeSelectionList = null!;

    private Button modifyTypeButton = null!;

    private Button deleteTypeButton = null!;

    private Button duplicateTypeButton = null!;

    private CustomDialog cannotDeleteInUseTypeDialog = null!;

    private CustomDialog duplicateCellTypeDialog = null!;

    private LineEdit duplicateCellTypeName = null!;

    private PackedScene cellTypeSelectionButtonScene = null!;

    private ButtonGroup cellTypeButtonGroup = new();

    private PackedScene microbeScene = null!;

    // Microbe scale applies done with 3 frame delay (that's why there are multiple list variables)
    private List<Microbe> pendingScaleApplies = new();
    private List<Microbe> nextFrameScaleApplies = new();
    private List<Microbe> thisFrameScaleApplies = new();

    [JsonProperty]
    private string newName = "unset";

    [JsonProperty]
    private IndividualHexLayout<CellTemplate> editedMicrobeCells = null!;

    /// <summary>
    ///   True when visuals of already placed things need to be updated
    /// </summary>
    private bool organelleDataDirty = true;

    [JsonProperty]
    private SelectionMenuTab selectedSelectionMenuTab = SelectionMenuTab.Structure;

    private bool forceUpdateCellGraphics;

    [Signal]
    public delegate void OnCellTypeToEditSelected(string name);

    public enum SelectionMenuTab
    {
        Structure,
        Reproduction,
        Behaviour,
    }

    [JsonIgnore]
    public override bool HasIslands => editedMicrobeCells.GetIslandHexes().Count > 0;

    [JsonIgnore]
    public bool NodeReferencesResolved { get; private set; }

    protected override bool ForceHideHover => false;

    public override void _Ready()
    {
        base._Ready();

        ResolveNodeReferences();

        cellTypeSelectionButtonScene =
            GD.Load<PackedScene>("res://src/early_multicellular_stage/editor/CellTypeSelection.tscn");

        ApplySelectionMenuTab();

        RegisterTooltips();
    }

    public override void Init(EarlyMulticellularEditor owningEditor, bool fresh)
    {
        base.Init(owningEditor, fresh);
        behaviourEditor.Init(owningEditor, fresh);

        var newLayout = new IndividualHexLayout<CellTemplate>(OnCellAdded, OnCellRemoved);

        if (fresh)
        {
            editedMicrobeCells = newLayout;
        }
        else
        {
            // We assume that the loaded save layout did not have anything weird set for the callbacks as we
            // do this rather than use SaveApplyHelpers
            foreach (var editedMicrobeOrganelle in editedMicrobeCells)
            {
                newLayout.Add(editedMicrobeOrganelle);
            }

            editedMicrobeCells = newLayout;

            if (Editor.EditedCellProperties != null)
            {
                UpdateGUIAfterLoadingSpecies(Editor.EditedBaseSpecies);
                UpdateArrow(false);
            }
            else
            {
                GD.Print("Loaded cell editor with no cell to edit set");
            }
        }

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

        reproductionTab = GetNode<PanelContainer>(ReproductionTabPath);
        reproductionTabButton = GetNode<Button>(ReproductionTabButtonPath);

        behaviourTabButton = GetNode<Button>(BehaviourTabButtonPath);
        behaviourEditor = GetNode<BehaviourEditorSubComponent>(BehaviourTabPath);

        cellTypeSelectionList = GetNode<CollapsibleList>(CellTypeSelectionListPath);

        modifyTypeButton = GetNode<Button>(ModifyTypeButtonPath);

        deleteTypeButton = GetNode<Button>(DeleteTypeButtonPath);

        duplicateTypeButton = GetNode<Button>(DuplicateTypeButtonPath);

        cannotDeleteInUseTypeDialog = GetNode<CustomDialog>(CannotDeleteInUseTypeDialogPath);

        duplicateCellTypeDialog = GetNode<CustomDialog>(DuplicateCellTypeDialogPath);

        duplicateCellTypeName = GetNode<LineEdit>(DuplicateCellTypeNamePath);
    }

    public override void OnEditorSpeciesSetup(Species species)
    {
        UpdateCellTypeSelections();

        behaviourEditor.OnEditorSpeciesSetup(species);

        // Undo the transformation that happens in OnFinishEditing to make the final layout, to go back to single hexes
        // representing each cell in the layout
        foreach (var cell in Editor.EditedSpecies.Cells)
        {
            // This doesn't copy the position to the hex yet but TryAddHexToEditedLayout does it so we are good
            var hex = new HexWithData<CellTemplate>((CellTemplate)cell.Clone());

            var originalPos = cell.Position;

            var direction = new Vector2(0, -1);

            if (originalPos != new Hex(0, 0))
            {
                direction = new Vector2(originalPos.Q, originalPos.R).Normalized();
            }

            float distance = 0;

            // Start at 0,0 and move towards the real position until an empty spot is found
            // TODO: need to make sure that this can't cause holes that the player would need to fix
            // distance is a float here to try to make the above TODO problem less likely
            while (true)
            {
                var positionVector = direction * distance;

                if (TryAddHexToEditedLayout(hex, (int)positionVector.x, (int)positionVector.y))
                    break;

                distance += 0.8f;
            }
        }

        newName = species.FormattedName;

        UpdateGUIAfterLoadingSpecies(Editor.EditedBaseSpecies);

        UpdateArrow(false);
    }

    public override void OnFinishEditing()
    {
        var editedSpecies = Editor.EditedSpecies;

        // Compute final cell layout positions and update the species
        // TODO: maybe in the future we want to switch to editing the full hex layout with the entire cells in this
        // editor so this step can be skipped
        editedSpecies.Cells.Clear();

        foreach (var hexWithData in editedMicrobeCells)
        {
            var direction = new Vector2(0, -1);

            if (hexWithData.Position != new Hex(0, 0))
            {
                direction = new Vector2(hexWithData.Position.Q, hexWithData.Position.R).Normalized();
            }

            hexWithData.Data!.Position = new Hex(0, 0);

            int distance = 0;

            while (true)
            {
                var positionVector = direction * distance;
                hexWithData.Data!.Position = new Hex((int)positionVector.x, (int)positionVector.y);

                if (editedSpecies.Cells.CanPlace(hexWithData.Data))
                {
                    editedSpecies.Cells.Add(hexWithData.Data);
                    break;
                }

                ++distance;
            }
        }

        editedSpecies.RepositionToOrigin();

        editedSpecies.UpdateInitialCompounds();

        editedSpecies.UpdateNameIfValid(newName);

        behaviourEditor.OnFinishEditing();
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

        if (organelleDataDirty)
        {
            OnOrganellesChanged();
            organelleDataDirty = false;
        }

        foreach (var microbe in thisFrameScaleApplies)
        {
            // This check is here for simplicity's sake as model display nodes can be destroyed on subsequent frames
            if (!IsInstanceValid(microbe))
                continue;

            // Scale is computed so that all the cells are the size of 1 hex when placed
            // TODO: figure out why the extra multiplier to make things smaller is needed
            microbe.OverrideScaleForPreview(1.0f / microbe.Radius * Constants.DEFAULT_HEX_SIZE *
                Constants.MULTICELLULAR_EDITOR_PREVIEW_MICROBE_SCALE_MULTIPLIER);
        }

        thisFrameScaleApplies.Clear();

        var tempList = thisFrameScaleApplies;
        thisFrameScaleApplies = nextFrameScaleApplies;
        nextFrameScaleApplies = pendingScaleApplies;
        pendingScaleApplies = tempList;

        // Show the cell that is about to be placed
        if (activeActionName != null && Editor.ShowHover)
        {
            GetMouseHex(out int q, out int r);

            var effectiveSymmetry = Symmetry;

            var cellType = CellTypeFromName(activeActionName);

            if (MovingPlacedHex == null)
            {
                // Can place stuff at all?
                // TODO: should organelleRot be used here in some way?
                isPlacementProbablyValid = IsValidPlacement(
                    new HexWithData<CellTemplate>(new CellTemplate(cellType))
                    {
                        Position = new Hex(q, r),
                    });
            }
            else
            {
                isPlacementProbablyValid = IsMoveTargetValid(new Hex(q, r), organelleRot, MovingPlacedHex);
                effectiveSymmetry = HexEditorSymmetry.None;
            }

            RunWithSymmetry(q, r,
                (finalQ, finalR, rotation) => RenderHighlightedCell(finalQ, finalR, rotation, cellType),
                effectiveSymmetry);
        }

        forceUpdateCellGraphics = false;
    }

    public override bool CanFinishEditing(IEnumerable<EditorUserOverride> userOverrides)
    {
        var editorUserOverrides = userOverrides.ToList();
        if (!base.CanFinishEditing(editorUserOverrides))
            return false;

        if (editorUserOverrides.Contains(EditorUserOverride.NotProducingEnoughATP))
            return true;

        // TODO: warning about not producing enough ATP if entire body plan would be negative

        return true;
    }

    public void OnCellTypeEdited(CellType changedType)
    {
        UpdateAlreadyPlacedVisuals();

        UpdateCellTypeSelections();

        RegenerateCellTypeIcon(changedType);

        // Update all cell graphics holders
        forceUpdateCellGraphics = true;
    }

    protected CellType CellTypeFromName(string name)
    {
        return Editor.EditedSpecies.CellTypes.First(c => c.TypeName == name);
    }

    protected override void OnTranslationsChanged()
    {
    }

    protected override int CalculateCurrentActionCost()
    {
        if (activeActionName == null || !Editor.ShowHover)
            return 0;

        var cost = CellTypeFromName(activeActionName).MPCost * Symmetry.PositionCount();

        return cost;
    }

    protected override void LoadScenes()
    {
        base.LoadScenes();

        microbeScene = GD.Load<PackedScene>("res://src/microbe_stage/Microbe.tscn");
    }

    protected override void PerformActiveAction()
    {
        AddCell(CellTypeFromName(activeActionName ?? throw new InvalidOperationException("no action active")));
    }

    protected override bool DoesActionEndInProgressAction(CombinedEditorAction action)
    {
        throw new NotImplementedException();
    }

    protected override CombinedEditorAction CreateCombinedAction(IEnumerable<EditorAction> actions)
    {
        throw new NotImplementedException();
    }

    protected override bool IsMoveTargetValid(Hex position, int rotation, CellTemplate hex)
    {
        throw new NotImplementedException();
    }

    protected override void OnMoveActionStarted()
    {
        throw new NotImplementedException();
    }

    protected override void PerformMove(int q, int r)
    {
        throw new NotImplementedException();
    }

    protected override CellTemplate? GetHexAt(Hex position)
    {
        return editedMicrobeCells.GetElementAt(position)?.Data;
    }

    protected override EditorAction? TryCreateRemoveHexActionAt(Hex location)
    {
        var hexHere = editedMicrobeCells.GetElementAt(location);
        if (hexHere == null)
            return null;

        // Dont allow deletion of last cell
        if (editedMicrobeCells.Count < 2)
            return null;

        // If it was placed this session, just refund the cost of adding it.
        // TODO: this doesn't take being placed in current editor session into account, this is instead waiting for
        // the dynamic MP changes to be made
        int cost = Constants.ORGANELLE_REMOVE_COST;

        // TODO: move to using actions
        editedMicrobeCells.Remove(hexHere);
        Editor.MutationPoints -= cost;
        if (Editor.MutationPoints < 0)
            Editor.MutationPoints = 0;

        /*
        var action = new EditorAction(Editor, cost,
            DoOrganelleRemoveAction, UndoOrganelleRemoveAction, new RemoveActionData(hexHere));

        return action*/

        return null;
    }

    protected override float CalculateEditorArrowZPosition()
    {
        // The calculation falls back to 0 if there are no hexes found in the middle 3 rows
        var highestPointInMiddleRows = 0.0f;

        // Iterate through all hexes
        foreach (var hex in editedMicrobeCells)
        {
            // Only consider the middle 3 rows
            if (hex.Position.Q is < -1 or > 1)
                continue;

            var cartesian = Hex.AxialToCartesian(hex.Position);

            // Get the min z-axis (highest point in the editor)
            highestPointInMiddleRows = Mathf.Min(highestPointInMiddleRows, cartesian.z);
        }

        return highestPointInMiddleRows - Constants.EDITOR_ARROW_OFFSET;
    }

    private void UpdateGUIAfterLoadingSpecies(Species species)
    {
        GD.Print("Starting early multicellular editor with: ", editedMicrobeCells.Count,
            " cells in the microbe");

        SetSpeciesInfo(newName,
            behaviourEditor.Behaviour ?? throw new Exception("Editor doesn't have Behaviour setup"));
    }

    private void SetSpeciesInfo(string name, BehaviourDictionary behaviour)
    {
        componentBottomLeftButtons.SetNewName(name);

        // TODO: put this call in some better place (also in CellEditorComponent)
        behaviourEditor.UpdateAllBehaviouralSliders(behaviour);
    }

    private bool TryAddHexToEditedLayout(HexWithData<CellTemplate> hex, int q, int r)
    {
        hex.Position = new Hex(q, r);
        if (editedMicrobeCells.CanPlace(hex))
        {
            editedMicrobeCells.Add(hex);
            return true;
        }

        return false;
    }

    private void RenderHighlightedCell(int q, int r, int rotation, CellType cellToPlace)
    {
        if (MovingPlacedHex == null && activeActionName == null)
            return;

        // For now a single hex represents entire cells
        RenderHoveredHex(q, r, new[] { new Hex(0, 0) }, isPlacementProbablyValid,
            out bool hadDuplicate);

        bool showModel = !hadDuplicate;

        if (showModel)
        {
            var cartesianPosition = Hex.AxialToCartesian(new Hex(q, r));

            var modelHolder = hoverModels[usedHoverModel++];

            ShowCellTypeInModelHolder(modelHolder, cellToPlace, cartesianPosition, rotation);

            modelHolder.Visible = true;
        }
    }

    /// <summary>
    ///   Places an organelle of the specified type under the cursor and also applies symmetry to
    ///   place multiple at once.
    /// </summary>
    /// <returns>True when at least one organelle got placed</returns>
    private bool AddCell(CellType cellType)
    {
        GetMouseHex(out int q, out int r);

        bool placedSomething = false;

        RunWithSymmetry(q, r,
            (attemptQ, attemptR, rotation) =>
            {
                if (PlaceIfPossible(cellType, attemptQ, attemptR, rotation))
                    placedSomething = true;
            });

        return placedSomething;
    }

    /// <summary>
    ///   Helper for AddCell
    /// </summary>
    private bool PlaceIfPossible(CellType cellType, int q, int r, int rotation)
    {
        var cell = new HexWithData<CellTemplate>(new CellTemplate(cellType, new Hex(q, r), rotation))
        {
            Position = new Hex(q, r),
        };

        if (!IsValidPlacement(cell))
        {
            // Play Sound
            Editor.OnInvalidAction();
            return false;
        }

        if (AddCell(cell))
            return true;

        return false;
    }

    private bool IsValidPlacement(HexWithData<CellTemplate> cell)
    {
        return editedMicrobeCells.CanPlaceAndIsTouching(cell);
    }

    private bool AddCell(HexWithData<CellTemplate> cell)
    {
        // TODO: editor actions
        if (Editor.MutationPoints <= 0)
        {
            Editor.OnInsufficientMP();
            return false;
        }

        Editor.MutationPoints -= cell.Data!.CellType.MPCost;
        editedMicrobeCells.Add(cell);

        /*var action = new EditorAction(Editor, organelle.Definition.MPCost,
            DoOrganellePlaceAction, UndoOrganellePlaceAction, new PlacementActionData(organelle));

        EnqueueAction(action);*/

        return true;
    }

    /// <summary>
    ///   Sets up or updates the list of buttons to select cell types to place
    /// </summary>
    private void UpdateCellTypeSelections()
    {
        // Re-use / create more buttons to hold all the cell types
        foreach (var cellType in Editor.EditedSpecies.CellTypes.OrderBy(t => t.TypeName, StringComparer.Ordinal))
        {
            if (!cellTypeSelectionButtons.TryGetValue(cellType.TypeName, out var control))
            {
                // Need new button
                control = (CellTypeSelection)cellTypeSelectionButtonScene.Instance();
                control.SelectionGroup = cellTypeButtonGroup;

                control.PartName = cellType.TypeName;
                control.CellType = cellType;
                control.Name = cellType.TypeName;

                cellTypeSelectionList.AddItem(control);
                cellTypeSelectionButtons.Add(cellType.TypeName, control);

                control.Connect(nameof(MicrobePartSelection.OnPartSelected), this, nameof(OnCellToPlaceSelected));
            }

            control.MPCost = cellType.MPCost;

            // TODO: tooltips for these
        }

        bool clearSelection = false;

        // Delete no longer needed buttons
        foreach (var key in cellTypeSelectionButtons.Keys.ToList())
        {
            if (Editor.EditedSpecies.CellTypes.All(t => t.TypeName != key))
            {
                var control = cellTypeSelectionButtons[key];
                cellTypeSelectionButtons.Remove(key);

                control.DetachAndQueueFree();

                if (activeActionName == key)
                    clearSelection = true;
            }
        }

        if (clearSelection)
            ClearSelectedAction();
    }

    private void OnCellToPlaceSelected(string cellTypeName)
    {
        if (!cellTypeSelectionButtons.TryGetValue(cellTypeName, out _))
        {
            GD.PrintErr("Attempted to select an unknown cell type");
            return;
        }

        activeActionName = cellTypeName;

        OnCurrentActionChanged();
    }

    private void OnCurrentActionChanged()
    {
        // Enable the duplicate, delete, edit buttons for the cell type
        if (!string.IsNullOrEmpty(activeActionName))
        {
            modifyTypeButton.Disabled = false;
            deleteTypeButton.Disabled = false;
            duplicateTypeButton.Disabled = false;

            if (!cellTypeSelectionButtons.TryGetValue(activeActionName!, out var cellTypeButton))
            {
                GD.PrintErr("Invalid active action for highlight update");
                return;
            }

            // Update the icon highlightings
            foreach (var element in cellTypeSelectionButtons.Values)
            {
                element.Selected = element == cellTypeButton;
            }
        }
        else
        {
            // Clear all highlights
            foreach (var element in cellTypeSelectionButtons.Values)
            {
                element.Selected = false;
            }
        }
    }

    private void ClearSelectedAction()
    {
        activeActionName = null;
        OnCurrentActionChanged();
    }

    private void OnOrganellesChanged()
    {
        UpdateAlreadyPlacedVisuals();

        UpdateArrow();
    }

    /// <summary>
    ///   This destroys and creates again entities to represent all the currently placed cells. Call this whenever
    ///   editedMicrobeCells is changed.
    /// </summary>
    private void UpdateAlreadyPlacedVisuals()
    {
        var islands = editedMicrobeCells.GetIslandHexes();

        // Build the entities to show the current microbe
        // TODO: implement placed this session flag
        UpdateAlreadyPlacedHexes(
            editedMicrobeCells.Select(o => (o.Position, new[] { new Hex(0, 0) }.AsEnumerable(), false)), islands);

        int nextFreeCell = 0;

        foreach (var hexWithData in editedMicrobeCells)
        {
            var pos = Hex.AxialToCartesian(hexWithData.Position) + microbeModelOffset;

            if (nextFreeCell >= placedModels.Count)
            {
                placedModels.Add(CreatePreviewModelHolder());
            }

            var modelHolder = placedModels[nextFreeCell++];

            ShowCellTypeInModelHolder(modelHolder, hexWithData.Data!.CellType, pos, hexWithData.Data!.Orientation);

            modelHolder.Visible = true;
        }

        while (nextFreeCell < placedModels.Count)
        {
            placedModels[placedModels.Count - 1].DetachAndQueueFree();
            placedModels.RemoveAt(placedModels.Count - 1);
        }
    }

    private void ShowCellTypeInModelHolder(SceneDisplayer modelHolder, CellType cell, Vector3 position, int orientation)
    {
        modelHolder.Transform = new Transform(Quat.Identity, position);

        var rotation = MathUtils.CreateRotationForOrganelle(1 * orientation);

        // Create a new microbe if one is not already in the model holder
        Microbe microbe;

        var newSpecies = new MicrobeSpecies(new MicrobeSpecies(0, string.Empty, string.Empty), cell);

        bool wasExisting = false;

        if (modelHolder.InstancedNode is Microbe existingMicrobe)
        {
            microbe = existingMicrobe;

            wasExisting = true;
        }
        else
        {
            microbe = (Microbe)microbeScene.Instance();
            microbe.IsForPreviewOnly = true;
        }

        // Set look direction
        microbe.LookAtPoint = position + rotation.Xform(Vector3.Forward);
        microbe.Transform = new Transform(rotation, new Vector3(0, 0, 0));

        // Skip if it is already displaying the type
        if (wasExisting && !forceUpdateCellGraphics &&
            microbe.Species.GetVisualHashCode() == newSpecies.GetVisualHashCode())
        {
            return;
        }

        // Attach to scene to initialize the microbe before the operations that need that
        modelHolder.LoadFromAlreadyLoadedNode(microbe);

        // TODO: don't reload the species if the species data would be exactly the same as before to save on
        // performance. This probably causes the bit of weird turning / flicker with placing more cells
        microbe.ApplySpecies(newSpecies);

        // Apply placeholder scale if doesn't have a scale
        if (microbe.Membrane.Scale == Vector3.One)
        {
            microbe.OverrideScaleForPreview(Constants.MULTICELLULAR_EDITOR_PREVIEW_PLACEHOLDER_SCALE);
        }

        // Scale needs to be applied some frames later so that organelle positions are sent
        pendingScaleApplies.Add(microbe);

        // TODO: render order setting for the cells? (similarly to how organelles are handled in the cell editor)
    }

    private void OnSpeciesNameChanged(string newText)
    {
        newName = newText;
    }

    private void OnTypeDuplicateStartPressed()
    {
        if (string.IsNullOrEmpty(activeActionName))
            return;

        GUICommon.Instance.PlayButtonPressSound();

        var type = CellTypeFromName(activeActionName!);

        duplicateCellTypeName.Text = type.TypeName;

        // Make sure it's shown in red initially as it is a duplicate name
        OnNewCellTypeNameChanged(type.TypeName);

        duplicateCellTypeDialog.PopupCenteredShrink();

        duplicateCellTypeName.GrabFocus();
        duplicateCellTypeName.SelectAll();
        duplicateCellTypeName.CaretPosition = type.TypeName.Length;
    }

    private void OnNewCellTypeNameChanged(string newText)
    {
        if (!IsNewCellTypeNameValid(newText))
        {
            GUICommon.MarkInputAsInvalid(duplicateCellTypeName);
        }
        else
        {
            GUICommon.MarkInputAsValid(duplicateCellTypeName);
        }
    }

    private bool IsNewCellTypeNameValid(string text)
    {
        // Name is invalid if it is empty or a duplicate
        // TODO: should this ensure the name doesn't have trailing whitespace?
        return !string.IsNullOrWhiteSpace(text) && !Editor.EditedSpecies.CellTypes.Any(c =>
            c.TypeName.Equals(text, StringComparison.InvariantCultureIgnoreCase));
    }

    private void OnNewCellTextAccepted(string text)
    {
        // Just to make sure the latest text is already available to us
        if (duplicateCellTypeName.Text != text)
            GD.PrintErr("Text not updated");

        OnNewCellTypeConfirmed();
    }

    private void OnNewCellTypeConfirmed()
    {
        var newTypeName = duplicateCellTypeName.Text;

        if (!IsNewCellTypeNameValid(newTypeName))
        {
            GD.Print("Bad name for new cell type");
            Editor.OnInvalidAction();
            return;
        }

        var type = CellTypeFromName(activeActionName!);

        // TODO: make this a reversible action
        var newType = (CellType)type.Clone();
        newType.TypeName = newTypeName;

        Editor.EditedSpecies.CellTypes.Add(newType);
        GD.Print("New cell type created: ", newType.TypeName);

        UpdateCellTypeSelections();

        duplicateCellTypeDialog.Hide();
    }

    private void OnDeleteCellTypePressed()
    {
        if (string.IsNullOrEmpty(activeActionName))
            return;

        GUICommon.Instance.PlayButtonPressSound();

        var type = CellTypeFromName(activeActionName!);

        // Disallow deleting a type that is in use currently
        if (editedMicrobeCells.Any(c => c.Data!.CellType == type))
        {
            GD.Print("Can't delete in use cell type");
            cannotDeleteInUseTypeDialog.PopupCenteredShrink();
            return;
        }

        // TODO: make a reversible action
        if (!Editor.EditedSpecies.CellTypes.Remove(type))
        {
            GD.PrintErr("Failed to delete cell type from species");
        }

        UpdateCellTypeSelections();
    }

    private void OnModifyCurrentCellTypePressed()
    {
        if (string.IsNullOrEmpty(activeActionName))
            return;

        GUICommon.Instance.PlayButtonPressSound();

        EmitSignal(nameof(OnCellTypeToEditSelected), activeActionName);
    }

    private void RegenerateCellTypeIcon(CellType type)
    {
        foreach (var entry in cellTypeSelectionButtons)
        {
            if (entry.Value.CellType == type)
            {
                entry.Value.ReportTypeChanged();
            }
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
        reproductionTab.Hide();
        behaviourEditor.Hide();

        // Show selected
        switch (selectedSelectionMenuTab)
        {
            case SelectionMenuTab.Structure:
            {
                structureTab.Show();
                structureTabButton.Pressed = true;
                break;
            }

            case SelectionMenuTab.Reproduction:
            {
                reproductionTab.Show();
                reproductionTabButton.Pressed = true;
                break;
            }

            case SelectionMenuTab.Behaviour:
            {
                behaviourEditor.Show();
                behaviourTabButton.Pressed = true;
                break;
            }

            default:
                throw new Exception("Invalid selection menu tab");
        }
    }
}
