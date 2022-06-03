using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using Newtonsoft.Json;

/// <summary>
///   Body plan editor component for making body plans from metaballs
/// </summary>
[SceneLoadedClass("res://src/late_multicellular_stage/editor/MetaballBodyEditorComponent.tscn")]
public partial class MetaballBodyEditorComponent :
    MetaballEditorComponentBase<LateMulticellularEditor, CombinedEditorAction, EditorAction, MulticellularMetaball>,
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

    [Export]
    public NodePath MetaballPopupMenuPath = null!;

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

    private MetaballPopupMenu metaballPopupMenu = null!;

    private PackedScene metaballDisplayerScene = null!;

    [JsonProperty]
    private string newName = "unset";

    [JsonProperty]
    private MetaballLayout<MulticellularMetaball> editedMetaballs = null!;

    /// <summary>
    ///   True when visuals of already placed things need to be updated
    /// </summary>
    private bool metaballDisplayDataDirty = true;

    [JsonProperty]
    private SelectionMenuTab selectedSelectionMenuTab = SelectionMenuTab.Structure;

    [Signal]
    public delegate void OnCellTypeToEditSelected(string name);

    public enum SelectionMenuTab
    {
        Structure,
        Reproduction,
        Behaviour,
    }

    [JsonIgnore]
    public override bool HasIslands =>
        throw new NotImplementedException() /*editedMetaballs.GetIslandHexes().Count > 0*/;

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

    public override void Init(LateMulticellularEditor owningEditor, bool fresh)
    {
        base.Init(owningEditor, fresh);
        behaviourEditor.Init(owningEditor, fresh);

        var newLayout = new MetaballLayout<MulticellularMetaball>(OnMetaballAdded, OnMetaballRemoved);

        if (fresh)
        {
            editedMetaballs = newLayout;
        }
        else
        {
            // We assume that the loaded save layout did not have anything weird set for the callbacks as we
            // do this rather than use SaveApplyHelpers
            foreach (var editedMicrobeOrganelle in editedMetaballs)
            {
                newLayout.Add(editedMicrobeOrganelle);
            }

            editedMetaballs = newLayout;

            // TODO: check are these checks needed
            /*if (Editor.EditedCellProperties != null)
            {*/
            UpdateGUIAfterLoadingSpecies(Editor.EditedBaseSpecies);
            UpdateArrow(false);
            /*}
            else
            {
                GD.Print("Loaded metaball editor with no cell to edit set");
            }*/
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

        metaballPopupMenu = GetNode<MetaballPopupMenu>(MetaballPopupMenuPath);
    }

    public override void OnEditorSpeciesSetup(Species species)
    {
        UpdateCellTypeSelections();

        behaviourEditor.OnEditorSpeciesSetup(species);

        foreach (var metaball in Editor.EditedSpecies.BodyLayout)
        {
            editedMetaballs.Add((MulticellularMetaball)metaball.Clone());
        }

        newName = species.FormattedName;

        UpdateGUIAfterLoadingSpecies(Editor.EditedBaseSpecies);

        UpdateArrow(false);
    }

    public override void OnFinishEditing()
    {
        var editedSpecies = Editor.EditedSpecies;

        editedSpecies.BodyLayout.Clear();

        foreach (var metaball in editedMetaballs)
        {
            editedSpecies.BodyLayout.Add((MulticellularMetaball)metaball.Clone());
        }

        editedSpecies.OnEdited();

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

        if (metaballDisplayDataDirty)
        {
            OnMetaballsChanged();
            metaballDisplayDataDirty = false;
        }

        // Show the ball that is about to be placed
        if (activeActionName != null && Editor.ShowHover)
        {
            throw new NotImplementedException();

            /*GetMouseHex(out int q, out int r);

            var effectiveSymmetry = Symmetry;

            var cellType = CellTypeFromName(activeActionName);

            if (MovingPlacedMetaball == null)
            {
                // Can place stuff at all?
                // TODO: should placementRotation be used here in some way?
                isPlacementProbablyValid = IsValidPlacement(
                    new HexWithData<CellTemplate>(new CellTemplate(cellType))
                    {
                        Position = new Hex(q, r),
                    });
            }
            else
            {
                isPlacementProbablyValid = IsMoveTargetValid(new Hex(q, r), 0, MovingPlacedMetaball);

                if (!Settings.Instance.MoveOrganellesWithSymmetry)
                    effectiveSymmetry = HexEditorSymmetry.None;
            }

            RunWithSymmetry(q, r,
                (finalQ, finalR, rotation) => RenderHighlightedMetaball(finalQ, finalR, rotation, cellType),
                effectiveSymmetry);*/
        }
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

    public void OnTissueTypeEdited(CellType changedType)
    {
        // TODO: check that undo/redo while in a different tab doesn't cause this to make unintended things visible
        UpdateAlreadyPlacedVisuals();

        UpdateCellTypeSelections();

        RegenerateCellTypeIcon(changedType);
    }

    [RunOnKeyDown("e_secondary")]
    public bool ShowMetaballOptions()
    {
        // Need to prevent this from running when not visible to not conflict in an editor with multiple tabs
        if (!Visible)
            return false;

        // Can't open popup menu while moving something
        if (MovingPlacedMetaball != null)
        {
            Editor.OnActionBlockedWhileMoving();
            return true;
        }

        throw new NotImplementedException();

        GetMouseHex(out int q, out int r);

        var metaballs = new List<MulticellularMetaball>();

        /*
        RunWithSymmetry(q, r, (symmetryQ, symmetryR, _) =>
        {
            var cell = editedMetaballs.GetElementAt(new Hex(symmetryQ, symmetryR));

            if (cell != null)
                metaballs.Add(cell);
        });*/

        if (metaballs.Count < 1)
            return true;

        ShowCellMenu(metaballs.Select(h => h).Distinct());
        return true;
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

        var cellType = CellTypeFromName(activeActionName);

        if (MouseHoverPositions == null)
            return Constants.METABALL_ADD_COST * Symmetry.PositionCount();

        var positions = MouseHoverPositions.ToList();

        var cellTemplates = positions.Select(tuple => new MulticellularMetaball(cellType)
        {
            Position = tuple.Position,
            Parent = tuple.Parent,
        }).ToList();

        // TODO: it's extremely unlikely that metaballs would overlap exactly so we can probably remove the occupancy
        // check here
        CombinedEditorAction moveOccupancies;

        if (MovingPlacedMetaball == null)
        {
            moveOccupancies = GetMultiActionWithOccupancies(positions, cellTemplates, true);
        }
        else
        {
            moveOccupancies = GetMultiActionWithOccupancies(positions.Take(1).ToList(),
                new List<MulticellularMetaball> { MovingPlacedMetaball }, false);
        }

        return Editor.WhatWouldActionsCost(moveOccupancies.Data);
    }

    protected override void LoadScenes()
    {
        base.LoadScenes();

        metaballDisplayerScene =
            GD.Load<PackedScene>("res://src/late_multicellular_stage/MulticellularMetaballDisplayer.tscn");
    }

    protected override IMetaballDisplayer<MulticellularMetaball> CreateMetaballDisplayer()
    {
        var displayer = (MulticellularMetaballDisplayer)metaballDisplayerScene.Instance();
        Editor.RootOfDynamicallySpawned.AddChild(displayer);
        return displayer;
    }

    protected override void PerformActiveAction()
    {
        AddMetaball(CellTypeFromName(activeActionName ?? throw new InvalidOperationException("no action active")));
    }

    protected override void PerformMove(int q, int r)
    {
        throw new NotImplementedException();
        /*if (!MoveCell(MovingPlacedMetaball!, MovingPlacedMetaball!.Position, new Hex(q, r), MovingPlacedMetaball.Data!.Orientation,
                placementRotation))
        {
            Editor.OnInvalidAction();
        }*/
    }

    protected override bool IsMoveTargetValid(Hex position, int rotation, MulticellularMetaball cell)
    {
        throw new NotImplementedException();

        // return editedMetaballs.CanPlace(cell);
    }

    protected override void OnCurrentActionCanceled()
    {
        editedMetaballs.Add(MovingPlacedMetaball!);
        MovingPlacedMetaball = null;
        base.OnCurrentActionCanceled();
    }

    protected override void OnMoveActionStarted()
    {
        editedMetaballs.Remove(MovingPlacedMetaball!);
    }

    protected override EditorAction? TryCreateRemoveHexAtAction(Hex location, ref int alreadyDeleted)
    {
        throw new NotImplementedException();
        /*
        var hexHere = editedMetaballs.GetElementAt(location);
        if (hexHere == null)
            return null;

        // Dont allow deletion of last cell
        if (editedMetaballs.Count - alreadyDeleted < 2)
            return null;

        ++alreadyDeleted;
        return new SingleEditorAction<CellRemoveActionData>(DoCellRemoveAction, UndoCellRemoveAction,
            new CellRemoveActionData(hexHere));*/
    }

    protected override float CalculateEditorArrowZPosition()
    {
        // The calculation falls back to 0 if there are no metaballs found in the middle "column"
        var highestPointInMiddleRows = 0.0f;

        foreach (var metaball in editedMetaballs)
        {
            // Only consider the middle 3 rows
            if (metaball.Position.x is < -3 or > 3)
                continue;

            // Get the min z-axis (highest point in the editor)
            highestPointInMiddleRows = Mathf.Min(highestPointInMiddleRows, metaball.Position.z);
        }

        return highestPointInMiddleRows;
    }

    private void UpdateGUIAfterLoadingSpecies(Species species)
    {
        GD.Print("Starting early multicellular editor with: ", editedMetaballs.Count,
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

    private void ShowCellMenu(IEnumerable<MulticellularMetaball> selectedMetaballs)
    {
        metaballPopupMenu.SelectedMetaballs = selectedMetaballs.ToList();
        metaballPopupMenu.GetActionPrice = Editor.WhatWouldActionsCost;
        metaballPopupMenu.ShowPopup = true;

        metaballPopupMenu.EnableDeleteOption = editedMetaballs.Count > 1;
        metaballPopupMenu.EnableMoveOption = editedMetaballs.Count > 1;
    }

    private void RenderHighlightedMetaball(int q, int r, int rotation, CellType cellToPlace)
    {
        if (MovingPlacedMetaball == null && activeActionName == null)
            return;

        throw new NotImplementedException();

        /*// For now a single hex represents entire cells
        RenderHoveredHex(q, r, new[] { new Hex(0, 0) }, isPlacementProbablyValid,
            out bool hadDuplicate);

        bool showModel = !hadDuplicate;

        if (showModel)
        {
            var cartesianPosition = Hex.AxialToCartesian(new Hex(q, r));

            var modelHolder = hoverModels[usedHoverModel++];

            
            ShowCellTypeInModelHolder(modelHolder, cellToPlace, cartesianPosition, rotation);

            modelHolder.Visible = true;
        }*/
    }

    /// <summary>
    ///   Places a cell of the specified type under the cursor and also applies symmetry to place multiple
    /// </summary>
    /// <returns>True when at least one hex got placed</returns>
    private bool AddMetaball(CellType cellType)
    {
        GetMouseHex(out int q, out int r);

        var placementActions = new List<EditorAction>();

        // For symmetrically placed cells keep track of where we already placed something
        var usedHexes = new HashSet<Hex>();

        RunWithSymmetry(q, r,
            (attemptQ, attemptR, rotation) =>
            {
                var hex = new Hex(attemptQ, attemptR);

                if (usedHexes.Contains(hex))
                {
                    // Duplicate with already placed
                    return;
                }

                throw new NotImplementedException();
                /*var placed = CreatePlaceActionIfPossible(cellType, attemptQ, attemptR, rotation);

                if (placed != null)
                {
                    placementActions.Add(placed);

                    usedHexes.Add(hex);
                }*/
            });

        if (placementActions.Count < 1)
            return false;

        var multiAction = new CombinedEditorAction(placementActions);

        return EnqueueAction(multiAction);
    }

    /// <summary>
    ///   Helper for AddMetaball
    /// </summary>
    private EditorAction? CreatePlaceActionIfPossible(CellType cellType, Vector3 position, float size,
        MulticellularMetaball parent)
    {
        var metaball = new MulticellularMetaball(cellType)
        {
            Position = position,
            Parent = parent,
            Size = size,
        };

        if (!IsValidPlacement(metaball))
        {
            // Play Sound
            Editor.OnInvalidAction();
            return null;
        }

        return CreateAddCellAction(metaball, parent);
    }

    private bool IsValidPlacement(MulticellularMetaball metaball)
    {
        // TODO: in the future we might want to prevent metaballs from overlapping too much, for now just check it has
        // a parent
        return metaball.Parent != null;
    }

    private EditorAction CreateAddCellAction(MulticellularMetaball metaball, MulticellularMetaball parent)
    {
        return new SingleEditorAction<MetaballPlacementActionData<MulticellularMetaball>>(DoCellPlaceAction,
            UndoCellPlaceAction,
            new MetaballPlacementActionData<MulticellularMetaball>(metaball));
    }

    /// <summary>
    ///   See: <see cref="CellEditorComponent.GetOccupancies"/>
    /// </summary>
    private IEnumerable<(Hex Hex, HexWithData<CellTemplate> Cell, int Orientation, bool Occupied)> GetOccupancies(
        List<(Hex Hex, int Orientation)> hexes, List<HexWithData<CellTemplate>> cells)
    {
        var cellPositions = new List<(Hex Hex, HexWithData<CellTemplate> Cell, int Orientation, bool Occupied)>();
        for (var i = 0; i < hexes.Count; i++)
        {
            var (hex, orientation) = hexes[i];
            var cell = cells[i];
            var oldCell = cellPositions.FirstOrDefault(p => p.Hex == hex);
            bool occupied = oldCell != default;

            cellPositions.Add((hex, cell, orientation, occupied));
        }

        return cellPositions;
    }

    private CombinedEditorAction GetMultiActionWithOccupancies(
        List<(Vector3 Position, MulticellularMetaball? Parent)> hexes,
        List<MulticellularMetaball> cells, bool moving)
    {
        throw new NotImplementedException();
        /*
        var moveActionData = new List<EditorAction>();
        foreach (var (hex, cell, orientation, occupied) in GetOccupancies(hexes, cells))
        {
            EditorAction action;
            if (occupied)
            {
                action = new SingleEditorAction<CellRemoveActionData>(DoCellRemoveAction,
                    UndoCellRemoveAction, new CellRemoveActionData(cell));
            }
            else
            {
                if (moving)
                {
                    var data = new CellMoveActionData(cell, cell.Position, hex, cell.Data!.Orientation,
                        orientation);
                    action = new SingleEditorAction<CellMoveActionData>(DoCellMoveAction,
                        UndoCellMoveAction, data);
                }
                else
                {
                    action = new SingleEditorAction<CellPlacementActionData>(DoCellPlaceAction,
                        UndoCellPlaceAction, new CellPlacementActionData(cell, hex, orientation));
                }
            }

            moveActionData.Add(action);
        }

        return new CombinedEditorAction(moveActionData);
        */
    }

    private bool MoveCell(HexWithData<CellTemplate> cell, Hex oldLocation, Hex newLocation, int oldRotation,
        int newRotation)
    {
        throw new NotImplementedException();

        /*// Make sure placement is valid
        if (!IsMoveTargetValid(newLocation, newRotation, cell))
            return false;

        var multiAction = GetMultiActionWithOccupancies(
            new List<(Hex Hex, int Orientation)> { (newLocation, newRotation) },
            new List<HexWithData<CellTemplate>> { cell },
            true);

        // Too low mutation points, cancel move
        if (Editor.MutationPoints < Editor.WhatWouldActionsCost(multiAction.Data))
        {
            CancelCurrentAction();
            Editor.OnInsufficientMP(false);
            return false;
        }

        EnqueueAction(multiAction);

        // It's assumed that the above enqueue can't fail, otherwise the reference to MovingPlacedMetaball may be
        // permanently lost (as the code that calls this assumes it's safe to set MovingPlacedMetaball to null
        // when we return true)
        return true;*/
    }

    private void OnMovePressed()
    {
        if (Settings.Instance.MoveOrganellesWithSymmetry.Value)
        {
            // Start moving the cells symmetrical to the clicked cell.
            StartHexMoveWithSymmetry(metaballPopupMenu.SelectedMetaballs);
        }
        else
        {
            StartHexMove(metaballPopupMenu.SelectedMetaballs.First());
        }

        // Once an cell move has begun, the button visibility should be updated so it becomes visible
        UpdateCancelButtonVisibility();
    }

    private void OnDeletePressed()
    {
        throw new NotImplementedException();

        /*int alreadyDeleted = 0;
        var action =
            new CombinedEditorAction(metaballPopupMenu.SelectedMetaballs
                .Select(o => TryCreateRemoveHexAtAction(o.Position, ref alreadyDeleted)).WhereNotNull());
        EnqueueAction(action);*/
    }

    private void OnModifyPressed()
    {
        EmitSignal(nameof(OnCellTypeToEditSelected), metaballPopupMenu.SelectedMetaballs.First().CellType.TypeName);
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

    private void OnMetaballsChanged()
    {
        UpdateAlreadyPlacedVisuals();

        UpdateArrow();
    }

    /// <summary>
    ///   This updates the metaball displayer that is used to show the currently placed metaballs in the edited layout
    /// </summary>
    private void UpdateAlreadyPlacedVisuals()
    {
        throw new NotImplementedException();

        /*var islands = editedMetaballs.GetIslandHexes();

        // Build the entities to show the current microbe
        UpdateAlreadyPlacedHexes(
            editedMetaballs.Select(o =>
                (o.Position, new[] { new Hex(0, 0) }.AsEnumerable(), Editor.HexPlacedThisSession(o))), islands);

        int nextFreeCell = 0;

        foreach (var hexWithData in editedMetaballs)
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
        }*/
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
        if (editedMetaballs.Any(c => c.CellType == type))
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
