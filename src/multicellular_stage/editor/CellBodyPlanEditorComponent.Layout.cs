using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Godot;

/// <summary>
///   Implements the manual layout editing part of the editor. This is a separate file as this is a bit of a long new
///   functionality, so it is cleared to have it in a separate file.
/// </summary>
public partial class CellBodyPlanEditorComponent
{
    /// <summary>
    ///   All positions in the full layout as last generated.
    /// </summary>
    private readonly HashSet<Hex> fullLayoutOccupied = [];

    /// <summary>
    ///   Positions in the full layout that have multiple things trying to be in them and are thus invalid.
    /// </summary>
    private readonly HashSet<Hex> fullLayoutInvalid = [];

    /// <summary>
    ///   Identity mapping for the full layout to keep it consistent
    /// </summary>
    private readonly Dictionary<HexWithData<CellTemplate>, CellTemplate> fullLayoutGrowthOrderSources =
        new(ReferenceEqualityComparer.Instance);

    private readonly Dictionary<HexWithData<CellTemplate>, int> fullLayoutGrowthOrderIndices =
        new(ReferenceEqualityComparer.Instance);

    /// <summary>
    ///   The manual layout contains clones of the compact layout cells. Keep their source references separately from
    ///   the growth order bookkeeping, as the latter is also rebuilt for the automatic preview.
    /// </summary>
    private readonly Dictionary<HexWithData<CellTemplate>, HexWithData<CellTemplate>> manualLayoutSources =
        new(ReferenceEqualityComparer.Instance);

    /// <summary>
    ///   Used as a fallback to map manual layouts with position and type data (when the above dictionary doesn't
    ///   give a match)
    /// </summary>
    private readonly Dictionary<HexWithData<CellTemplate>, (Hex Position, CellType Type)> manualLayoutSourceData =
        new(ReferenceEqualityComparer.Instance);

    private readonly HashSet<HexWithData<CellTemplate>> manualLayoutCellsWithBrokenExpectedAdjacencies =
        new(ReferenceEqualityComparer.Instance);

    /// <summary>
    ///   Cache of missing adjacency pairs in the manual layout. This is used to display problem arrows.
    /// </summary>
    private readonly List<(HexWithData<CellTemplate> First, HexWithData<CellTemplate> Second)>
        manualLayoutMissingAdjacencyPairs = [];

    // The compact editor layout is kept intact while the Layout tab displays the actual cell footprints.
    private IndividualHexLayout<CellTemplate> fullLayoutPreview = new();

    /// <summary>
    ///   Unlike the automatic layout store, this is a list because manual editing must preserve invalid intermediate
    ///   states such as overlapping cells so that they can be displayed and fixed by the player.
    /// </summary>
    private List<HexWithData<CellTemplate>> manualFullLayout = [];

    private Task<LayoutCalculationResult>? pendingLayoutCalculation;
    private bool layoutCalculationRequested;
    private bool layoutPreviewActive;
    private bool fullLayoutNeedsRefresh;
    private bool manualLayoutHasErrors;
    private bool fullLayoutHasOverlaps;
    private bool fullLayoutHasDisconnectedCells;
    private bool fullLayoutHasAdjacencyDisconnects;

    private IReadOnlyList<HexWithData<CellTemplate>> CurrentFullLayout =>
        UsesManualPlayerLayout ? manualFullLayout : fullLayoutPreview;

    private static LayoutCalculationResult CalculateFullLayout(IndividualHexLayout<CellTemplate> sourceLayout)
    {
        var result = new LayoutCalculationResult();
        var work1 = new List<Hex>();
        var work2 = new List<Hex>();
        var work3 = new HashSet<Hex>();
        var growthOrderSources = sourceLayout.AsModifiable().Select(cell => cell.Data!).ToList();

        var gameplay = new CellLayout<CellTemplate>();
        var editor = new IndividualHexLayout<CellTemplate>();
        MulticellularLayoutHelpers.UpdateGameplayLayout(gameplay, editor, sourceLayout, AlgorithmQuality.High,
            work1, work2, work3);

        result.Gameplay = gameplay;
        result.GrowthOrderSources = growthOrderSources;

        // This is not actually required for now
        // result.Editor = editor;

        return result;
    }

