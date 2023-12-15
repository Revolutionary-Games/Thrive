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
    HexEditorComponentBase<EarlyMulticellularEditor, CombinedEditorAction, EditorAction, HexWithData<CellTemplate>,
        EarlyMulticellularSpecies>, IGodotEarlyNodeResolve
{
    [Export]
    public NodePath? TabButtonsPath;

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
    public NodePath CellPopupMenuPath = null!;

    private static Vector3 microbeModelOffset = new(0, -0.1f, 0);

    private readonly Dictionary<string, CellTypeSelection> cellTypeSelectionButtons = new();

#pragma warning disable CA2213

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

    private CustomWindow cannotDeleteInUseTypeDialog = null!;

    private CustomWindow duplicateCellTypeDialog = null!;

    private LineEdit duplicateCellTypeName = null!;

    private PackedScene cellTypeSelectionButtonScene = null!;

    private ButtonGroup cellTypeButtonGroup = new();

    private CellPopupMenu cellPopupMenu = null!;

    private PackedScene billboardScene = null!;
#pragma warning restore CA2213

    [JsonProperty]
    private string newName = "unset";

    [JsonProperty]
    private IndividualHexLayout<CellTemplate> editedMicrobeCells = null!;

    /// <summary>
    ///   True when visuals of already placed things need to be updated
    /// </summary>
    private bool cellDataDirty = true;

    [JsonProperty]
    private SelectionMenuTab selectedSelectionMenuTab = SelectionMenuTab.Structure;

    private bool forceUpdateCellGraphics;

    [Signal]
    public delegate void OnCellTypeToEditSelected(string name, bool switchTab);

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

        billboardScene = GD.Load<PackedScene>("res://src/early_multicellular_stage/CellBillboard.tscn");

        ApplySelectionMenuTab();

        RegisterTooltips();
    }

    public override void ResolveNodeReferences()
    {
        if (NodeReferencesResolved)
            return;

        base.ResolveNodeReferences();

        NodeReferencesResolved = true;

        if (TabButtonsPath == null)
            throw new MissingExportVariableValueException();

        var tabButtons = GetNode<TabButtons>(TabButtonsPath);

        structureTab = GetNode<PanelContainer>(StructureTabPath);
        structureTabButton = GetNode<Button>(tabButtons.GetAdjustedButtonPath(TabButtonsPath, StructureTabButtonPath));

        reproductionTab = GetNode<PanelContainer>(ReproductionTabPath);
        reproductionTabButton =
            GetNode<Button>(tabButtons.GetAdjustedButtonPath(TabButtonsPath, ReproductionTabButtonPath));

        behaviourEditor = GetNode<BehaviourEditorSubComponent>(BehaviourTabPath);
        behaviourTabButton = GetNode<Button>(tabButtons.GetAdjustedButtonPath(TabButtonsPath, BehaviourTabButtonPath));

        cellTypeSelectionList = GetNode<CollapsibleList>(CellTypeSelectionListPath);

        modifyTypeButton = GetNode<Button>(ModifyTypeButtonPath);

        deleteTypeButton = GetNode<Button>(DeleteTypeButtonPath);

        duplicateTypeButton = GetNode<Button>(DuplicateTypeButtonPath);

        cannotDeleteInUseTypeDialog = GetNode<CustomWindow>(CannotDeleteInUseTypeDialogPath);

        duplicateCellTypeDialog = GetNode<CustomWindow>(DuplicateCellTypeDialogPath);

        duplicateCellTypeName = GetNode<LineEdit>(DuplicateCellTypeNamePath);

        cellPopupMenu = GetNode<CellPopupMenu>(CellPopupMenuPath);
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
                GD.Print("Loaded body plan editor with no cell to edit set");
            }
        }

        UpdateCancelButtonVisibility();
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

        if (cellDataDirty)
        {
            OnCellsChanged();
            cellDataDirty = false;
        }

        // Show the cell that is about to be placed
        if (activeActionName != null && Editor.ShowHover)
        {
            GetMouseHex(out int q, out int r);

            var effectiveSymmetry = Symmetry;

            var cellType = CellTypeFromName(activeActionName);

            if (MovingPlacedHex == null)
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
                isPlacementProbablyValid = IsMoveTargetValid(new Hex(q, r), placementRotation, MovingPlacedHex);

                if (!Settings.Instance.MoveOrganellesWithSymmetry)
                    effectiveSymmetry = HexEditorSymmetry.None;
            }

            RunWithSymmetry(q, r,
                (finalQ, finalR, rotation) => RenderHighlightedCell(finalQ, finalR, rotation, cellType),
                effectiveSymmetry);
        }
        else if (forceUpdateCellGraphics)
        {
            // Make sure all cell graphics holders are updated
            foreach (var hoverModel in hoverModels)
            {
                if (hoverModel.InstancedNode is CellBillboard billboard)
                    billboard.NotifyCellTypeMayHaveChanged();
            }
        }

        forceUpdateCellGraphics = false;
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

        // Note that for the below calculations to work all cell types need to be positioned correctly. So we need
        // to force that to happen here first. This also ensures that the skipped positioning to origin of the cell
        // editor component (that is used as a special mode in multicellular) is performed.
        foreach (var cellType in editedSpecies.CellTypes)
        {
            cellType.RepositionToOrigin();
        }

        // Compute final cell layout positions and update the species
        // TODO: maybe in the future we want to switch to editing the full hex layout with the entire cells in this
        // editor so this step can be skipped. Or another approach that keeps the shape the player worked on better
        // than this approach that can move around the cells a lot.
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

        editedSpecies.OnEdited();

        editedSpecies.UpdateNameIfValid(newName);

        behaviourEditor.OnFinishEditing();
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
        // Update all cell graphics holders
        forceUpdateCellGraphics = true;

        // This may be called while hidden from the undo/redo system
        if (Visible)
            UpdateAlreadyPlacedVisuals();

        UpdateCellTypeSelections();

        RegenerateCellTypeIcon(changedType);
    }

    /// <summary>
    ///   Show options for the cell under the cursor
    /// </summary>
    [RunOnKeyDown("e_secondary")]
    public bool ShowCellOptions()
    {
        // Need to prevent this from running when not visible to not conflict in an editor with multiple tabs
        if (!Visible)
            return false;

        // Can't open popup menu while moving something
        if (MovingPlacedHex != null)
        {
            Editor.OnActionBlockedWhileMoving();
            return true;
        }

        GetMouseHex(out int q, out int r);

        var cells = new List<HexWithData<CellTemplate>>();

        RunWithSymmetry(q, r, (symmetryQ, symmetryR, _) =>
        {
            var cell = editedMicrobeCells.GetElementAt(new Hex(symmetryQ, symmetryR));

            if (cell != null)
                cells.Add(cell);
        });

        if (cells.Count < 1)
            return true;

        ShowCellMenu(cells.Select(h => h).Distinct());
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
            return cellType.MPCost * Symmetry.PositionCount();

        var positions = MouseHoverPositions.ToList();

        var cellTemplates = positions
            .Select(h => new HexWithData<CellTemplate>(new CellTemplate(cellType, h.Hex, h.Orientation))
                { Position = h.Hex }).ToList();

        CombinedEditorAction moveOccupancies;

        if (MovingPlacedHex == null)
        {
            moveOccupancies = GetMultiActionWithOccupancies(positions, cellTemplates, false);
        }
        else
        {
            moveOccupancies = GetMultiActionWithOccupancies(positions.Take(1).ToList(),
                new List<HexWithData<CellTemplate>>
                    { MovingPlacedHex }, true);
        }

        return Editor.WhatWouldActionsCost(moveOccupancies.Data);
    }

    protected override void PerformActiveAction()
    {
        AddCell(CellTypeFromName(activeActionName ?? throw new InvalidOperationException("no action active")));
    }

    protected override void PerformMove(int q, int r)
    {
        if (!MoveCell(MovingPlacedHex!, MovingPlacedHex!.Position, new Hex(q, r), MovingPlacedHex.Data!.Orientation,
                placementRotation))
        {
            Editor.OnInvalidAction();
        }
    }

    protected override bool IsMoveTargetValid(Hex position, int rotation, HexWithData<CellTemplate> cell)
    {
        return editedMicrobeCells.CanPlace(cell);
    }

    protected override void OnCurrentActionCanceled()
    {
        editedMicrobeCells.Add(MovingPlacedHex!);
        MovingPlacedHex = null;
        base.OnCurrentActionCanceled();
    }

    protected override void OnMoveActionStarted()
    {
        editedMicrobeCells.Remove(MovingPlacedHex!);
    }

    protected override HexWithData<CellTemplate>? GetHexAt(Hex position)
    {
        return editedMicrobeCells.GetElementAt(position);
    }

    protected override EditorAction? TryCreateRemoveHexAtAction(Hex location, ref int alreadyDeleted)
    {
        var hexHere = editedMicrobeCells.GetElementAt(location);
        if (hexHere == null)
            return null;

        // Dont allow deletion of last cell
        if (editedMicrobeCells.Count - alreadyDeleted < 2)
            return null;

        ++alreadyDeleted;
        return new SingleEditorAction<CellRemoveActionData>(DoCellRemoveAction, UndoCellRemoveAction,
            new CellRemoveActionData(hexHere));
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

        return highestPointInMiddleRows;
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            if (TabButtonsPath != null)
            {
                TabButtonsPath.Dispose();
                StructureTabButtonPath.Dispose();
                ReproductionTabButtonPath.Dispose();
                BehaviourTabButtonPath.Dispose();
                StructureTabPath.Dispose();
                ReproductionTabPath.Dispose();
                BehaviourTabPath.Dispose();
                CellTypeSelectionListPath.Dispose();
                ModifyTypeButtonPath.Dispose();
                DeleteTypeButtonPath.Dispose();
                DuplicateTypeButtonPath.Dispose();
                CannotDeleteInUseTypeDialogPath.Dispose();
                DuplicateCellTypeDialogPath.Dispose();
                DuplicateCellTypeNamePath.Dispose();
                CellPopupMenuPath.Dispose();
            }
        }

        base.Dispose(disposing);
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

    private void ShowCellMenu(IEnumerable<HexWithData<CellTemplate>> selectedCells)
    {
        cellPopupMenu.SelectedCells = selectedCells.ToList();
        cellPopupMenu.GetActionPrice = Editor.WhatWouldActionsCost;
        cellPopupMenu.ShowPopup = true;

        cellPopupMenu.EnableDeleteOption = editedMicrobeCells.Count > 1;
        cellPopupMenu.EnableMoveOption = editedMicrobeCells.Count > 1;
    }

    private void RenderHighlightedCell(int q, int r, int rotation, CellType cellToPlace)
    {
        if (MovingPlacedHex == null && activeActionName == null)
            return;

        // For now a single hex represents entire cells
        RenderHoveredHex(q, r, new[] { new Hex(0, 0) }, isPlacementProbablyValid,
            out bool hadDuplicate);

        bool showModel = !hadDuplicate;

        // When force updating this has to run to make sure the cell holder has been forced to refresh so that when
        // it becomes visible it doesn't have outdated graphics on it
        if (showModel || forceUpdateCellGraphics)
        {
            var cartesianPosition = Hex.AxialToCartesian(new Hex(q, r));

            var modelHolder = hoverModels[usedHoverModel++];

            ShowCellTypeInModelHolder(modelHolder, cellToPlace, cartesianPosition, rotation);

            if (showModel)
                modelHolder.Visible = true;
        }
    }

    /// <summary>
    ///   Places a cell of the specified type under the cursor and also applies symmetry to place multiple
    /// </summary>
    /// <returns>True when at least one hex got placed</returns>
    private bool AddCell(CellType cellType)
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

                var placed = CreatePlaceActionIfPossible(cellType, attemptQ, attemptR, rotation);

                if (placed != null)
                {
                    placementActions.Add(placed);

                    usedHexes.Add(hex);
                }
            });

        if (placementActions.Count < 1)
            return false;

        var multiAction = new CombinedEditorAction(placementActions);

        return EnqueueAction(multiAction);
    }

    /// <summary>
    ///   Helper for AddCell
    /// </summary>
    private EditorAction? CreatePlaceActionIfPossible(CellType cellType, int q, int r, int rotation)
    {
        var cell = new HexWithData<CellTemplate>(new CellTemplate(cellType, new Hex(q, r), rotation))
        {
            Position = new Hex(q, r),
        };

        if (!IsValidPlacement(cell))
        {
            // Play Sound
            Editor.OnInvalidAction();
            return null;
        }

        return CreateAddCellAction(cell);
    }

    private bool IsValidPlacement(HexWithData<CellTemplate> cell)
    {
        return editedMicrobeCells.CanPlaceAndIsTouching(cell);
    }

    private EditorAction CreateAddCellAction(HexWithData<CellTemplate> cell)
    {
        return new SingleEditorAction<CellPlacementActionData>(DoCellPlaceAction, UndoCellPlaceAction,
            new CellPlacementActionData(cell));
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

    private CombinedEditorAction GetMultiActionWithOccupancies(List<(Hex Hex, int Orientation)> hexes,
        List<HexWithData<CellTemplate>> cells, bool moving)
    {
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
    }

    private bool MoveCell(HexWithData<CellTemplate> cell, Hex oldLocation, Hex newLocation, int oldRotation,
        int newRotation)
    {
        // TODO: consider allowing rotation inplace (https://github.com/Revolutionary-Games/Thrive/issues/2993)

        // Make sure placement is valid
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

        // It's assumed that the above enqueue can't fail, otherwise the reference to MovingPlacedHex may be
        // permanently lost (as the code that calls this assumes it's safe to set MovingPlacedHex to null
        // when we return true)
        return true;
    }

    private void OnMovePressed()
    {
        if (Settings.Instance.MoveOrganellesWithSymmetry.Value)
        {
            // Start moving the cells symmetrical to the clicked cell.
            StartHexMoveWithSymmetry(cellPopupMenu.SelectedCells);
        }
        else
        {
            StartHexMove(cellPopupMenu.SelectedCells.First());
        }

        // Once an cell move has begun, the button visibility should be updated so it becomes visible
        UpdateCancelButtonVisibility();
    }

    private void OnDeletePressed()
    {
        int alreadyDeleted = 0;
        var action =
            new CombinedEditorAction(cellPopupMenu.SelectedCells
                .Select(o => TryCreateRemoveHexAtAction(o.Position, ref alreadyDeleted)).WhereNotNull());
        EnqueueAction(action);
    }

    private void OnModifyPressed()
    {
        EmitSignal(nameof(OnCellTypeToEditSelected), cellPopupMenu.SelectedCells.First().Data!.CellType.TypeName, true);
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
        var isActionNull = string.IsNullOrEmpty(activeActionName);

        // These buttons should only be enabled if a cell type is selected
        modifyTypeButton.Disabled = isActionNull;
        deleteTypeButton.Disabled = isActionNull;
        duplicateTypeButton.Disabled = isActionNull;

        CellTypeSelection? cellTypeButton = null;

        if (!isActionNull &&
            !cellTypeSelectionButtons.TryGetValue(activeActionName!, out cellTypeButton))
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

    private void ClearSelectedAction()
    {
        activeActionName = null;
        OnCurrentActionChanged();

        // After clearing the selected cell, emit a signal to let the editor know
        EmitSignal(nameof(OnCellTypeToEditSelected), null, false);
    }

    private void OnCellsChanged()
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
        UpdateAlreadyPlacedHexes(
            editedMicrobeCells.Select(o => (o.Position, new[] { new Hex(0, 0) }.AsEnumerable(),
                Editor.HexPlacedThisSession<HexWithData<CellTemplate>, EarlyMulticellularSpecies>(o))), islands);

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

        CellBillboard billboard;
        bool wasExisting = false;

        // Create a new billboard if one not already there for the displayer
        if (modelHolder.InstancedNode is CellBillboard existing)
        {
            billboard = existing;
            wasExisting = true;
        }
        else
        {
            billboard = (CellBillboard)billboardScene.Instance();
        }

        // Set look direction
        billboard.Transform = new Transform(rotation, new Vector3(0, 0, 0));

        billboard.DisplayedCell = cell;

        if (forceUpdateCellGraphics && wasExisting)
        {
            billboard.NotifyCellTypeMayHaveChanged();
        }

        modelHolder.LoadFromAlreadyLoadedNode(billboard);

        // TODO: render priority setting for the cells? (similarly to how organelles are handled in the cell editor)
        // Alternatively maybe 0.01 of randomness in y-position would be fine?
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

        // This isn't absolutely necessary but makes the dialog open a bit nicer in that the same thing stays focused
        // the entire time and doesn't change due to the focus grabber a tiny bit later
        duplicateCellTypeName.GrabFocusInOpeningPopup();
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

        var data = new DuplicateDeleteCellTypeData(newType);
        var action = new SingleEditorAction<DuplicateDeleteCellTypeData>(DuplicateCellType, DeleteCellType, data);
        EnqueueAction(new CombinedEditorAction(action));

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

        var data = new DuplicateDeleteCellTypeData(type);
        var action = new SingleEditorAction<DuplicateDeleteCellTypeData>(DeleteCellType, DuplicateCellType, data);
        EnqueueAction(new CombinedEditorAction(action));
    }

    private void OnModifyCurrentCellTypePressed()
    {
        if (string.IsNullOrEmpty(activeActionName))
            return;

        GUICommon.Instance.PlayButtonPressSound();

        EmitSignal(nameof(OnCellTypeToEditSelected), activeActionName, true);
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
