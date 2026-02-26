using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using SharedBase.Archive;
using Systems;

/// <summary>
///   Body plan editor component for making body plans from hexes (that represent cells)
/// </summary>
public partial class CellBodyPlanEditorComponent :
    HexEditorComponentBase<MulticellularEditor, CombinedEditorAction, EditorAction, HexWithData<CellTemplate>,
        MulticellularSpecies>, IArchiveUpdatable
{
    public const ushort SERIALIZATION_VERSION = 4;

    [Export]
    public int MaxToleranceWarnings = 3;

    private static Vector3 microbeModelOffset = new(0, -0.1f, 0);

    private readonly Dictionary<string, CellTypeSelection> cellTypeSelectionButtons = new();

    private readonly IndividualHexLayout<CellTemplate> tempFreshlyUpdatedCells = new();

    private readonly List<Hex> hexTemporaryMemory = new();
    private readonly List<Hex> hexTemporaryMemory2 = new();
    private readonly List<Hex> islandResults = new();
    private readonly HashSet<Hex> islandsWorkMemory1 = new();
    private readonly List<Hex> islandsWorkMemory2 = new();
    private readonly Queue<Hex> islandsWorkMemory3 = new();

    private readonly List<EditorUserOverride> ignoredEditorWarnings = new();

    private readonly Dictionary<Compound, float> processSpeedWorkMemory = new();

    private readonly Dictionary<CellType, int> cellTypesCount = new();

    /// <summary>
    ///   Stores cells that end up being disconnected from the colony because of growth order
    /// </summary>
    private readonly HashSet<Hex> wrongGrowthOrderCells = new();

#pragma warning disable CA2213

    // Selection menu tab selector buttons
    [Export]
    private Button structureTabButton = null!;

    [Export]
    private Button reproductionTabButton = null!;

    [Export]
    private Button behaviourTabButton = null!;

    [Export]
    private Button growthOrderTabButton = null!;

    [Export]
    private Button tolerancesTabButton = null!;

    [Export]
    private PanelContainer structureTab = null!;

    [Export]
    private PanelContainer reproductionTab = null!;

    [Export]
    private BehaviourEditorSubComponent behaviourEditor = null!;

    [Export]
    private PanelContainer growthOrderTab = null!;

    [Export]
    private GrowthOrderPicker growthOrderGUI = null!;

    [Export]
    private CheckBox showGrowthOrderCoordinates = null!;

    [Export]
    private TolerancesEditorSubComponent tolerancesEditor = null!;

    [Export]
    private PanelContainer toleranceTab = null!;

    [Export]
    private Container toleranceWarningContainer = null!;

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

    private PackedScene cellTypeTooltipButtonScene = null!;

    private ButtonGroup cellTypeButtonGroup = new();

    [Export]
    private CellPopupMenu cellPopupMenu = null!;

    private PackedScene billboardScene = null!;

    [Export]
    private OrganismStatisticsPanel organismStatisticsPanel = null!;

    [Export]
    private CustomConfirmationDialog negativeAtpPopup = null!;

    [Export]
    private CustomConfirmationDialog wrongGrowthOrderPopup = null!;

    [Export]
    private LabelSettings toleranceWarningsFont = null!;
#pragma warning restore CA2213

    private string newName = "unset";

    private IndividualHexLayout<CellTemplate> editedMicrobeCells = null!;

    private List<IReadOnlyOrganelleTemplate> tempAllOrganelles = new();
    private List<TweakedProcess> tempAllProcesses = new();
    private Dictionary<OrganelleDefinition, int> tempMemory3 = new();

    /// <summary>
    ///   True, when visuals of already placed things need to be updated
    /// </summary>
    private bool cellDataDirty = true;

    private bool refreshTolerancesWarnings = true;

    private SelectionMenuTab selectedSelectionMenuTab = SelectionMenuTab.Structure;

    private bool forceUpdateCellGraphics;

    private bool hasNegativeATPCells;

    private bool showGrowthOrderNumbers;

    private EnergyBalanceInfoFull? energyBalanceInfo;

    [Signal]
    public delegate void OnCellTypeToEditSelectedEventHandler(string name, bool switchTab);

    public enum SelectionMenuTab
    {
        Structure,
        Reproduction,
        Behaviour,
        GrowthOrder,
        Tolerance,
    }

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

            if (wrongGrowthOrderCells.Count > 0)
                return true;

            return false;
        }
    }

    /// <summary>
    ///   When not null, this is used to retrieve updated visuals during editing a species rather than reading the
    ///   outdated data from the species object.
    /// </summary>
    public CellTypeEditsHolder? CellTypeVisualsOverride { get; set; }

    public override ushort CurrentArchiveVersion => SERIALIZATION_VERSION;

    public override ArchiveObjectType ArchiveObjectType =>
        (ArchiveObjectType)ThriveArchiveObjectType.CellBodyPlanEditorComponent;

    public bool CanBeSpecialReference => true;

    /// <summary>
    ///   When enabled numbers are shown above the organelles to indicate their growth order
    /// </summary>
    public bool ShowGrowthOrder
    {
        get => showGrowthOrderNumbers;
        set
        {
            showGrowthOrderNumbers = value;

            UpdateGrowthOrderUI();
        }
    }

    protected override bool ShowFloatingLabels => ShowGrowthOrder;

    protected override bool ForceHideHover => false;

    public override void _Ready()
    {
        base._Ready();

        growthOrderGUI.ShowCoordinates = showGrowthOrderCoordinates.ButtonPressed;

        cellTypeSelectionButtonScene =
            GD.Load<PackedScene>("res://src/multicellular_stage/editor/CellTypeSelection.tscn");

        cellTypeTooltipButtonScene = GD.Load<PackedScene>("res://src/multicellular_stage/editor/CellTypeTooltip.tscn");

        billboardScene = GD.Load<PackedScene>("res://src/multicellular_stage/CellBillboard.tscn");

        ApplySelectionMenuTab();

        RegisterTooltips();
    }

    public override void Init(MulticellularEditor owningEditor, bool fresh)
    {
        base.Init(owningEditor, fresh);
        behaviourEditor.Init(owningEditor, fresh);
        tolerancesEditor.Init(owningEditor, fresh);

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

            foreach (var editedMicrobeOrganelle in editedMicrobeCells.AsModifiable())
            {
                newLayout.AddFast(editedMicrobeOrganelle, workMemory1, workMemory2);
            }

            editedMicrobeCells = newLayout;

            UpdateGUIAfterLoadingSpecies(Editor.EditedSpecies);
            UpdateArrow(false);

            UpdateCellTypeSelections();

            newName = Editor.EditedSpecies.FormattedName;

            tolerancesEditor.OnEditorSpeciesSetup(Editor.EditedBaseSpecies);
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

        if (refreshTolerancesWarnings)
        {
            refreshTolerancesWarnings = false;

            // These are all also affected by the environmental tolerances
            CalculateEnergyAndCompoundBalance(GetCurrentCellsWithLatestTypes());
            UpdateCellTypesSecondaryInfo();

            // Health is also affected
            UpdateStats();

            CalculateAndDisplayToleranceWarnings();
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

                var position = new Hex(q, r);

                // Can place stuff at all?
                isPlacementProbablyValid =
                    IsValidPlacement(new HexWithData<CellTemplate>(
                        new CellTemplate(cellType, position, placementRotation), position, placementRotation));
            }
            else if (MovingPlacedHex != null)
            {
                isPlacementProbablyValid = IsMoveTargetValid(new Hex(q, r), placementRotation, MovingPlacedHex);

                if (MovingPlacedHex.Data != null)
                {
                    cellType = MovingPlacedHex.Data.ModifiableCellType;
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
                HashSet<(Hex Hex, int Orientation)> hoveredHexes = new();

                /*if (!componentBottomLeftButtons.SymmetryEnabled)
                    effectiveSymmetry = HexEditorSymmetry.None;*/

                RunWithSymmetry(q, r,
                    (finalQ, finalR, rotation) =>
                    {
                        RenderHighlightedCell(finalQ, finalR, rotation, cellType);

                        var finalHex = new Hex(finalQ, finalR);

                        // Only add unique positions so that duplicate actions are not attempted
                        bool exists = false;
                        foreach (var existingHex in hoveredHexes)
                        {
                            if (existingHex.Hex == finalHex)
                            {
                                exists = true;
                                break;
                            }
                        }

                        if (exists)
                            return;

                        hoveredHexes.Add((finalHex, rotation));
                    },
                    effectiveSymmetry);

                MouseHoverPositions = hoveredHexes.ToList();
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

    public override void WritePropertiesToArchive(ISArchiveWriter writer)
    {
        writer.Write(SERIALIZATION_VERSION_HEX);
        base.WritePropertiesToArchive(writer);

        writer.WriteObjectProperties(behaviourEditor);
        writer.Write(newName);
        writer.WriteObject(editedMicrobeCells);
        writer.Write((int)selectedSelectionMenuTab);
        writer.WriteObjectProperties(growthOrderGUI);

        writer.WriteObjectProperties(tolerancesEditor);
    }

    public override void ReadPropertiesFromArchive(ISArchiveReader reader, ushort version)
    {
        if (version is > SERIALIZATION_VERSION or <= 0)
            throw new InvalidArchiveVersionException(version, SERIALIZATION_VERSION);

        base.ReadPropertiesFromArchive(reader, reader.ReadUInt16());

        reader.ReadObjectProperties(behaviourEditor);

        newName = reader.ReadString() ?? throw new NullArchiveObjectException();
        editedMicrobeCells = reader.ReadObject<IndividualHexLayout<CellTemplate>>();
        selectedSelectionMenuTab = (SelectionMenuTab)reader.ReadInt32();

        if (version < 2)
        {
            // Need to fix duplicated data references in older saves
            var newLayout = new IndividualHexLayout<CellTemplate>();

            foreach (var cell in editedMicrobeCells.AsModifiable())
            {
                // Fix cell references being shared in older saves
                newLayout.AddFast(cell.Clone(), hexTemporaryMemory, hexTemporaryMemory2);

                if (cell.Data != null)
                {
                    if (cell.Data.Position != cell.Position || cell.Data.Orientation != cell.Orientation)
                    {
                        GD.PrintErr("Expected edited cells in editor (even in old save) to have updated positions");
                    }
                }
            }

            if (editedMicrobeCells.Count != newLayout.Count)
                throw new Exception("Couldn't copy data correctly from old save");

            editedMicrobeCells = newLayout;
        }

        if (version >= 3)
        {
            reader.ReadObjectProperties(growthOrderGUI);
        }

        if (version >= 4)
        {
            reader.ReadObjectProperties(tolerancesEditor);
        }
    }

    public override void OnEditorSpeciesSetup(Species species)
    {
        UpdateCellTypeSelections();

        behaviourEditor.OnEditorSpeciesSetup(species);
        tolerancesEditor.OnEditorSpeciesSetup(species);

        foreach (var cell in Editor.EditedSpecies.ModifiableEditorCells.AsModifiable())
        {
            // Clone here so that we don't directly modify the original data which will cause errors with MP
            // comparisons
            editedMicrobeCells.AddFast(cell.Clone(), hexTemporaryMemory, hexTemporaryMemory2);
        }

        newName = species.FormattedName;

        UpdateGUIAfterLoadingSpecies(species);

        UpdateArrow(false);

        // Make sure initial tolerance warnings are shown
        OnTolerancesChanged(tolerancesEditor.CurrentTolerances);
    }

    public override void OnFinishEditing()
    {
        var editedSpecies = Editor.EditedSpecies;

        if (CellTypeVisualsOverride == null)
        {
            GD.PrintErr("Cell body plan doesn't have visuals holder, so something went wrong and final position " +
                "apply will fail");
        }
        else
        {
            // Apply all queued cell type edits so that we can work with fully final data here
            GD.Print("Applying cell type edits to real species data");
            CellTypeVisualsOverride.ApplyChanges();
        }

        // Note that for the below calculations to work, all cell types need to be positioned correctly. So we need
        // to force that to happen here first. This also ensures that the skipped positioning to the origin of the cell
        // editor component (that is used as a special mode in multicellular) is performed.
        foreach (var cellType in editedSpecies.ModifiableCellTypes)
        {
            cellType.RepositionToOrigin();
        }

        // Safety check against cell layouts that forever want to shift
        foreach (var cellType in editedSpecies.ModifiableCellTypes)
        {
            if (cellType.RepositionToOrigin())
            {
                GD.PrintErr("Cell type shouldn't get a second move to origin");
                LogInterceptor.ForwardCaughtError(new Exception(
                        "Detected a cell layout that infinitely shifts around the origin, this will break " +
                        "multicellular cell positioning!"),
                    "Please include a save or screenshot of your species' cell types with the report");
                break;
            }
        }

        ApplyGrowthOrderToCells();

        // Compute final cell layout positions and update the species
        // TODO: maybe in the future we want to switch to editing the full hex layout with the entire cells in this
        // editor so this step can be skipped. Or another approach that keeps the shape the player worked on better
        // than this approach that can move around the cells a lot.
        // This uses high quality as extra time spent doesn't matter here and is even important for the player species.
        MulticellularLayoutHelpers.UpdateGameplayLayout(editedSpecies.ModifiableGameplayCells,
            editedSpecies.ModifiableEditorCells, editedMicrobeCells, AlgorithmQuality.High, hexTemporaryMemory,
            hexTemporaryMemory2);

        tempFreshlyUpdatedCells.Clear();
        editedSpecies.OnEdited();

        editedSpecies.UpdateNameIfValid(newName);

        behaviourEditor.OnFinishEditing();
        tolerancesEditor.OnFinishEditing();
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

        if (wrongGrowthOrderCells.Count > 0)
        {
            wrongGrowthOrderPopup.PopupCenteredShrink();
            return false;
        }

        return true;
    }

    public void OnCellTypeEdited(CellType changedType)
    {
        // Make sure specialization is calculated
        changedType.CalculateSpecialization();

        // Update all cell graphics holders
        forceUpdateCellGraphics = true;

        // This may be called while hidden from the undo/redo system
        if (Visible)
            UpdateAlreadyPlacedVisuals();

        UpdateCellTypeSelections();

        RegenerateCellTypeIcon(changedType);

        UpdateStats();
        tolerancesEditor.OnDataTolerancesDependOnChanged();

        UpdateFinishButtonWarningVisibility();
    }

    /// <summary>
    ///   Gets the current body plan that is being edited. For calculating stats etc. based on this in other editor
    ///   parts. This also makes sure the latest cell types are applied to the layout, so this is a bit of an expensive
    ///   method to call.
    /// </summary>
    /// <returns>Current cells. Should not be edited directly.</returns>
    public IndividualHexLayout<CellTemplate> GetCurrentCellsWithLatestTypes()
    {
        tempFreshlyUpdatedCells.Clear();

        foreach (var editedMicrobeCell in editedMicrobeCells)
        {
            if (editedMicrobeCell.Data == null)
                throw new InvalidOperationException("Layout to edit should not have cells with no data");

            var dataToAdd = new CellTemplate(GetEditedCellDataIfEdited(editedMicrobeCell.Data.ModifiableCellType),
                editedMicrobeCell.Position, editedMicrobeCell.Orientation);

            tempFreshlyUpdatedCells.AddFast(
                new HexWithData<CellTemplate>(dataToAdd, dataToAdd.Position, dataToAdd.Orientation), hexTemporaryMemory,
                hexTemporaryMemory2);
        }

        return tempFreshlyUpdatedCells;
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

        // Can't open a popup menu while moving something
        if (MovingPlacedHex != null)
        {
            Editor.OnActionBlockedWhileMoving();
            return true;
        }

        GetMouseHex(out int q, out int r);

        var cells = new List<HexWithData<CellTemplate>>();

        RunWithSymmetry(q, r, (symmetryQ, symmetryR, _) =>
        {
            var cell = editedMicrobeCells.AsModifiable()
                .GetElementAt(new Hex(symmetryQ, symmetryR), hexTemporaryMemory);

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
        return CellBodyPlanInternalCalculations.GetTotalSpecificCapacity(
            editedMicrobeCells.AsModifiable().Select(o => o.Data!),
            out nominalCapacity);
    }

    public void OnCurrentPatchUpdated(Patch patch)
    {
        CalculateEnergyAndCompoundBalance(GetCurrentCellsWithLatestTypes());

        UpdateCellTypesSecondaryInfo();

        organismStatisticsPanel.UpdateLightSelectionPanelVisibility(
            Editor.CurrentGame.GameWorld.WorldSettings.DayNightCycleEnabled && Editor.CurrentPatch.HasDayAndNight);

        tolerancesEditor.OnDataTolerancesDependOnChanged();
        OnTolerancesChanged(tolerancesEditor.CurrentTolerances);
    }

    /// <summary>
    ///   Call when tolerance data changes
    /// </summary>
    /// <param name="newTolerances">New tolerance data</param>
    public void OnTolerancesChanged(EnvironmentalTolerances newTolerances)
    {
        // Need to show new tolerances warnings (and refresh a few other things)
        refreshTolerancesWarnings = true;

        Editor.OnTolerancesChanged(newTolerances);
    }

    public ToleranceResult CalculateRawTolerances(bool excludePositiveBuffs = false)
    {
        return MicrobeEnvironmentalToleranceCalculations.CalculateTolerances(tolerancesEditor.CurrentTolerances,
            GetCurrentCellsWithLatestTypes(), Editor.CurrentPatch.Biome, excludePositiveBuffs);
    }

    public override void OnLightLevelChanged(float dayLightFraction)
    {
        UpdateVisualLightLevel(dayLightFraction, Editor.CurrentPatch);

        CalculateEnergyAndCompoundBalance(GetCurrentCellsWithLatestTypes());

        UpdateCellTypesSecondaryInfo();
    }

    protected override void RegisterTooltips()
    {
        base.RegisterTooltips();

        organismStatisticsPanel.RegisterTooltips();
    }

    protected CellType CellTypeFromName(string name)
    {
        return Editor.EditedSpecies.ModifiableCellTypes.First(c => c.CellTypeName == name);
    }

    protected override double CalculateCurrentActionCost()
    {
        if (activeActionName == null || !Editor.ShowHover)
            return 0;

        var cellType = CellTypeFromName(activeActionName);

        if (MouseHoverPositions == null)
            return GetEditedCellDataIfEdited(cellType).MPCost * Symmetry.PositionCount();

        var positions = MouseHoverPositions.ToList();

        var cellTemplates = positions
            .Select(h =>
                new HexWithData<CellTemplate>(new CellTemplate(cellType, h.Hex, h.Orientation), h.Hex, h.Orientation))
            .ToList();

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
        return editedMicrobeCells.AsModifiable().GetElementAt(position, hexTemporaryMemory);
    }

    protected override EditorAction? TryCreateRemoveHexAtAction(Hex location, ref int alreadyDeleted)
    {
        var hexHere = editedMicrobeCells.AsModifiable().GetElementAt(location, hexTemporaryMemory);
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
        RenderHoveredHex(q, r, [new Hex(0, 0)], isPlacementProbablyValid,
            out bool hadDuplicate);

        bool showModel = !hadDuplicate;

        // When force updating this has to run to make sure the cell holder has been forced to refresh so that when
        // it becomes visible it doesn't have outdated graphics on it
        if (showModel || forceUpdateCellGraphics)
        {
            var cartesianPosition = Hex.AxialToCartesian(new Hex(q, r));

            var modelHolder = hoverModels[usedHoverModel++];

            ShowCellTypeInModelHolder(modelHolder, GetEditedCellDataIfEdited(cellToPlace), cartesianPosition, rotation);

            if (showModel)
                modelHolder.Visible = true;
        }
    }

    /// <summary>
    ///   Gets the freshest, edited data of a cell type.
    /// </summary>
    /// <param name="cellType">Cell type to check</param>
    /// <returns>Either an edited copy or the original if no edits are done on the type yet</returns>
    private CellType GetEditedCellDataIfEdited(CellType cellType)
    {
        if (CellTypeVisualsOverride == null)
        {
            GD.PrintErr("No cell type visual override set");
            return cellType;
        }

        return CellTypeVisualsOverride.GetCellType(cellType);
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
                    GD.Print($"Trying to place cell \"{cellType.CellTypeName}\" at {hex}");

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
        var cell = new HexWithData<CellTemplate>(new CellTemplate(cellType, new Hex(q, r), rotation), new Hex(q, r),
            rotation);

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
                    var data = new CellMoveActionData(cell, cell.Position, hex, cell.Orientation,
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
            cellPopupMenu.SelectedCells.First().Data!.ModifiableCellType.CellTypeName,
            true);
    }

    /// <summary>
    ///   Sets up or updates the list of buttons to select cell types to place
    /// </summary>
    private void UpdateCellTypeSelections()
    {
        var costMultiplier = Editor.CurrentGame.GameWorld.WorldSettings.MPMultiplier;

        // Re-use / create more buttons to hold all the cell types
        foreach (var cellType in Editor.EditedSpecies.ModifiableCellTypes.OrderBy(t => t.CellTypeName,
                     StringComparer.Ordinal))
        {
            if (!cellTypeSelectionButtons.TryGetValue(cellType.CellTypeName, out var control))
            {
                // Need a new button
                control = (CellTypeSelection)cellTypeSelectionButtonScene.Instantiate();
                control.SelectionGroup = cellTypeButtonGroup;

                control.PartName = cellType.CellTypeName;
                control.CellType = GetEditedCellDataIfEdited(cellType);
                control.Name = cellType.CellTypeName;

                cellTypeSelectionList.AddItem(control);
                cellTypeSelectionButtons.Add(cellType.CellTypeName, control);

                control.Connect(MicrobePartSelection.SignalName.OnPartSelected,
                    new Callable(this, nameof(OnCellToPlaceSelected)));

                // Reuse an existing tooltip when possible
                var tooltip = ToolTipManager.Instance.GetToolTipIfExists<CellTypeTooltip>(cellType.CellTypeName,
                    "cellTypes");

                if (tooltip == null)
                {
                    tooltip = cellTypeTooltipButtonScene.Instantiate<CellTypeTooltip>();
                    ToolTipManager.Instance.AddToolTip(tooltip, "cellTypes");
                }

                tooltip.Name = cellType.CellTypeName;
                tooltip.MutationPointCost = Math.Min(GetEditedCellDataIfEdited(cellType).MPCost * costMultiplier,
                    Constants.MAX_SINGLE_EDIT_MP_COST);

                control.RegisterToolTipForControl(tooltip, true);
            }
            else
            {
                var tooltip = ToolTipManager.Instance.GetToolTipIfExists<CellTypeTooltip>(cellType.CellTypeName,
                    "cellTypes");

                tooltip?.MutationPointCost = Math.Min(GetEditedCellDataIfEdited(cellType).MPCost * costMultiplier,
                    Constants.MAX_SINGLE_EDIT_MP_COST);
            }

            control.MPCost = Math.Min(GetEditedCellDataIfEdited(cellType).MPCost * costMultiplier,
                Constants.MAX_SINGLE_EDIT_MP_COST);
        }

        bool clearSelection = false;

        // Delete no longer necessary buttons
        foreach (var key in cellTypeSelectionButtons.Keys.ToList())
        {
            if (Editor.EditedSpecies.ModifiableCellTypes.All(t => t.CellTypeName != key))
            {
                var control = cellTypeSelectionButtons[key];
                cellTypeSelectionButtons.Remove(key);

                ToolTipManager.Instance.RemoveToolTip(key, "cellTypes");
                control.DetachAndQueueFree();

                if (activeActionName == key)
                    clearSelection = true;
            }
        }

        if (clearSelection)
            ClearSelectedAction();
    }

    /// <summary>
    ///   Updates cell type buttons and tooltips various secondary info, such as cell stats, ATP balances, etc.
    /// </summary>
    private void UpdateCellTypesSecondaryInfo()
    {
        UpdateCellTypesCounts();

        hasNegativeATPCells = false;

        var tolerances = Editor.CalculateCurrentTolerances(tolerancesEditor.CurrentTolerances);
        var environmentalTolerances = MicrobeEnvironmentalToleranceCalculations.ResolveToleranceValues(tolerances);

        foreach (var button in cellTypeSelectionButtons.Values)
        {
            var cellType = button.CellType;

            var tooltip = ToolTipManager.Instance.GetToolTip<CellTypeTooltip>(cellType.CellTypeName, "cellTypes");

            if (tooltip == null)
            {
                GD.PrintErr($"Tooltip not found for species' cell type: {cellType.CellTypeName}");
                continue;
            }

            cellTypesCount.TryGetValue(cellType, out var count);

            UpdateCellTypeTooltipAndWarning(tooltip, button, GetEditedCellDataIfEdited(cellType),
                environmentalTolerances, count);
        }
    }

    /// <summary>
    ///   Updates the info that the cell type tooltip contains and its button's ATP warning badge.
    /// </summary>
    private void UpdateCellTypeTooltipAndWarning(CellTypeTooltip tooltip, CellTypeSelection button, CellType cellType,
        ResolvedMicrobeTolerances environmentalTolerances, int cellCount)
    {
        // Energy and compound balance calculations
        var balances = new Dictionary<Compound, CompoundBalance>();

        energyBalanceInfo = new EnergyBalanceInfoFull();
        energyBalanceInfo.SetupTrackingForRequiredCompounds();

        bool moving = organismStatisticsPanel.CalculateBalancesWhenMoving;

        var maximumMovementDirection =
            MicrobeInternalCalculations.MaximumSpeedDirection(cellType.ModifiableOrganelles);

        var specialization =
            MicrobeInternalCalculations.CalculateSpecializationBonus(cellType.ModifiableOrganelles, tempMemory3);

        ProcessSystem.ComputeEnergyBalanceFull(cellType.ModifiableOrganelles, Editor.CurrentPatch.Biome,
            environmentalTolerances, specialization,
            cellType.MembraneType,
            maximumMovementDirection, moving, true, Editor.CurrentGame.GameWorld.WorldSettings,
            organismStatisticsPanel.CompoundAmountType, null, energyBalanceInfo);

        AddCellTypeCompoundBalance(balances, cellType.ModifiableOrganelles, organismStatisticsPanel.BalanceDisplayType,
            organismStatisticsPanel.CompoundAmountType, Editor.CurrentPatch.Biome, energyBalanceInfo,
            environmentalTolerances, specialization);

        tooltip.DisplayName = cellType.CellTypeName;
        tooltip.MutationPointCost = Math.Min(cellType.MPCost * Editor.CurrentGame.GameWorld.WorldSettings.MPMultiplier,
            Constants.MAX_SINGLE_EDIT_MP_COST);
        tooltip.DisplayCellTypeBalances(balances);
        tooltip.UpdateATPBalance(energyBalanceInfo.TotalProduction, energyBalanceInfo.TotalConsumption);

        tooltip.UpdateHealthIndicator(MicrobeInternalCalculations.CalculateHealth(environmentalTolerances,
            cellType.MembraneType, cellType.MembraneRigidity));

        tooltip.UpdateStorageIndicator(
            MicrobeInternalCalculations.GetTotalNominalCapacity(cellType.ModifiableOrganelles));

        tooltip.UpdateSpeedIndicator(MicrobeInternalCalculations.CalculateSpeed(cellType.ModifiableOrganelles,
            cellType.MembraneType, cellType.MembraneRigidity, cellType.IsBacteria, false));

        tooltip.UpdateRotationSpeedIndicator(
            MicrobeInternalCalculations.CalculateRotationSpeed(cellType.ModifiableOrganelles));

        tooltip.UpdateSizeIndicator(cellType.Organelles.Sum(o => o.Definition.HexCount));
        tooltip.UpdateDigestionSpeedIndicator(
            MicrobeInternalCalculations.CalculateTotalDigestionSpeed(cellType.ModifiableOrganelles));

        button.ShowInsufficientATPWarning = energyBalanceInfo.TotalProduction < energyBalanceInfo.TotalConsumption;

        if (energyBalanceInfo.TotalConsumption > energyBalanceInfo.TotalProduction
            && cellCount > 0)
        {
            // This cell is present in the colony and has a negative energy balance
            hasNegativeATPCells = true;
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
        CalculateEnergyAndCompoundBalance(GetCurrentCellsWithLatestTypes());

        UpdateCellTypesSecondaryInfo();

        UpdateFinishButtonWarningVisibility();
    }

    private void OnResourceLimitingModeChanged()
    {
        CalculateEnergyAndCompoundBalance(GetCurrentCellsWithLatestTypes());

        UpdateCellTypesSecondaryInfo();

        UpdateFinishButtonWarningVisibility();
    }

    private void OnCellsChanged()
    {
        UpdateAlreadyPlacedVisuals();

        UpdateStats();

        UpdateArrow();

        RecalculateWrongGrowthOrderCells();

        UpdateFinishButtonWarningVisibility();

        UpdateGrowthOrderUI();

        OnTolerancesChanged(tolerancesEditor.CurrentTolerances);
        tolerancesEditor.OnDataTolerancesDependOnChanged();
    }

    private void UpdateStats()
    {
        var latestTypes = GetCurrentCellsWithLatestTypes();

        organismStatisticsPanel.UpdateStorage(GetAdditionalCapacities(out var nominalCapacity), nominalCapacity);
        organismStatisticsPanel.UpdateSpeed(CellBodyPlanInternalCalculations.CalculateSpeed(latestTypes));
        organismStatisticsPanel.UpdateRotationSpeed(
            CellBodyPlanInternalCalculations.CalculateRotationSpeed(latestTypes));
        var (ammoniaCost, phosphatesCost) =
            CellBodyPlanInternalCalculations.CalculateOrganellesCost(latestTypes);
        organismStatisticsPanel.UpdateOrganellesCost(ammoniaCost, phosphatesCost);

        CalculateEnergyAndCompoundBalance(latestTypes);

        UpdateCellTypesSecondaryInfo();
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
            tempAllOrganelles.Clear();
            foreach (var cell in cells)
            {
                foreach (var organelle in cell.Data!.CellType.Organelles)
                {
                    tempAllOrganelles.Add(organelle);
                }
            }

            ProcessSystem.ComputeActiveProcessList(tempAllOrganelles, ref tempAllProcesses);

            conditionsData = new BiomeResourceLimiterAdapter(organismStatisticsPanel.ResourceLimitingMode,
                conditionsData, tempAllProcesses);
        }

        energyBalanceInfo = new EnergyBalanceInfoFull();
        energyBalanceInfo.SetupTrackingForRequiredCompounds();

        // Cells can't individually move in the body plan, so this probably makes sense
        var maximumMovementDirection =
            MicrobeInternalCalculations.MaximumSpeedDirection(
                GetEditedCellDataIfEdited(cells[0].Data!.ModifiableCellType).ModifiableOrganelles);

        var environmentalTolerances =
            MicrobeEnvironmentalToleranceCalculations.ResolveToleranceValues(Editor.CalculateRawTolerances());

        // TODO: improve performance by calculating the balance per cell type
        foreach (var hex in cells)
        {
            var specialization =
                MicrobeInternalCalculations.CalculateSpecializationBonus(hex.Data!.ModifiableOrganelles, tempMemory3);

            // TODO: adjacency bonuses from body plan (GetAdjacencySpecializationBonus)

            ProcessSystem.ComputeEnergyBalanceFull(hex.Data.ModifiableOrganelles, conditionsData,
                environmentalTolerances, specialization, hex.Data.MembraneType,
                maximumMovementDirection, moving, true, Editor.CurrentGame.GameWorld.WorldSettings,
                organismStatisticsPanel.CompoundAmountType, null, energyBalanceInfo);
        }

        // Passing those variables by refs to the following functions to reuse them
        float nominalStorage = 0;
        Dictionary<Compound, float>? specificStorages = null;

        // This takes balanceType into account as well, https://github.com/Revolutionary-Games/Thrive/issues/2068
        var compoundBalanceData =
            CalculateCompoundBalanceWithMethod(organismStatisticsPanel.BalanceDisplayType,
                organismStatisticsPanel.CompoundAmountType,
                cells, conditionsData, energyBalanceInfo,
                ref specificStorages, ref nominalStorage, environmentalTolerances);

        UpdateCompoundBalances(compoundBalanceData);

        // TODO: should this skip on being affected by the resource limited?
        var nightBalanceData = CalculateCompoundBalanceWithMethod(organismStatisticsPanel.BalanceDisplayType,
            CompoundAmountType.Minimum, cells, conditionsData, energyBalanceInfo, ref specificStorages,
            ref nominalStorage, environmentalTolerances);

        UpdateCompoundLastingTimes(compoundBalanceData, nightBalanceData, nominalStorage,
            specificStorages ?? throw new Exception("Special storages should have been calculated"));

        // TODO: find out why this method used to take the cells parameter but now causes a warning so it is removed
        // HandleProcessList( cells, energyBalance, conditionsData);
        HandleProcessList(energyBalanceInfo, conditionsData);
    }

    private Dictionary<Compound, CompoundBalance> CalculateCompoundBalanceWithMethod(BalanceDisplayType calculationType,
        CompoundAmountType amountType,
        IReadOnlyList<HexWithData<CellTemplate>> cells, IBiomeConditions biome, EnergyBalanceInfoFull energyBalance,
        ref Dictionary<Compound, float>? specificStorages, ref float nominalStorage,
        in ResolvedMicrobeTolerances tolerances)
    {
        Dictionary<Compound, CompoundBalance> compoundBalanceData = new();
        foreach (var cell in cells)
        {
            var organelles = GetEditedCellDataIfEdited(cell.Data!.ModifiableCellType).ModifiableOrganelles;
            var specialization =
                MicrobeInternalCalculations.CalculateSpecializationBonus(organelles, tempMemory3);

            // TODO: efficiency from cell layout positions (GetAdjacencySpecializationBonus)

            AddCellTypeCompoundBalance(compoundBalanceData, organelles, calculationType,
                amountType, biome, energyBalance, tolerances, specialization);
        }

        specificStorages ??= CellBodyPlanInternalCalculations.GetTotalSpecificCapacity(cells.Select(o => o.Data!),
            out nominalStorage);

        return ProcessSystem.ComputeCompoundFillTimes(compoundBalanceData, nominalStorage, specificStorages);
    }

    private void AddCellTypeCompoundBalance(Dictionary<Compound, CompoundBalance> compoundBalanceData,
        IEnumerable<OrganelleTemplate> organelles, BalanceDisplayType calculationType, CompoundAmountType amountType,
        IBiomeConditions biome, EnergyBalanceInfoFull energyBalance, ResolvedMicrobeTolerances tolerances,
        float specializationFactor)
    {
        switch (calculationType)
        {
            case BalanceDisplayType.MaxSpeed:
                ProcessSystem.ComputeCompoundBalance(organelles, biome, tolerances, specializationFactor,
                    amountType, true, compoundBalanceData);
                break;
            case BalanceDisplayType.EnergyEquilibrium:
                ProcessSystem.ComputeCompoundBalanceAtEquilibrium(organelles, biome,
                    tolerances, specializationFactor, amountType, energyBalance, compoundBalanceData);
                break;
            default:
                GD.PrintErr("Unknown compound balance type: ", organismStatisticsPanel.BalanceDisplayType);
                goto case BalanceDisplayType.EnergyEquilibrium;
        }
    }

    private void UpdateCellTypesCounts()
    {
        cellTypesCount.Clear();

        foreach (var cell in editedMicrobeCells)
        {
            var type = GetEditedCellDataIfEdited(cell.Data!.ModifiableCellType);

            cellTypesCount.TryGetValue(type, out var count);
            cellTypesCount[type] = count + 1;
        }
    }

    /// <summary>
    ///   Recalculates cells that end up being disconnected because of their growth order.
    ///   Saves the results in <see cref="wrongGrowthOrderCells"/>
    /// </summary>
    private void RecalculateWrongGrowthOrderCells()
    {
        wrongGrowthOrderCells.Clear();

        // Reuse this work memory
        islandsWorkMemory1.Clear();

        foreach (var cell in growthOrderGUI.ApplyOrderingToItems(editedMicrobeCells.AsModifiable(), i => i.Data!))
        {
            islandsWorkMemory1.Add(cell.Position);

            if (islandsWorkMemory1.Count == 1)
                continue;

            bool hasNeighboor = false;

            foreach (var offset in Hex.HexNeighbourOffset.Values)
            {
                if (islandsWorkMemory1.Contains(cell.Position + offset))
                {
                    hasNeighboor = true;
                    break;
                }
            }

            if (!hasNeighboor)
            {
                wrongGrowthOrderCells.Add(cell.Position);
            }
        }
    }

    private void OnGrowthOrderChanged()
    {
        RecalculateWrongGrowthOrderCells();

        UpdateGrowthOrderUI();

        UpdateFinishButtonWarningVisibility();
    }

    private void ApplyGrowthOrderToCells()
    {
        var order = new HexWithData<CellTemplate>[editedMicrobeCells.Count];

        editedMicrobeCells.CopyTo(order, 0);
        editedMicrobeCells.Clear();

        foreach (var cell in growthOrderGUI.ApplyOrderingToItems(order, i => i.Data!))
        {
            editedMicrobeCells.AddFast(cell, hexTemporaryMemory, hexTemporaryMemory2);
        }

        var leaderPosition = editedMicrobeCells[0].Position;

        foreach (var cell in editedMicrobeCells.AsModifiable())
        {
            cell.Position -= leaderPosition;
            cell.Data!.Position -= leaderPosition;
        }
    }

    /// <summary>
    ///   This destroys and creates again entities to represent all the currently placed cells. Call this whenever
    ///   editedMicrobeCells-variable is changed.
    /// </summary>
    private void UpdateAlreadyPlacedVisuals()
    {
        editedMicrobeCells.GetIslandHexes(islandResults, islandsWorkMemory1, islandsWorkMemory2, islandsWorkMemory3);

        // Build the entities to show the current microbe
        IReadOnlyList<Hex> positionZeroList = [new(0, 0)];
        UpdateAlreadyPlacedHexes(editedMicrobeCells.AsModifiable().Select(o => (o.Position, positionZeroList,
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

            ShowCellTypeInModelHolder(modelHolder, GetEditedCellDataIfEdited(hexWithData.Data!.ModifiableCellType), pos,
                hexWithData.Data!.Orientation);

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

        duplicateCellTypeName.Text = type.CellTypeName;

        // Make sure it's shown in red initially as it is a duplicate name
        OnNewCellTypeNameChanged(type.CellTypeName);

        duplicateCellTypeDialog.PopupCenteredShrink();

        // This isn't absolutely necessary but makes the dialog open a bit nicer in that the same thing stays focused
        // the entire time and doesn't change due to the focus grabber a tiny bit later
        duplicateCellTypeName.GrabFocusInOpeningPopup();
        duplicateCellTypeName.SelectAll();
        duplicateCellTypeName.CaretColumn = type.CellTypeName.Length;
    }

    private void OnNewCellTypeNameChanged(string newText)
    {
        if (!Editor.IsNewCellTypeNameValid(newText))
        {
            GUICommon.MarkInputAsInvalid(duplicateCellTypeName);
        }
        else
        {
            GUICommon.MarkInputAsValid(duplicateCellTypeName);
        }
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

        if (!Editor.IsNewCellTypeNameValid(newTypeName))
        {
            GD.Print("Bad name for new cell type");
            Editor.OnInvalidAction();
            return;
        }

        var type = CellTypeFromName(activeActionName!);

        // The player probably wants their latest edits to be in the duplicated cell type
        // TODO: store name of the original cell type this is cloned from to make MP comparisons easier?
        var newType = (CellType)GetEditedCellDataIfEdited(type).Clone();
        newType.CellTypeName = newTypeName;

        // Remember what this split from for better MP result calculations (as otherwise matching intermediate cell
        // types with minimum MP usage is very challenging)
        newType.SplitFromTypeName = type.CellTypeName;

        var data = new DuplicateDeleteCellTypeData(newType, false);
        var action = new SingleEditorAction<DuplicateDeleteCellTypeData>(DuplicateCellType, DeleteCellType, data);
        EnqueueAction(new CombinedEditorAction(action));

        duplicateCellTypeDialog.Hide();
    }

    private void OnDeleteCellTypePressed()
    {
        if (string.IsNullOrEmpty(activeActionName))
            return;

        GUICommon.Instance.PlayButtonPressSound();

        // Apparently this works without needing to call GetEditedCellDataIfEdited here. But keep an eye out
        // for allowing deleting a used type problem.
        var type = CellTypeFromName(activeActionName!);

        // Disallow deleting a type in use currently
        if (editedMicrobeCells.AsModifiable().Any(c => c.Data!.ModifiableCellType == type))
        {
            GD.Print("Can't delete in use cell type");
            cannotDeleteInUseTypeDialog.PopupCenteredShrink();
            return;
        }

        var data = new DuplicateDeleteCellTypeData(type, true);
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
        var newType = GetEditedCellDataIfEdited(type);

        foreach (var entry in cellTypeSelectionButtons)
        {
            if (entry.Value.CellType == newType || (entry.Value.CellType == type && newType == type))
            {
                // Updating existing
                entry.Value.ReportTypeChanged();
            }
            else if (entry.Value.CellType == type)
            {
                // Button is seeing its first edit (and needs to transform to be for the edit type)
                GD.Print($"First edit of cell type {type.CellTypeName}");
                var control = entry.Value;
                control.CellType = newType;
                control.MPCost = Math.Min(newType.MPCost * Editor.CurrentGame.GameWorld.WorldSettings.MPMultiplier,
                    Constants.MAX_SINGLE_EDIT_MP_COST);

                // Name shouldn't be able to change here

                // Also fix the tooltip
                var tooltip = ToolTipManager.Instance.GetToolTipIfExists<CellTypeTooltip>(newType.CellTypeName,
                    "cellTypes");

                tooltip?.MutationPointCost =
                    Math.Min(newType.MPCost * Editor.CurrentGame.GameWorld.WorldSettings.MPMultiplier,
                        Constants.MAX_SINGLE_EDIT_MP_COST);

                control.ReportTypeChanged();
            }
        }
    }

    private void SetSelectionMenuTab(string tab)
    {
        var selection = (SelectionMenuTab)Enum.Parse(typeof(SelectionMenuTab), tab);

        if (selection == selectedSelectionMenuTab)
            return;

        GUICommon.Instance.PlayButtonPressSound();

        if (!BlockTabSwitchIfInProgressAction(CanCancelAction))
        {
            selectedSelectionMenuTab = selection;
        }

        ApplySelectionMenuTab();
    }

    private void ApplySelectionMenuTab()
    {
        // Hide all
        structureTab.Hide();
        reproductionTab.Hide();
        behaviourEditor.Hide();
        growthOrderTab.Hide();
        toleranceTab.Hide();

        ShowGrowthOrder = selectedSelectionMenuTab is SelectionMenuTab.GrowthOrder;

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

            case SelectionMenuTab.GrowthOrder:
            {
                growthOrderTab.Show();
                growthOrderTabButton.ButtonPressed = true;

                UpdateGrowthOrderUI();
                break;
            }

            case SelectionMenuTab.Tolerance:
            {
                toleranceTab.Show();
                tolerancesTabButton.ButtonPressed = true;
                break;
            }

            default:
                throw new Exception("Invalid selection menu tab");
        }
    }
}