    private void CopyLayout(IReadOnlyList<HexWithData<CellTemplate>> source,
        List<HexWithData<CellTemplate>> target)
    {
        var sourceCells = source.ToList();
        var sourceGrowthOrderSources = sourceCells
            .Select(cell => fullLayoutGrowthOrderSources.GetValueOrDefault(cell))
            .ToList();
        var sourceGrowthOrderIndices = sourceCells
            .Select(cell => fullLayoutGrowthOrderIndices.GetValueOrDefault(cell, -1))
            .ToList();

        target.Clear();
        manualLayoutSources.Clear();
        manualLayoutSourceData.Clear();

        fullLayoutGrowthOrderSources.Clear();
        fullLayoutGrowthOrderIndices.Clear();

        var growthOrderCells = growthOrderGUI.ApplyOrderingToItems(editedMicrobeCells.AsModifiable(), i => i.Data!)
            .ToList();
        var growthOrder = growthOrderCells.Select(i => i.Data!).ToList();

        for (int i = 0; i < sourceCells.Count; ++i)
        {
            var cell = sourceCells[i];
            var clone = (CellTemplate)cell.Data!.Clone();
            var copied = new HexWithData<CellTemplate>(clone, clone.Position, clone.Orientation);
            target.Add(copied);

            var growthOrderIndex = sourceGrowthOrderIndices[i] >= 0 ? sourceGrowthOrderIndices[i] : i;
            if (growthOrderIndex < growthOrder.Count)
            {
                SetManualLayoutSource(copied, growthOrderCells[growthOrderIndex]);
                fullLayoutGrowthOrderSources[copied] = sourceGrowthOrderSources[i] ?? growthOrder[growthOrderIndex];
                fullLayoutGrowthOrderIndices[copied] = growthOrderIndex;
            }
        }
    }

    private void RebuildFullLayoutGrowthOrderSources(IReadOnlyList<HexWithData<CellTemplate>> layout,
        IReadOnlyList<CellTemplate>? growthOrderSources = null)
    {
        fullLayoutGrowthOrderSources.Clear();
        fullLayoutGrowthOrderIndices.Clear();

        var cells = layout.ToList();

        var growthOrder = growthOrderSources ?? growthOrderGUI
            .ApplyOrderingToItems(editedMicrobeCells.AsModifiable(), i => i.Data!).Select(i => i.Data!).ToList();

        if (growthOrderSources != null)
        {
            var growthOrderIndices = new Dictionary<CellTemplate, int>(ReferenceEqualityComparer.Instance);
            var growthOrderTypeIndices = new Dictionary<CellType, int>(ReferenceEqualityComparer.Instance);
            for (int i = 0; i < growthOrder.Count; ++i)
            {
                growthOrderIndices[growthOrder[i]] = i;
                growthOrderTypeIndices[growthOrder[i].ModifiableCellType] = i;
            }

            var originalGrowthOrder = growthOrderGUI
                .ApplyOrderingToItems(editedMicrobeCells.AsModifiable(), i => i.Data!)
                .Select(i => i.Data!).ToList();

            foreach (var cell in cells)
            {
                if (cell.Data != null &&
                    (growthOrderIndices.TryGetValue(cell.Data, out var index) ||
                        growthOrderTypeIndices.TryGetValue(cell.Data.ModifiableCellType, out index)) &&
                    index < originalGrowthOrder.Count)
                {
                    // Keep the original editor data as the source. RefreshFullLayoutGrowthOrderIndices compares these
                    // references with the growth-order picker after the preview has been recalculated.
                    fullLayoutGrowthOrderSources[cell] = originalGrowthOrder[index];
                    fullLayoutGrowthOrderIndices[cell] = index;
                }
            }
        }
        else
        {
            for (int i = 0; i < cells.Count && i < growthOrder.Count; ++i)
            {
                fullLayoutGrowthOrderSources[cells[i]] = growthOrder[i];
                fullLayoutGrowthOrderIndices[cells[i]] = i;
            }
        }
    }

    private void RefreshFullLayoutGrowthOrderIndices()
    {
        var order = growthOrderGUI.GetCurrentOrder();
        foreach (var pair in fullLayoutGrowthOrderSources)
        {
            var index = -1;
            for (int i = 0; i < order.Count; ++i)
            {
                if (ReferenceEquals(order[i], pair.Value))
                {
                    index = i;
                    break;
                }
            }

            if (index >= 0)
                fullLayoutGrowthOrderIndices[pair.Key] = index;
        }
    }

    private void SetManualLayoutSource(HexWithData<CellTemplate> manualCell, HexWithData<CellTemplate> source)
    {
        manualLayoutSources[manualCell] = source;
        manualLayoutSourceData[manualCell] = (source.Position, source.Data!.ModifiableCellType);
    }

