using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Godot;

public sealed class MulticellularEditsFacade : SpeciesEditsFacade, IReadOnlyMulticellularSpecies,
    IReadOnlyCellLayout<IReadOnlyCellTemplate>, IReadOnlyList<IReadOnlyCellTypeDefinition>
{
    private readonly IReadOnlyMulticellularSpecies multicellularSpecies;

    private readonly List<IReadOnlyCellTemplate> removedCells = new();
    private readonly List<CellWithOriginalReference> addedCells = new();

    private readonly Stack<CellWithOriginalReference> unusedCells = new();

    private readonly CellTypeFacadeHelper cellTypes = new();

    public MulticellularEditsFacade(IReadOnlyMulticellularSpecies species) : base(species)
    {
        multicellularSpecies = species;
    }

    public IReadOnlyList<IReadOnlyCellTypeDefinition> CellTypes => this;

    public IReadOnlyCellLayout<IReadOnlyCellTemplate> CellLayout => this;

    // Approximate counts
    public int Count => multicellularSpecies.CellLayout.Count + addedCells.Count - removedCells.Count;

    int IReadOnlyCollection<IReadOnlyCellTypeDefinition>.Count =>
        multicellularSpecies.CellTypes.Count + cellTypes.ApproximateCount;

    public IReadOnlyCellTypeDefinition this[int index]
    {
        // Extremely inefficient, but we don't need this really
        get
        {
            ResolveDataIfDirty();
            return ((IEnumerable<IReadOnlyCellTypeDefinition>)this).Skip(index).First();
        }
        set => throw new NotSupportedException("Facade cannot set cells by index");
    }

    public IEnumerator<IReadOnlyCellTemplate> GetEnumerator()
    {
        ResolveDataIfDirty();
        return new CellEnumerator(this);
    }

    IEnumerator<IReadOnlyCellTypeDefinition> IEnumerable<IReadOnlyCellTypeDefinition>.GetEnumerator()
    {
        ResolveDataIfDirty();
        return new CellTypeFacadeHelper.CellTypeEnumerator(cellTypes, multicellularSpecies.CellTypes.GetEnumerator());
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return ((IEnumerable<IReadOnlyCellDefinition>)this).GetEnumerator();
    }

    public IReadOnlyCellTemplate? GetElementAt(Hex location, List<Hex> temporaryHexesStorage)
    {
        ResolveDataIfDirty();
        var originalItem = multicellularSpecies.CellLayout.GetElementAt(location, temporaryHexesStorage);

        if (originalItem != null && !removedCells.Contains(originalItem))
            return originalItem;

        foreach (var addedCell in addedCells)
        {
            if (addedCell.Position == location)
                return addedCell;
        }

        return null;
    }

    public IReadOnlyCellTemplate? GetByExactElementRootPosition(Hex location)
    {
        ResolveDataIfDirty();

        // This is basically the same as above as cells only occupy a single hex
        var originalItem = multicellularSpecies.CellLayout.GetByExactElementRootPosition(location);

        if (originalItem != null && !removedCells.Contains(originalItem))
            return originalItem;

        foreach (var addedCell in addedCells)
        {
            if (addedCell.Position == location)
                return addedCell;
        }

        return null;
    }

    internal override void OnStartApplyChanges()
    {
        base.OnStartApplyChanges();

        // Capture temporaries back
        foreach (var addedCell in addedCells)
        {
            unusedCells.Push(addedCell);
        }

        removedCells.Clear();
        addedCells.Clear();

        cellTypes.ClearUsed();
    }

    internal override bool ApplyAction(EditorCombinableActionData actionData)
    {
        if (actionData is CellPlacementActionData cellPlacementActionData)
        {
            var newCell = GetModifiable(cellPlacementActionData.PlacedHex.Data ??
                throw new Exception("Action hex has no data"));

            newCell.Position = cellPlacementActionData.Location;
            newCell.Orientation = cellPlacementActionData.Orientation;

            addedCells.Add(newCell);

#if DEBUG
            if (cellTypes.ResolveCellDefinition(newCell.Data.CellType) !=
                cellTypes.ResolveCellDefinition(cellPlacementActionData.PlacedHex.CellType))
            {
                throw new Exception("Failed to setup cell type correctly");
            }
#endif
            return true;
        }

        if (actionData is CellMoveActionData cellMoveActionData)
        {
            IReadOnlyCellTemplate? original = null;

            // Find a match first if we have done something on this before
            foreach (var addedCell in addedCells)
            {
                if (addedCell.Position == cellMoveActionData.OldLocation &&
                    addedCell.Orientation == cellMoveActionData.OldRotation)
                {
                    original = addedCell;

                    if (cellTypes.ResolveCellDefinition(original.CellType) !=
                        cellTypes.ResolveCellDefinition(cellMoveActionData.MovedHex.CellType))
                    {
                        throw new InvalidOperationException("Found an unrelated cell at move old location");
                    }

                    addedCells.Remove(addedCell);
                    break;
                }
            }

            if (original == null)
            {
                // Then match to the original body plan
                original =
                    multicellularSpecies.CellLayout.GetByExactElementRootPosition(cellMoveActionData.OldLocation);

                if (original != null)
                {
                    if (cellTypes.ResolveCellDefinition(original.CellType) !=
                        cellTypes.ResolveCellDefinition(cellMoveActionData.MovedHex.CellType))
                    {
                        GD.PrintErr("Found unrelated cell at exact position of moved cell");
                    }

                    // Don't want the old instance to show up any more
                    removedCells.Add(original);
                }
            }

            if (original == null)
                throw new InvalidOperationException("Could not find the cell a move operation is related to");

            var modifiable = GetModifiable(original);
            modifiable.Position = cellMoveActionData.NewLocation;
            modifiable.Orientation = cellMoveActionData.NewRotation;

            addedCells.Add(modifiable);
            return true;
        }

        if (actionData is CellRemoveActionData cellRemoveActionData)
        {
            IReadOnlyCellTemplate? original = null;

            // Find a match first if we have done something to this before
            foreach (var addedCell in addedCells)
            {
                if (addedCell.Position == cellRemoveActionData.Location &&
                    addedCell.Orientation == cellRemoveActionData.Orientation)
                {
                    original = addedCell;

                    if (cellTypes.ResolveCellDefinition(original.CellType) !=
                        cellTypes.ResolveCellDefinition(cellRemoveActionData.RemovedHex.CellType))
                    {
                        throw new InvalidOperationException("Found an unrelated cell at delete location");
                    }

                    addedCells.Remove(addedCell);
                    break;
                }
            }

            if (original == null)
            {
                // Then match to the originals
                original =
                    multicellularSpecies.CellLayout.GetByExactElementRootPosition(cellRemoveActionData.Location);

                if (original != null)
                {
                    if (cellTypes.ResolveCellDefinition(original.CellType) !=
                        cellTypes.ResolveCellDefinition(cellRemoveActionData.RemovedHex.CellType))
                    {
                        GD.PrintErr("Found unrelated cell at exact position of removed cell");
                    }

                    // Don't want the old instance to show up any more
                    removedCells.Add(original);
                }
            }

            if (original == null)
                throw new InvalidOperationException("Could not find the cell a remove operation is related to");

            // We already removed the original, so there's nothing more to do

            return true;
        }

        if (cellTypes.HandleAction(actionData))
            return true;

        // Need to handle edits to the cell types (by forwarding to the right facade)
        if (actionData is EditorCombinableActionData<CellType> cellTypeEdit)
        {
            if (cellTypeEdit.Context != null)
            {
                // Get the cell type edit that matches the context
                var targetType = cellTypes.GetOrCreateCellType(cellTypeEdit.Context);

                cellTypes.OnEditOnType(targetType, cellTypeEdit.Context);

                // And then apply the change. The overall start applying changes has been called already
                if (!targetType.ApplyAction(cellTypeEdit))
                    throw new Exception("Failed to apply cell type edit");

                return true;
            }

            GD.PrintErr("Cell type edit without context");

#if DEBUG
            if (Debugger.IsAttached)
                Debugger.Break();
#endif
        }

        return base.ApplyAction(actionData);
    }

    private CellWithOriginalReference GetModifiable(IReadOnlyCellTemplate cellTemplate)
    {
        if (unusedCells.TryPop(out var existing))
        {
            existing.ReuseFor(cellTemplate, cellTypes.GetOrCreateCellType(cellTemplate.CellType));
            return existing;
        }

        return new CellWithOriginalReference(cellTemplate, cellTypes.GetOrCreateCellType(cellTemplate.CellType));
    }

    private sealed class CellWithOriginalReference : IReadOnlyCellTemplate
    {
        private IReadOnlyCellTypeDefinition usingCustomType;

        // The underlying cell type is always the same, so we just cast without caring here (also in ReuseFor)
        public CellWithOriginalReference(IReadOnlyCellTemplate original,
            CellTypeEditsFacade typeWrapper)
        {
            Position = original.Position;
            Orientation = original.Orientation;

            usingCustomType = typeWrapper;

            // Make sure creating further reference objects keeps the original reference
            if (original is CellWithOriginalReference withAncestorReference)
            {
                OriginalFrom = withAncestorReference.OriginalFrom;
            }
            else
            {
                OriginalFrom = original;
            }
        }

        public Hex Position { get; set; }
        public int Orientation { get; set; }
        public IReadOnlyCellTemplate Data => this;

        public IReadOnlyCellTypeDefinition CellType => usingCustomType;

        public IReadOnlyCellTemplate OriginalFrom { get; private set; }

        // Data forwarding for the interface implementation
        public IReadOnlyOrganelleLayout<IReadOnlyOrganelleTemplate> Organelles => CellType.Organelles;
        public MembraneType MembraneType => CellType.MembraneType;
        public float MembraneRigidity => CellType.MembraneRigidity;
        public Color Colour => CellType.Colour;
        public bool IsBacteria => CellType.IsBacteria;

        public bool MatchesDefinition(IActionHex other)
        {
            return OriginalFrom.CellType == ((IReadOnlyCellTemplate)other).CellType;
        }

        internal void ReuseFor(IReadOnlyCellTemplate original, CellTypeEditsFacade typeWrapper)
        {
            if (original is CellWithOriginalReference withAncestorReference)
            {
                OriginalFrom = withAncestorReference.OriginalFrom;
            }
            else
            {
                OriginalFrom = original;
            }

            usingCustomType = typeWrapper;

            Position = original.Position;
            Orientation = original.Orientation;
        }
    }

    private class CellEnumerator : IEnumerator<IReadOnlyCellTemplate>
    {
        private readonly MulticellularEditsFacade dataSource;

        private readonly IEnumerator<IReadOnlyCellTemplate> originalReader;

        private int readIndex = -1;

        private IReadOnlyCellTemplate? current;

        public CellEnumerator(MulticellularEditsFacade dataSource)
        {
            this.dataSource = dataSource;
            originalReader = dataSource.multicellularSpecies.CellLayout.GetEnumerator();
        }

        IReadOnlyCellTemplate IEnumerator<IReadOnlyCellTemplate>.Current =>
            current ?? throw new InvalidOperationException("No element");

        object? IEnumerator.Current => current;

        public bool MoveNext()
        {
            if (readIndex == -1)
            {
                // Reading original items
                while (true)
                {
                    if (originalReader.MoveNext())
                    {
                        current = originalReader.Current;

                        // Need to read the next item if we are ignoring this item
                        if (dataSource.removedCells.Contains(current))
                            continue;

                        // Otherwise we found a good item
                        return true;
                    }

                    // Original items ended
                    break;
                }
            }

            // Reading extra items now
            ++readIndex;

            if (readIndex >= dataSource.addedCells.Count)
            {
                current = null;
                return false;
            }

            current = dataSource.addedCells[readIndex];
            return true;
        }

        public void Reset()
        {
            current = null;
            readIndex = -1;
            originalReader.Reset();
        }

        public void Dispose()
        {
            originalReader.Dispose();
        }
    }
}
