using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using Newtonsoft.Json;
using Systems;

/// <summary>
///   Body plan editor component for making body plans from hexes (that represent cells)
/// </summary>
[SceneLoadedClass("res://src/multicellular_stage/editor/CellBodyPlanEditorComponent.tscn", UsesEarlyResolve = false)]
public partial class CellBodyPlanEditorComponent :
    HexEditorComponentBase<MulticellularEditor, CombinedEditorAction, EditorAction, HexWithData<CellTemplate>,
        MulticellularSpecies>
{
    private static Vector3 microbeModelOffset = new(0, -0.1f, 0);

    private readonly Dictionary<string, CellTypeSelection> cellTypeSelectionButtons = new();

    private readonly List<Hex> hexTemporaryMemory = new();
    private readonly List<Hex> hexTemporaryMemory2 = new();
    private readonly List<Hex> islandResults = new();
    private readonly HashSet<Hex> islandsWorkMemory1 = new();
    private readonly List<Hex> islandsWorkMemory2 = new();
    private readonly Queue<Hex> islandsWorkMemory3 = new();

    private readonly List<EditorUserOverride> ignoredEditorWarnings = new();

    private readonly Dictionary<Compound, float> processSpeedWorkMemory = new();

    private readonly Dictionary<CellType, int> cellTypesCount = new();

#pragma warning disable CA2213

    // Selection menu tab selector buttons
    [Export]
    private Button structureTabButton = null!;

    [Export]
    private Button reproductionTabButton = null!;

    [Export]
    private Button behaviourTabButton = null!;

    [Export]
    private PanelContainer structureTab = null!;

    [Export]
    private PanelContainer reproductionTab = null!;

    [JsonProperty]
    [AssignOnlyChildItemsOnDeserialize]
    [Export]
    private BehaviourEditorSubComponent behaviourEditor = null!;

    [Export]
    private CollapsibleList cellTypeSelectionList = null!;

    [Export]
    private Button modifyTypeButton = null!;

    [Export]
    private Button deleteTypeButton = null!;

    [Export]
    private Button duplicateTypeButton = null!;

    [Export]
    private CustomWindow cannotDeleteInUseTypeDialog = null!;

    [Export]
    private CustomWindow duplicateCellTypeDialog = null!;

    [Export]
    private LineEdit duplicateCellTypeName = null!;

    private PackedScene cellTypeSelectionButtonScene = null!;

    private ButtonGroup cellTypeButtonGroup = new();

    [Export]
    private CellPopupMenu cellPopupMenu = null!;

    private PackedScene billboardScene = null!;

    [Export]
    private OrganismStatisticsPanel organismStatisticsPanel = null!;

    [Export]
    private CustomConfirmationDialog negativeAtpPopup = null!;
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

    private EnergyBalanceInfoFull? energyBalanceInfo;

    private bool hasNegativeATPCells;

    [Signal]
    public delegate void OnCellTypeToEditSelectedEventHandler(string name, bool switchTab);

    public enum SelectionMenuTab
    {
        Structure,
        Reproduction,
        Behaviour,
    }

    [JsonIgnore]
    public override bool HasIslands =>
        editedMicrobeCells.GetIslandHexes(islandResults, islandsWorkMemory1, islandsWorkMemory2,
            islandsWorkMemory3) > 0;

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

    protected override bool ForceHideHover => false;

    public override void _Ready()
    {
        base._Ready();

        cellTypeSelectionButtonScene =
            GD.Load<PackedScene>("res://src/multicellular_stage/editor/CellTypeSelection.tscn");

        billboardScene = GD.Load<PackedScene>("res://src/multicellular_stage/CellBillboard.tscn");

        ApplySelectionMenuTab();

        RegisterTooltips();
    }

    public override void Init(MulticellularEditor owningEditor, bool fresh)
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
            var workMemory1 = new List<Hex>();
            var workMemory2 = new List<Hex>();

            foreach (var editedMicrobeOrganelle in editedMicrobeCells)
            {
                newLayout.AddFast(editedMicrobeOrganelle, workMemory1, workMemory2);
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

        organismStatisticsPanel.UpdateLightSelectionPanelVisibility(
            Editor.CurrentGame.GameWorld.WorldSettings.DayNightCycleEnabled && Editor.CurrentPatch.HasDayAndNight);

        UpdateCancelButtonVisibility();
    }

    public override void _Process(double delta)
    {
        base._Process(delta);

        if (!Visible)
            return;

        var debugOverlay = DebugOverlays.Instance;

        if (debugOverlay.PerformanceMetricsVisible)
        {
            var roughCount = Editor.RootOfDynamicallySpawned.GetChildCount();
            debugOverlay.ReportEntities(roughCount);
        }

        if (cellDataDirty)
        {
            OnCellsChanged();
            cellDataDirty = false;
        }

        // Show the cell that is about to be placed
        if (Editor.ShowHover)
        {
            GetMouseHex(out int q, out int r);

            var effectiveSymmetry = Symmetry;

            CellType? cellType = null;

            if (MovingPlacedHex == null && activeActionName != null)
            {
                cellType = CellTypeFromName(activeActionName);

                // Can place stuff at all?
                // TODO: should placementRotation be used here in some way?
                isPlacementProbablyValid = IsValidPlacement(new HexWithData<CellTemplate>(new CellTemplate(cellType))
                {
                    Position = new Hex(q, r),
                });
            }
            else if (MovingPlacedHex != null)
            {
                isPlacementProbablyValid = IsMoveTargetValid(new Hex(q, r), placementRotation, MovingPlacedHex);

                if (MovingPlacedHex.Data != null)
                {
                    cellType = MovingPlacedHex.Data.CellType;
                }
                else
                {
                    GD.PrintErr("Moving placed hex has no cell data");
                }

                if (!Settings.Instance.MoveOrganellesWithSymmetry)
                    effectiveSymmetry = HexEditorSymmetry.None;
            }

            if (cellType != null)
            {
                RunWithSymmetry(q, r,
                    (finalQ, finalR, rotation) => RenderHighlightedCell(finalQ, finalR, rotation, cellType),
                    effectiveSymmetry);
            }
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

                if (TryAddHexToEditedLayout(hex, (int)positionVector.X, (int)positionVector.Y))
                    break;

                distance += 0.8f;
            }
        }

        newName = species.FormattedName;

        UpdateGUIAfterLoadingSpecies(species);

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
                hexWithData.Data!.Position = new Hex((int)positionVector.X, (int)positionVector.Y);

                if (editedSpecies.Cells.CanPlace(hexWithData.Data, hexTemporaryMemory, hexTemporaryMemory2))
                {
                    editedSpecies.Cells.AddFast(hexWithData.Data, hexTemporaryMemory, hexTemporaryMemory2);
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

        if (IsNegativeAtpProduction() &&
            !editorUserOverrides.Contains(EditorUserOverride.NotProducingEnoughATP))
        {
            negativeAtpPopup.PopupCenteredShrink();
            return false;
        }

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

        UpdateStats();

        UpdateFinishButtonWarningVisibility();
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
            var cell = editedMicrobeCells.GetElementAt(new Hex(symmetryQ, symmetryR), hexTemporaryMemory);

            if (cell != null)
                cells.Add(cell);
        });

        if (cells.Count < 1)
            return true;

        ShowCellMenu(cells.Select(h => h).Distinct());
        return true;
    }

    public Dictionary<Compound, float> GetAdditionalCapacities(out float nominalCapacity)
    {
        return CellBodyPlanInternalCalculations.GetTotalSpecificCapacity(editedMicrobeCells.Select(o => o.Data!),
            out nominalCapacity);
    }

    public void OnCurrentPatchUpdated(Patch patch)
    {
        CalculateEnergyAndCompoundBalance(editedMicrobeCells);

        UpdateCellTypeBalances();

        organismStatisticsPanel.UpdateLightSelectionPanelVisibility(
            Editor.CurrentGame.GameWorld.WorldSettings.DayNightCycleEnabled && Editor.CurrentPatch.HasDayAndNight);
    }

    public override void OnLightLevelChanged(float dayLightFraction)
    {
        UpdateVisualLightLevel(dayLightFraction, Editor.CurrentPatch);

        CalculateEnergyAndCompoundBalance(editedMicrobeCells);

        UpdateCellTypeBalances();
    }

    protected override void RegisterTooltips()
    {
        base.RegisterTooltips();

        organismStatisticsPanel.RegisterTooltips();
    }

    protected CellType CellTypeFromName(string name)
    {
        return Editor.EditedSpecies.CellTypes.First(c => c.TypeName == name);
    }

    protected override double CalculateCurrentActionCost()
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
        if (AddCell(CellTypeFromName(activeActionName ?? throw new InvalidOperationException("no action active"))))
        {
            // Placed a cell, could trigger a tutorial or something
        }
    }

    protected override void PerformMove(int q, int r)
    {
        if (!MoveCell(MovingPlacedHex!, new Hex(q, r),
                placementRotation))
        {
            Editor.OnInvalidAction();
        }
    }

    protected override bool IsMoveTargetValid(Hex position, int rotation, HexWithData<CellTemplate> cell)
    {
        return editedMicrobeCells.CanPlace(cell, hexTemporaryMemory, hexTemporaryMemory2);
    }

    protected override void OnCurrentActionCanceled()
    {
        editedMicrobeCells.AddFast(MovingPlacedHex!, hexTemporaryMemory, hexTemporaryMemory2);
        MovingPlacedHex = null;
        base.OnCurrentActionCanceled();
    }

    protected override void OnMoveActionStarted()
    {
        editedMicrobeCells.Remove(MovingPlacedHex!);
    }

    protected override HexWithData<CellTemplate>? GetHexAt(Hex position)
    {
        return editedMicrobeCells.GetElementAt(position, hexTemporaryMemory);
    }

    protected override EditorAction? TryCreateRemoveHexAtAction(Hex location, ref int alreadyDeleted)
    {
        var hexHere = editedMicrobeCells.GetElementAt(location, hexTemporaryMemory);
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
            highestPointInMiddleRows = MathF.Min(highestPointInMiddleRows, cartesian.Z);
        }

        return highestPointInMiddleRows;
    }

    private void SetLightLevelOption(int option)
    {
        // Show selected light level
        switch ((LightLevelOption)option)
        {
            case LightLevelOption.Day:
            {
                Editor.DayLightFraction = 1;
                break;
            }

            case LightLevelOption.Night:
            {
                Editor.DayLightFraction = 0;
                break;
            }

            case LightLevelOption.Average:
            {
                Editor.DayLightFraction = Editor.CurrentGame.GameWorld.LightCycle.AverageSunlight;
                break;
            }

            case LightLevelOption.Current:
            {
                Editor.DayLightFraction = Editor.CurrentGame.GameWorld.LightCycle.DayLightFraction;
                break;
            }

            default:
                throw new Exception("Invalid light level option");
        }
    }

    private bool IsNegativeAtpProduction()
    {
        return hasNegativeATPCells;
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
        if (editedMicrobeCells.CanPlace(hex, hexTemporaryMemory, hexTemporaryMemory2))
        {
            editedMicrobeCells.AddFast(hex, hexTemporaryMemory, hexTemporaryMemory2);
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
        return editedMicrobeCells.CanPlaceAndIsTouching(cell, hexTemporaryMemory, hexTemporaryMemory2);
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
        for (var i = 0; i < hexes.Count; ++i)
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

    private bool MoveCell(HexWithData<CellTemplate> cell, Hex newLocation,
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
            StartHexMoveWithSymmetry(cellPopupMenu.GetSelectedThatAreStillValid(editedMicrobeCells));
        }
        else
        {
            StartHexMove(cellPopupMenu.GetSelectedThatAreStillValid(editedMicrobeCells).FirstOrDefault());
        }
    }

    private void OnDeletePressed()
    {
        int alreadyDeleted = 0;
        var targets = cellPopupMenu.GetSelectedThatAreStillValid(editedMicrobeCells)
            .Select(o => TryCreateRemoveHexAtAction(o.Position, ref alreadyDeleted)).WhereNotNull().ToList();

        if (targets.Count < 1)
        {
            GD.PrintErr("No targets found to delete");
            return;
        }

        var action = new CombinedEditorAction(targets);

        EnqueueAction(action);
    }

    private void OnModifyPressed()
    {
        // This should be fine to trigger even when the cell is no longer in the layout as the other code should
        // prevent editing invalid cell types
        EmitSignal(SignalName.OnCellTypeToEditSelected,
            cellPopupMenu.SelectedCells.First().Data!.CellType.TypeName,
            true);
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
                control = (CellTypeSelection)cellTypeSelectionButtonScene.Instantiate();
                control.SelectionGroup = cellTypeButtonGroup;

                control.PartName = cellType.TypeName;
                control.CellType = cellType;
                control.Name = cellType.TypeName;

                cellTypeSelectionList.AddItem(control);
                cellTypeSelectionButtons.Add(cellType.TypeName, control);

                control.Connect(MicrobePartSelection.SignalName.OnPartSelected,
                    new Callable(this, nameof(OnCellToPlaceSelected)));
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

    private void UpdateCellTypeBalances()
    {
        float maxValue = 0.0f;

        var maximumMovementDirection =
            MicrobeInternalCalculations.MaximumSpeedDirection(editedMicrobeCells[0].Data!.Organelles);

        var conditionsData = new BiomeResourceLimiterAdapter(organismStatisticsPanel.ResourceLimitingMode,
            Editor.CurrentPatch.Biome);

        UpdateCellTypesCounts();
        hasNegativeATPCells = false;

        // TODO: environmental tolerances for multicellular
        var environmentalTolerances = new ResolvedMicrobeTolerances
        {
            HealthModifier = 1,
            OsmoregulationModifier = 1,
            ProcessSpeedModifier = 1,
        };

        foreach (var button in cellTypeSelectionButtons)
        {
            var energyBalance = new EnergyBalanceInfoSimple();

            ProcessSystem.ComputeEnergyBalanceSimple(button.Value.CellType.Organelles, conditionsData,
                environmentalTolerances, button.Value.CellType.MembraneType, maximumMovementDirection,
                organismStatisticsPanel.CalculateBalancesWhenMoving, true, Editor.CurrentGame.GameWorld.WorldSettings,
                organismStatisticsPanel.CompoundAmountType, null, energyBalance);

            button.Value.SetEnergyBalanceValues(energyBalance.TotalProduction, energyBalance.TotalConsumption);

            if (energyBalance.TotalProduction > maxValue)
                maxValue = energyBalance.TotalProduction;

            if (energyBalance.TotalConsumption > maxValue)
                maxValue = energyBalance.TotalConsumption;

            cellTypesCount.TryGetValue(button.Value.CellType, out var count);

            if (energyBalance.TotalConsumption > energyBalance.TotalProduction
                && count > 0)
            {
                // This cell is present in the microbe and has a negative energy balance
                hasNegativeATPCells = true;
            }
        }

        foreach (var button in cellTypeSelectionButtons)
        {
            button.Value.MaxEnergyValue = maxValue;
        }
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
        EmitSignal(SignalName.OnCellTypeToEditSelected, default(Variant), false);
    }

    private void OnEnergyBalanceOptionsChanged()
    {
        CalculateEnergyAndCompoundBalance(editedMicrobeCells);

        UpdateCellTypeBalances();

        UpdateFinishButtonWarningVisibility();
    }

    private void OnResourceLimitingModeChanged()
    {
        CalculateEnergyAndCompoundBalance(editedMicrobeCells);

        UpdateCellTypeBalances();

        UpdateFinishButtonWarningVisibility();
    }

    private void OnCellsChanged()
    {
        UpdateAlreadyPlacedVisuals();

        UpdateStats();

        UpdateArrow();

        UpdateFinishButtonWarningVisibility();
    }

    private void UpdateStats()
    {
        organismStatisticsPanel.UpdateStorage(GetAdditionalCapacities(out var nominalCapacity), nominalCapacity);
        organismStatisticsPanel.UpdateSpeed(CellBodyPlanInternalCalculations.CalculateSpeed(editedMicrobeCells));
        organismStatisticsPanel.UpdateRotationSpeed(
            CellBodyPlanInternalCalculations.CalculateRotationSpeed(editedMicrobeCells));
        var (ammoniaCost, phosphatesCost) =
            CellBodyPlanInternalCalculations.CalculateOrganellesCost(editedMicrobeCells);
        organismStatisticsPanel.UpdateOrganellesCost(ammoniaCost, phosphatesCost);

        CalculateEnergyAndCompoundBalance(editedMicrobeCells);

        UpdateCellTypeBalances();
    }

    /// <summary>
    ///   Calculates the energy balance and compound balance for a colony
    /// </summary>
    private void CalculateEnergyAndCompoundBalance(IReadOnlyList<HexWithData<CellTemplate>> cells,
        BiomeConditions? biome = null)
    {
        biome ??= Editor.CurrentPatch.Biome;

        bool moving = organismStatisticsPanel.CalculateBalancesWhenMoving;

        IBiomeConditions conditionsData = biome;

        if (organismStatisticsPanel.ResourceLimitingMode != ResourceLimitingMode.AllResources)
        {
            conditionsData = new BiomeResourceLimiterAdapter(organismStatisticsPanel.ResourceLimitingMode,
                conditionsData);
        }

        var energyBalance = new EnergyBalanceInfoFull();
        energyBalance.SetupTrackingForRequiredCompounds();

        // Cells can't individually move in the body plan, so this probably makes sense
        var maximumMovementDirection =
            MicrobeInternalCalculations.MaximumSpeedDirection(cells[0].Data!.CellType.Organelles);

        // TODO: environmental tolerances for multicellular
        var environmentalTolerances = new ResolvedMicrobeTolerances
        {
            HealthModifier = 1,
            OsmoregulationModifier = 1,
            ProcessSpeedModifier = 1,
        };

        // TODO: improve performance by calculating the balance per cell type
        foreach (var hex in cells)
        {
            ProcessSystem.ComputeEnergyBalanceFull(hex.Data!.Organelles, conditionsData, environmentalTolerances,
                hex.Data.MembraneType,
                maximumMovementDirection, moving, true, Editor.CurrentGame.GameWorld.WorldSettings,
                organismStatisticsPanel.CompoundAmountType, null, energyBalance);
        }

        energyBalanceInfo = energyBalance;

        // Passing those variables by refs to the following functions to reuse them
        float nominalStorage = 0;
        Dictionary<Compound, float>? specificStorages = null;

        // This takes balanceType into account as well, https://github.com/Revolutionary-Games/Thrive/issues/2068
        var compoundBalanceData =
            CalculateCompoundBalanceWithMethod(organismStatisticsPanel.BalanceDisplayType,
                organismStatisticsPanel.CompoundAmountType,
                cells, conditionsData, energyBalance,
                ref specificStorages, ref nominalStorage);

        UpdateCompoundBalances(compoundBalanceData);

        // TODO: should this skip on being affected by the resource limited?
        var nightBalanceData = CalculateCompoundBalanceWithMethod(organismStatisticsPanel.BalanceDisplayType,
            CompoundAmountType.Minimum, cells, conditionsData, energyBalance, ref specificStorages,
            ref nominalStorage);

        UpdateCompoundLastingTimes(compoundBalanceData, nightBalanceData, nominalStorage,
            specificStorages ?? throw new Exception("Special storages should have been calculated"));

        HandleProcessList(cells, energyBalance, conditionsData);
    }

    private Dictionary<Compound, CompoundBalance> CalculateCompoundBalanceWithMethod(BalanceDisplayType calculationType,
        CompoundAmountType amountType,
        IReadOnlyList<HexWithData<CellTemplate>> cells, IBiomeConditions biome, EnergyBalanceInfoFull energyBalance,
        ref Dictionary<Compound, float>? specificStorages, ref float nominalStorage)
    {
        // TODO: environmental tolerances for multicellular
        var environmentalTolerances = new ResolvedMicrobeTolerances
        {
            HealthModifier = 1,
            OsmoregulationModifier = 1,
            ProcessSpeedModifier = 1,
        };

        Dictionary<Compound, CompoundBalance> compoundBalanceData = new();
        foreach (var cell in cells)
        {
            switch (calculationType)
            {
                case BalanceDisplayType.MaxSpeed:
                    ProcessSystem.ComputeCompoundBalance(cell.Data!.Organelles, biome, environmentalTolerances,
                        amountType, true, compoundBalanceData);
                    break;
                case BalanceDisplayType.EnergyEquilibrium:
                    ProcessSystem.ComputeCompoundBalanceAtEquilibrium(cell.Data!.Organelles, biome,
                        environmentalTolerances, amountType, energyBalance, compoundBalanceData);
                    break;
                default:
                    GD.PrintErr("Unknown compound balance type: ", organismStatisticsPanel.BalanceDisplayType);
                    goto case BalanceDisplayType.EnergyEquilibrium;
            }
        }

        specificStorages ??= CellBodyPlanInternalCalculations.GetTotalSpecificCapacity(cells.Select(o => o.Data!),
            out nominalStorage);

        return ProcessSystem.ComputeCompoundFillTimes(compoundBalanceData, nominalStorage, specificStorages);
    }

    private void UpdateCellTypesCounts()
    {
        cellTypesCount.Clear();

        foreach (var cell in editedMicrobeCells)
        {
            var type = cell.Data!.CellType;

            cellTypesCount.TryGetValue(type, out var count);
            cellTypesCount[type] = count + 1;
        }
    }

    /// <summary>
    ///   This destroys and creates again entities to represent all the currently placed cells. Call this whenever
    ///   editedMicrobeCells is changed.
    /// </summary>
    private void UpdateAlreadyPlacedVisuals()
    {
        editedMicrobeCells.GetIslandHexes(islandResults, islandsWorkMemory1, islandsWorkMemory2, islandsWorkMemory3);

        // Build the entities to show the current microbe
        IReadOnlyList<Hex> positionZeroList = [new(0, 0)];
        UpdateAlreadyPlacedHexes(editedMicrobeCells.Select(o => (o.Position, positionZeroList,
            Editor.HexPlacedThisSession<HexWithData<CellTemplate>, MulticellularSpecies>(o))), islandResults);

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
        modelHolder.Transform = new Transform3D(Basis.Identity, position);

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
            billboard = (CellBillboard)billboardScene.Instantiate();
        }

        // Set look direction
        billboard.Transform = new Transform3D(new Basis(rotation), new Vector3(0, 0, 0));

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
        duplicateCellTypeName.CaretColumn = type.TypeName.Length;
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

        EmitSignal(SignalName.OnCellTypeToEditSelected, activeActionName, true);
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

        // Don't change the tab if there's an in-progress action
        if (CanCancelAction)
        {
            ToolTipManager.Instance.ShowPopup(Localization.Translate("TAB_CHANGE_BLOCKED_WHILE_ACTION_IN_PROGRESS"),
                1.5f);

            ApplySelectionMenuTab();

            return;
        }

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
                structureTabButton.ButtonPressed = true;
                break;
            }

            case SelectionMenuTab.Reproduction:
            {
                reproductionTab.Show();
                reproductionTabButton.ButtonPressed = true;
                break;
            }

            case SelectionMenuTab.Behaviour:
            {
                behaviourEditor.Show();
                behaviourTabButton.ButtonPressed = true;
                break;
            }

            default:
                throw new Exception("Invalid selection menu tab");
        }
    }
}