    /// <summary>
    ///   Starts a background task to calculate the full layout of the edited microbe cells. This is done in the
    ///   background as the calculation can take tens of seconds.
    /// </summary>
    private void StartLayoutCalculation()
    {
        if (pendingLayoutCalculation != null)
        {
            // The existing task uses an immutable snapshot. Remember that a newer layout is needed once it finishes.
            layoutCalculationRequested = true;
            return;
        }

        layoutCalculationRequested = false;

        // Clone the data on the main thread to prepare it for background processing
        var source = new IndividualHexLayout<CellTemplate>();
        foreach (var cell in growthOrderGUI.ApplyOrderingToItems(editedMicrobeCells.AsModifiable(), i => i.Data!))
        {
            var type = GetEditedCellDataIfEdited(cell.Data!.ModifiableCellType);

            // Unfortunately, we have to take a full clone here for safety
            var snapshot = new CellTemplate(type.Clone(true), cell.Position, cell.Orientation);
            source.AddFast(new HexWithData<CellTemplate>(snapshot, snapshot.Position, snapshot.Orientation),
                hexTemporaryMemory, hexTemporaryMemory2);
        }

        layoutCalculationSpinner.Show();
        pendingLayoutCalculation = new Task<LayoutCalculationResult>(() => CalculateFullLayout(source));
        TaskExecutor.Instance.AddTask(pendingLayoutCalculation);
    }

    private void SetFullLayoutPreview(CellLayout<CellTemplate> gameplay)
    {
        fullLayoutPreview.Clear();

        // CellLayout stores the final cell templates directly. The editor renderer uses HexWithData wrappers, so
        // create only those wrappers here on the main thread without cloning the already calculated data.
        foreach (var cell in gameplay)
        {
            // TODO: check if these allocations would make more sense to run in the background task
            var wrapped = new HexWithData<CellTemplate>(cell, cell.Position, cell.Orientation);
            fullLayoutPreview.AddFast(wrapped, hexTemporaryMemory, hexTemporaryMemory2);
        }
    }

    private void UpdateFullLayoutVisuals()
    {
        if (CurrentFullLayout.Count == 0)
        {
            manualLayoutHasErrors = false;
            fullLayoutHasOverlaps = false;
            fullLayoutHasDisconnectedCells = false;
            fullLayoutHasAdjacencyDisconnects = false;
            manualLayoutCellsWithBrokenExpectedAdjacencies.Clear();
            UpdateLayoutErrorDisplay();
            return;
        }

        fullLayoutOccupied.Clear();
        fullLayoutInvalid.Clear();
        fullLayoutHasOverlaps = false;
        fullLayoutHasDisconnectedCells = false;
        fullLayoutHasAdjacencyDisconnects = false;
        manualLayoutCellsWithBrokenExpectedAdjacencies.Clear();

        // Maps each position to a cell that exists there
        var cellHexes = new Dictionary<Hex, HexWithData<CellTemplate>>();

        // Positions inside each cell
        var cellPositions = new Dictionary<HexWithData<CellTemplate>, List<Hex>>();

        foreach (var cell in CurrentFullLayout)
        {
            var positions = GetFullCellPositionsGlobal(cell);

            // Detect overlaps globally here
            foreach (var globalPosition in positions)
            {
                if (!cellHexes.TryAdd(globalPosition, cell))
                {
                    fullLayoutInvalid.Add(globalPosition);
                    fullLayoutHasOverlaps = true;
                }

                fullLayoutOccupied.Add(globalPosition);
            }

            // For the below loop, it assumes global positions per cell, so we updated them after the shift to have
            // global positions
            cellPositions[cell] = positions;
        }

        // A cell is invalid as a whole when it overlaps or has no neighbouring cell.
        foreach (var pair in cellPositions)
        {
            bool touchesAnotherCell = pair.Value.Any(position =>
                Hex.HexNeighbourOffset.Values.Any(offset =>
                    cellHexes.TryGetValue(position + offset, out var other) && !ReferenceEquals(other, pair.Key)));

            if (!touchesAnotherCell && CurrentFullLayout.Count > 1)
            {
                // Mark all hexes invalid of this cell
                fullLayoutInvalid.UnionWith(pair.Value);
                fullLayoutHasDisconnectedCells = true;
            }
        }

        // Next, check that cells still approximately follow the adjacencies as set in the main layout.
        if (UsesManualPlayerLayout)
        {
            foreach (var pair in cellPositions)
            {
                if (!HasEnoughExpectedAdjacencies(pair.Key, pair.Value))
                {
                    fullLayoutInvalid.UnionWith(pair.Value);
                    fullLayoutHasAdjacencyDisconnects = true;
                    manualLayoutCellsWithBrokenExpectedAdjacencies.Add(pair.Key);
                }
            }

            CacheMissingManualLayoutAdjacencyPairs(cellPositions);
        }

        manualLayoutHasErrors = fullLayoutInvalid.Count > 0;
        UpdateLayoutErrorDisplay();

        if (!layoutPreviewActive)
        {
            UpdateFinishButtonWarningVisibility();
            return;
        }

        // Update the display hexes.
        // Order doesn't really matter here, so just make it a list.
        var displayed = fullLayoutOccupied /*.OrderBy(h => h.Q).ThenBy(h => h.R) */.ToList();

        // We use the invalid hexes as islands to mark them as invalid
        UpdateAlreadyPlacedHexes(displayed.Select(position => (position, (IReadOnlyList<Hex>)[new Hex(0, 0)], false)),
            fullLayoutInvalid.ToList());

        // TODO: display all organelle models?
        int nextModel = 0;
        foreach (var cell in CurrentFullLayout)
        {
            if (nextModel >= placedModels.Count)
                placedModels.Add(CreatePreviewModelHolder());

            var model = placedModels[nextModel++];
            ShowCellTypeInModelHolder(model, GetEditedCellDataIfEdited(cell.Data!.ModifiableCellType),
                Hex.AxialToCartesian(cell.Position) + microbeModelOffset, cell.Orientation);
            model.Visible = true;
        }

        while (nextModel < placedModels.Count)
        {
            placedModels[^1].DetachAndQueueFree();
            placedModels.RemoveAt(placedModels.Count - 1);
        }

        RecalculateFullLayoutGrowthOrderErrors();
        UpdateGrowthOrderNumbers();
        UpdateFinishButtonWarningVisibility();
        UpdateArrow();
    }

    private void UpdateLayoutErrorDisplay()
    {
        bool growthOrderHasErrors = UsesManualPlayerLayout && wrongGrowthOrderCells.Count > 0;
        layoutErrorLabel.Visible = manualLayoutHasErrors || growthOrderHasErrors;
        if (!manualLayoutHasErrors && !growthOrderHasErrors)
            return;

        string error;
        if (!manualLayoutHasErrors)
        {
            error = Localization.Translate("CELL_BODY_MANUAL_LAYOUT_ERROR_GROWTH_ORDER");
        }
        else
        {
            error = (fullLayoutHasOverlaps, fullLayoutHasDisconnectedCells, fullLayoutHasAdjacencyDisconnects) switch
            {
                (true, _, true) => Localization.Translate(
                    "CELL_BODY_MANUAL_LAYOUT_ERROR_OVERLAP_AND_ADJACENCY_DISCONNECT"),
                (_, _, true) => Localization.Translate("CELL_BODY_MANUAL_LAYOUT_ERROR_ADJACENCY_DISCONNECT"),
                (true, true, false) => Localization.Translate("CELL_BODY_MANUAL_LAYOUT_ERROR_OVERLAP_AND_DISCONNECT"),
                (true, false, false) => Localization.Translate("CELL_BODY_MANUAL_LAYOUT_ERROR_OVERLAP"),
                (false, true, false) => Localization.Translate("CELL_BODY_MANUAL_LAYOUT_ERROR_DISCONNECT"),
                _ => throw new InvalidOperationException("Manual layout has errors without a known error type"),
            };
        }

        layoutErrorLabel.Text = error;
    }

    private void RecalculateFullLayoutGrowthOrderErrors()
    {
        if (!UsesManualPlayerLayout)
        {
            // The automatic layout algorithm may place cells in an order that differs from the player's compact
            // growth order. Do not report that algorithm-internal difference as a player error.
            wrongGrowthOrderCells.Clear();
            UpdateLayoutErrorDisplay();
            return;
        }

        wrongGrowthOrderCells.Clear();

        // Just a single cell is always in the right order
        if (CurrentFullLayout.Count < 2)
        {
            UpdateLayoutErrorDisplay();
            return;
        }

        // Order by growth order index
        var orderedCells = CurrentFullLayout
            .OrderBy(cell => fullLayoutGrowthOrderIndices.GetValueOrDefault(cell, int.MaxValue))
            .ToList();

        // Then detect where cells are grown in the wrong order (i.e. no grown position before it)
        var grownPositions = new HashSet<Hex>();
        foreach (var cell in orderedCells)
        {
            var positions = GetFullCellPositionsGlobal(cell);
            bool touchesEarlierCell = positions.Any(position => Hex.HexNeighbourOffset.Values.Any(offset =>
                grownPositions.Contains(position + offset)));

            if (grownPositions.Count > 0 && !touchesEarlierCell)
                wrongGrowthOrderCells.Add(cell.Position);

            grownPositions.UnionWith(positions);
        }

        UpdateLayoutErrorDisplay();
    }

    // TODO: this should use a temporary work list, and callers can then duplicate it when needed
    private List<Hex> GetFullCellPositionsGlobal(HexWithData<CellTemplate> cell)
    {
        var positions = new List<Hex>();

        // We do a manual fetch of the organelle positions here so that we have the latest data if the type is
        // edited
        var type = GetEditedCellDataIfEdited(cell.Data!.ModifiableCellType);
        foreach (var organelle in type.ModifiableOrganelles)
        {
            foreach (var organelleHex in organelle.Definition.GetRotatedHexes(organelle.Orientation))
            {
                var position = organelleHex + organelle.Position;
                positions.Add(position);
            }
        }

        // We have to run reposition to origin equivalent logic here! as otherwise the layout might not be valid
        // after applying edits
        Hex originShift = CalculateExpectedLayoutShift(type.ModifiableOrganelles);

        positions.Clear();

        // Then apply the origin shift
        foreach (var organelle in type.ModifiableOrganelles)
        {
            foreach (var organelleHex in organelle.Definition.GetRotatedHexes(organelle.Orientation))
            {
                // And now we can calculate global final positions
                var globalPosition = cell.Position +
                    Hex.RotateAxialNTimes(organelleHex + organelle.Position - originShift, cell.Orientation);

                positions.Add(globalPosition);
            }
        }

        return positions;
    }

    private List<Hex> GetFullCellPositionsLocal(CellType type)
    {
        var positions = new List<Hex>();

        foreach (var organelle in type.ModifiableOrganelles)
        {
            foreach (var organelleHex in organelle.Definition.GetRotatedHexes(organelle.Orientation))
            {
                var position = organelleHex + organelle.Position;
                positions.Add(position);
            }
        }

        // We have to run reposition to origin equivalent logic here! as otherwise the layout might not be valid
        // after applying edits
        Hex originShift = CalculateExpectedLayoutShift(type.ModifiableOrganelles);

        // Then apply the origin shift
        for (int i = 0; i < positions.Count; ++i)
        {
            positions[i] -= originShift;
        }

        return positions;
    }

    private Hex CalculateExpectedLayoutShift(IReadOnlyList<IReadOnlyOrganelleTemplate> localOrganelles)
    {
        // Origin shift here. This is done because only after exiting the editor are things layout shifted, which means
        // that we need to pre-emptively apply a layout shift here so that the final results match what is shown here.
        var center = MicrobeInternalCalculations.CalculateCenterOfMass(localOrganelles);

        return Hex.CartesianToAxial(center);
    }

    /// <summary>
    ///   Updates <see cref="manualFullLayout"/> so that it is ordered according to growth order
    ///   and fixes the root position to be at the origin (as this is assumed by colony logic in gameplay).
    /// </summary>
    private void ReorderManualLayoutToGrowthOrderAndFixRootPosition()
    {
        // Growth order doesn't matter with a single cell
        if (manualFullLayout.Count < 2)
            return;

        var ordered = manualFullLayout
            .OrderBy(cell => fullLayoutGrowthOrderIndices.GetValueOrDefault(cell, int.MaxValue))
            .ToList();
        var leaderPosition = ordered[0].Position;
        if (leaderPosition != new Hex(0, 0))
        {
            foreach (var cell in ordered)
            {
                cell.Position -= leaderPosition;
                cell.Data!.Position -= leaderPosition;
            }
        }

        manualFullLayout.Clear();
        manualFullLayout.AddRange(ordered);
    }

    private IEnumerable<(Vector3 Position, string Text, Color TextColor)> FullLayoutGrowthOrderFloatingNumbers()
    {
        var ordered = CurrentFullLayout
            .OrderBy(cell => fullLayoutGrowthOrderIndices.GetValueOrDefault(cell, int.MaxValue))
            .ToList();
        for (int i = 0; i < ordered.Count; ++i)
        {
            var cell = ordered[i];

            // Automatic layout still shows the growth order for reference, but its algorithmic placement order is
            // excluded from error detection, so its numbers must always remain white.
            var textColor = UsesManualPlayerLayout && wrongGrowthOrderCells.Contains(cell.Position) ?
                Colors.Red :
                Colors.White;

            yield return (Hex.AxialToCartesian(cell.Position), (i + 1).ToString(), textColor);
        }
    }

    private HexWithData<CellTemplate>? GetFullCellAt(Hex position)
    {
        // Later entries are newly added cells. When an invalid overlap exists, select the last one so the player can
        // move the cell that was just added instead of accidentally moving the root cell underneath it.
        for (int i = CurrentFullLayout.Count - 1; i >= 0; --i)
        {
            var cell = CurrentFullLayout[i];

            // TODO: it would be more efficient if this data was cached (or at least we didn't generate the list
            // each time), luckily this is rarely called
            if (GetFullCellPositionsGlobal(cell).Contains(position))
                return cell;
        }

        return null;
    }

    /// <summary>
    ///   Checks that a full layout move does not introduce an overlap for the moved cell and that it remains connected
    ///   to the rest of the layout. Other existing layout errors are allowed, so they can be fixed one at a time.
    /// </summary>
    /// <returns>True if the move is valid</returns>
    private bool IsFullLayoutMoveValid(Hex position, HexWithData<CellTemplate> moving)
    {
        var occupiedByOtherCells = new HashSet<Hex>();
        var otherCells = CurrentFullLayout.Where(cell => !ReferenceEquals(cell, moving)).ToList();

        // Create a temporary "layout" to test the move
        var oldPosition = moving.Position;
        moving.Position = position;
        moving.Data!.Position = position;
        List<Hex> movingPositions;

        try
        {
            foreach (var cell in otherCells)
            {
                var positions = GetFullCellPositionsGlobal(cell);

                foreach (var finalPosition in positions)
                    occupiedByOtherCells.Add(finalPosition);
            }

            movingPositions = GetFullCellPositionsGlobal(moving);
        }
        finally
        {
            // Restore the move data we modified during the check
            moving.Position = oldPosition;
            moving.Data.Position = oldPosition;
        }

        // Existing overlaps are intentionally ignored. Only reject a destination if this move would make the moved
        // cell overlap another cell.
        if (movingPositions.Any(occupiedByOtherCells.Contains))
            return false;

        // A single-cell layout has no other cell it can touch.
        if (otherCells.Count == 0)
            return true;

        if (!movingPositions.Any(cellPosition => Hex.HexNeighbourOffset.Values.Any(offset =>
                occupiedByOtherCells.Contains(cellPosition + offset))))
        {
            // Not touching any neighbours
            return false;
        }

        // It's a bit too cumbersome to enforce adjacencies when trying to move, so this check is disabled.

        // return HasEnoughExpectedAdjacencies(moving, movingPositions);

        return true;
    }

    /// <summary>
    ///   Checks whether a cell retains enough of its statically expected compact-layout neighbours.
    /// </summary>
    private bool HasEnoughExpectedAdjacencies(HexWithData<CellTemplate> cell, IReadOnlyList<Hex> cellPositions)
    {
        int expectedAdjacencies = 0;
        int retainedAdjacencies = 0;

        foreach (var otherCell in CurrentFullLayout)
        {
            if (ReferenceEquals(cell, otherCell) || !AreExpectedManualLayoutNeighbours(cell, otherCell))
                continue;

            ++expectedAdjacencies;
            if (CellPositionsAreAdjacent(cellPositions, GetFullCellPositionsGlobal(otherCell)))
                ++retainedAdjacencies;
        }

        if (expectedAdjacencies == 0)
            return true;

        int requiredAdjacencies = Math.Max(Constants.MANUAL_LAYOUT_MINIMUM_RETAINED_CELL_ADJACENCIES,
            expectedAdjacencies - Constants.MANUAL_LAYOUT_MAX_IGNORED_CELL_ADJACENCIES);

        return retainedAdjacencies >= requiredAdjacencies;
    }

    private bool AreExpectedManualLayoutNeighbours(HexWithData<CellTemplate> first, HexWithData<CellTemplate> second)
    {
        if (!TryGetManualLayoutSourcePosition(first, out var firstSourcePosition) ||
            !TryGetManualLayoutSourcePosition(second, out var secondSourcePosition))
        {
            return false;
        }

        return firstSourcePosition.DistanceTo(secondSourcePosition) <= 1;
    }

    private bool TryGetManualLayoutSourcePosition(HexWithData<CellTemplate> cell, out Hex sourcePosition)
    {
        if (manualLayoutSources.TryGetValue(cell, out var source))
        {
            sourcePosition = source.Position;
            return true;
        }

        if (manualLayoutSourceData.TryGetValue(cell, out var sourceData))
        {
            sourcePosition = sourceData.Position;
            return true;
        }

        sourcePosition = default;
        return false;
    }

    private bool CellPositionsAreAdjacent(IReadOnlyList<Hex> firstCellPositions, IReadOnlyList<Hex> secondCellPositions)
    {
        int maximumDistance = Constants.MANUAL_LAYOUT_MAXIMUM_CELL_ADJACENCY_GAP + 1;

        return firstCellPositions.Any(firstPosition => secondCellPositions.Any(secondPosition =>
            firstPosition.DistanceTo(secondPosition) <= maximumDistance));
    }

    private void CacheMissingManualLayoutAdjacencyPairs(
        IReadOnlyDictionary<HexWithData<CellTemplate>, List<Hex>> cellPositions)
    {
        manualLayoutMissingAdjacencyPairs.Clear();

        var cells = manualFullLayout;
        for (int i = 0; i < cells.Count; ++i)
        {
            var cell = cells[i];
            for (int j = i + 1; j < cells.Count; ++j)
            {
                var otherCell = cells[j];
                if (AreExpectedManualLayoutNeighbours(cell, otherCell) &&
                    !CellPositionsAreAdjacent(cellPositions[cell], cellPositions[otherCell]) &&
                    (manualLayoutCellsWithBrokenExpectedAdjacencies.Contains(cell) ||
                        manualLayoutCellsWithBrokenExpectedAdjacencies.Contains(otherCell)))
                {
                    manualLayoutMissingAdjacencyPairs.Add((cell, otherCell));
                }
            }
        }
    }

    /// <summary>
    ///   Draws red links for expected contacts the current manual layout fails to preserve.
    /// </summary>
    private void DisplayManualLayoutAdjacencyErrors()
    {
        foreach (var pair in manualLayoutMissingAdjacencyPairs)
        {
            DisplayHexAdjacencyEffect(pair.First.Position, pair.Second.Position, string.Empty, Colors.Red);
        }
    }

    private void RenderFullLayoutMoveHover()
    {
        if (MovingPlacedHex == null)
            throw new InvalidOperationException("MovingPlacedHex is null");

        GetMouseHex(out int q, out int r);
        var moving = MovingPlacedHex;
        var type = GetEditedCellDataIfEdited(moving.Data!.ModifiableCellType);

        isPlacementProbablyValid = IsFullLayoutMoveValid(new Hex(q, r), moving);

        // These are rendered in local coordinates
        var positionsLocal = GetFullCellPositionsLocal(type)
            .Select(position => Hex.RotateAxialNTimes(position, moving.Orientation));
        RenderHoveredHex(q, r, positionsLocal, isPlacementProbablyValid, out _);

        var model = hoverModels[usedHoverModel++];
        ShowCellTypeInModelHolder(model, type, Hex.AxialToCartesian(new Hex(q, r)) + microbeModelOffset,
            moving.Orientation);
        model.Visible = true;
    }

    private void ApplyManualMove(Hex position)
    {
        if (MovingPlacedHex == null || !IsFullLayoutMoveValid(position, MovingPlacedHex))
        {
            Editor.OnInvalidAction();
            return;
        }

        MovingPlacedHex.Position = position;
        MovingPlacedHex.Data!.Position = position;
        manualFullLayout.Add(MovingPlacedHex);
        MovingPlacedHex = null;
        UpdateFullLayoutVisuals();
        OnActionStatusChanged();
    }

    private void EnterFullLayoutPreview()
    {
        layoutPreviewActive = true;
        bool refreshRequested = fullLayoutNeedsRefresh;
        fullLayoutNeedsRefresh = false;
        fullLayoutOccupied.Clear();

        // Make the normal view invisible
        MouseHoverPositions = null;
        foreach (var hex in placedHexes)
        {
            hex.Visible = false;
        }

        foreach (var model in placedModels)
        {
            model.Visible = false;
        }

        if (UsesManualPlayerLayout && manualFullLayout.Count > 0)
        {
            if (refreshRequested)
                RefreshManualFullLayoutCellTypes();

            UpdateFullLayoutVisuals();
        }
        else
        {
            StartLayoutCalculation();
        }

        UpdateArrow();
    }

    private void RefreshManualFullLayoutCellTypes()
    {
        foreach (var cell in manualFullLayout)
        {
            var sourceType = manualLayoutSources.TryGetValue(cell, out var source) ?
                source.Data!.ModifiableCellType :
                cell.Data!.ModifiableCellType;
            var type = GetEditedCellDataIfEdited(sourceType);
            cell.Data = new CellTemplate(type, cell.Position, cell.Orientation);
        }
    }

    private void ExitFullLayoutPreview()
    {
        layoutPreviewActive = false;
        MouseHoverPositions = null;
        manualLayoutHasErrors = false;
        UpdateAlreadyPlacedVisuals();
        RecalculateWrongGrowthOrderCells();
        UpdateGrowthOrderNumbers();
        UpdateArrow();
    }

    private void OnReapplyAutomaticLayoutPressed()
    {
        manualFullLayout.Clear();
        manualLayoutMissingAdjacencyPairs.Clear();
        manualLayoutSources.Clear();
        manualLayoutSourceData.Clear();
        StartLayoutCalculation();
    }

    /// <summary>
    ///   Keeps the manual preview in sync with the compact editor layout. Newly added cells are deliberately placed at
    ///   the origin so that an overlap is visible and the player can resolve it manually.
    /// </summary>
    private void SynchronizeManualLayoutWithEditorCells()
    {
        var currentCells = new HashSet<HexWithData<CellTemplate>>(editedMicrobeCells.AsModifiable(),
            ReferenceEqualityComparer.Instance);

        var mappedSources = new HashSet<HexWithData<CellTemplate>>(ReferenceEqualityComparer.Instance);

        // A main-view rotation can replace the compact wrapper while keeping the cell at the same position. Restore
        // the old manual entry by its compact position and type before treating it as a newly added cell.
        foreach (var manualCell in manualFullLayout)
        {
            if (manualLayoutSources.TryGetValue(manualCell, out var source) && currentCells.Contains(source))
            {
                mappedSources.Add(source);
                manualCell.Orientation = source.Orientation;
                manualCell.Data!.Orientation = source.Orientation;
                continue;
            }

            if (!manualLayoutSourceData.TryGetValue(manualCell, out var oldSourceData))
                continue;

            var replacement = editedMicrobeCells.AsModifiable().FirstOrDefault(candidate =>
                !mappedSources.Contains(candidate) && candidate.Position == oldSourceData.Position &&
                ReferenceEquals(candidate.Data!.ModifiableCellType, oldSourceData.Type));

            if (replacement != null)
            {
                SetManualLayoutSource(manualCell, replacement);
                mappedSources.Add(replacement);
                manualCell.Orientation = replacement.Orientation;
                manualCell.Data!.Orientation = replacement.Orientation;
            }
        }

        foreach (var manualCell in manualFullLayout.ToList())
        {
            if (!manualLayoutSources.TryGetValue(manualCell, out var source) || !mappedSources.Contains(source))
            {
                // A compact cell is temporarily absent while a move action is in progress. Keep its manual entry and
                // source metadata so the replacement wrapper can be matched when the move finishes.
                if (MovingPlacedHex != null)
                    continue;

                manualFullLayout.Remove(manualCell);
                manualLayoutSources.Remove(manualCell);
                manualLayoutSourceData.Remove(manualCell);
            }
        }

        foreach (var source in editedMicrobeCells.AsModifiable())
        {
            if (mappedSources.Contains(source))
                continue;

            // Adding new cells to the layout at 0, 0 so that they need to be fixed manually
            var type = GetEditedCellDataIfEdited(source.Data!.ModifiableCellType);
            var clone = new CellTemplate(type, new Hex(0, 0), source.Orientation);
            var manualCell = new HexWithData<CellTemplate>(clone, clone.Position, clone.Orientation);
            manualFullLayout.Add(manualCell);
            SetManualLayoutSource(manualCell, source);
        }
    }

    private sealed class LayoutCalculationResult
    {
        // public IndividualHexLayout<CellTemplate> Editor { get; set; } = null!;

        public CellLayout<CellTemplate> Gameplay { get; set; } = null!;
        public IReadOnlyList<CellTemplate> GrowthOrderSources { get; set; } = null!;
    }
}
