using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Godot;

public sealed class MulticellularEditsFacade : SpeciesEditsFacade, IReadOnlyMulticellularSpecies,
    IReadOnlyCellLayout<IReadOnlyCellTemplate>, IReadOnlyList<IReadOnlyCellTypeDefinition>
{
    private readonly IReadOnlyMulticellularSpecies multicellularSpecies;

    private readonly List<IReadOnlyCellTemplate> removedCells = new();
    private readonly List<CellWithOriginalReference> addedCells = new();

    private readonly List<IReadOnlyCellTypeDefinition> removedCellTypes = new();
    private readonly List<CellTypeEditsFacade> addedCellTypes = new();

    private readonly Stack<CellWithOriginalReference> unusedCells = new();

    private readonly Dictionary<IReadOnlyCellTypeDefinition, CellTypeEditsFacade> activeCellTypes = new();
    private readonly Stack<CellTypeEditsFacade> unusedCellTypes = new();

    public MulticellularEditsFacade(IReadOnlyMulticellularSpecies species) : base(species)
    {
        multicellularSpecies = species;
    }

    public IReadOnlyCellLayout<IReadOnlyCellTemplate> Cells => this;
    public IReadOnlyList<IReadOnlyCellTypeDefinition> CellTypes => this;

    // Approximate counts
    int IReadOnlyCollection<IReadOnlyCellTemplate>.Count =>
        multicellularSpecies.Cells.Count + addedCells.Count - removedCells.Count;

    int IReadOnlyCollection<IReadOnlyCellTypeDefinition>.Count =>
        multicellularSpecies.CellTypes.Count + addedCellTypes.Count - removedCellTypes.Count;

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

    IEnumerator<IReadOnlyCellTypeDefinition> IEnumerable<IReadOnlyCellTypeDefinition>.GetEnumerator()
    {
        ResolveDataIfDirty();
        return new CellTypeEnumerator(this);
    }

    IEnumerator<IReadOnlyCellTemplate> IEnumerable<IReadOnlyCellTemplate>.GetEnumerator()
    {
        ResolveDataIfDirty();
        return new CellEnumerator(this);
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return ((IEnumerable<IReadOnlyCellDefinition>)this).GetEnumerator();
    }

    public IReadOnlyCellTemplate? GetElementAt(Hex location, List<Hex> temporaryHexesStorage)
    {
        ResolveDataIfDirty();
        var originalItem = multicellularSpecies.Cells.GetElementAt(location, temporaryHexesStorage);

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
        var originalItem = multicellularSpecies.Cells.GetByExactElementRootPosition(location);

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

        foreach (var addedCellType in activeCellTypes)
        {
            unusedCellTypes.Push(addedCellType.Value);
        }

        activeCellTypes.Clear();

        removedCellTypes.Clear();
        addedCellTypes.Clear();
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
            if (ResolveCellDefinition(newCell.CellType) !=
                ResolveCellDefinition(cellPlacementActionData.PlacedHex.Data?.CellType))
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
            foreach (var addedOrganelle in addedCells)
            {
                if (addedOrganelle.Position == cellMoveActionData.OldLocation &&
                    addedOrganelle.Orientation == cellMoveActionData.OldRotation)
                {
                    original = addedOrganelle;

                    if (ResolveCellDefinition(original.CellType) !=
                        ResolveCellDefinition(cellMoveActionData.MovedHex.Data?.CellType))
                    {
                        throw new InvalidOperationException("Found an unrelated cell at move old location");
                    }

                    addedCells.Remove(addedOrganelle);
                    break;
                }
            }

            if (original == null)
            {
                // Then match to the original body plan
                original = multicellularSpecies.Cells.GetByExactElementRootPosition(cellMoveActionData.OldLocation);

                if (original != null)
                {
                    if (ResolveCellDefinition(original.CellType) !=
                        ResolveCellDefinition(cellMoveActionData.MovedHex.Data?.CellType))
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
            foreach (var addedOrganelle in addedCells)
            {
                if (addedOrganelle.Position == cellRemoveActionData.Location &&
                    addedOrganelle.Orientation == cellRemoveActionData.Orientation)
                {
                    original = addedOrganelle;

                    if (ResolveCellDefinition(original.CellType) !=
                        ResolveCellDefinition(cellRemoveActionData.RemovedHex.Data?.CellType))
                    {
                        throw new InvalidOperationException("Found an unrelated cell at delete location");
                    }

                    addedCells.Remove(addedOrganelle);
                    break;
                }
            }

            if (original == null)
            {
                // Then match to the originals
                original = multicellularSpecies.Cells.GetByExactElementRootPosition(cellRemoveActionData.Location);

                if (original != null)
                {
                    if (ResolveCellDefinition(original.CellType) !=
                        ResolveCellDefinition(cellRemoveActionData.RemovedHex.Data?.CellType))
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

        if (actionData is DuplicateDeleteCellTypeData cellTypeActionData)
        {
            // Make sure the original is suppressed
            if (!removedCellTypes.Contains(cellTypeActionData.CellType))
                removedCellTypes.Add(cellTypeActionData.CellType);

            if (cellTypeActionData.Delete)
            {
                // We don't need to remove from the added cell types if the cell type has never been seen before, so
                // this is purely a deletion of an original type
                if (activeCellTypes.ContainsKey(cellTypeActionData.CellType))
                {
                    if (!addedCellTypes.Remove(GetOrCreateCellType(cellTypeActionData.CellType)))
                        throw new InvalidOperationException("Cell type not found for delete");
                }
            }
            else
            {
                addedCellTypes.Add(GetOrCreateCellType(cellTypeActionData.CellType));
            }

            return true;
        }

        // Need to handle edits to the cell types
        if (actionData is EditorCombinableActionData<CellType> cellTypeEdit && cellTypeEdit.Context != null)
        {
            // Get the cell type edit that matches the context
            var targetType = GetOrCreateCellType(cellTypeEdit.Context);

            // Make sure the original is suppressed
            if (!removedCellTypes.Contains(cellTypeEdit.Context))
                removedCellTypes.Add(cellTypeEdit.Context);

            // And that the new one is added
            if (!addedCellTypes.Contains(targetType))
                addedCellTypes.Add(targetType);

            // And then apply the change. The overall start applying changes has been called already
            if (!targetType.ApplyAction(cellTypeEdit))
                throw new Exception("Failed to apply cell type edit");

            return true;
        }

        return base.ApplyAction(actionData);
    }

    private CellWithOriginalReference GetModifiable(IReadOnlyCellTemplate cellTemplate)
    {
        if (unusedCells.TryPop(out var existing))
        {
            existing.ReuseFor(cellTemplate, GetOrCreateCellType(cellTemplate.CellType));
            return existing;
        }

        return new CellWithOriginalReference(cellTemplate, GetOrCreateCellType(cellTemplate.CellType));
    }

    /// <summary>
    ///   Only resolves a type if we have overwritten it. So a light version of <see cref="GetOrCreateCellType"/>
    /// </summary>
    /// <param name="cellDefinition">Raw type definition</param>
    /// <returns>Either a wrapped value or the plain value if it is not overwritten</returns>
    private IReadOnlyCellDefinition? ResolveCellDefinition(IReadOnlyCellTypeDefinition? cellDefinition)
    {
        if (cellDefinition == null)
            return cellDefinition;

        // If it is a facade type, it is already the result
        if (cellDefinition is CellTypeEditsFacade facade)
        {
            return facade;
        }

        if (activeCellTypes.TryGetValue(cellDefinition, out var existing))
            return existing;

        return cellDefinition;
    }

    private CellTypeEditsFacade GetOrCreateCellType(IReadOnlyCellTypeDefinition typeDefinition)
    {
        // If the type is already a facade, we can just return it directly
        if (typeDefinition is CellTypeEditsFacade facade)
            return facade;

        if (activeCellTypes.TryGetValue(typeDefinition, out var existing))
            return existing;

        if (unusedCellTypes.TryPop(out var existingType))
        {
            existingType.SwitchBase(typeDefinition);
        }
        else
        {
            existingType = new CellTypeEditsFacade(typeDefinition);
        }

        existingType.BecomeUsedByTopLevelFacade();

        // Let go of internal data here / prepare for applying the changes one by one we encounter for this
        existingType.OnStartApplyChanges();

        activeCellTypes.Add(typeDefinition, existingType);
        return existingType;
    }

    private sealed class CellWithOriginalReference : CellTemplate
    {
        private IReadOnlyCellTypeDefinition usingCustomType;

        // The underlying cell type is always the same, so we just cast without caring here (also in ReuseFor)
        public CellWithOriginalReference(IReadOnlyCellTemplate original, CellTypeEditsFacade typeWrapper) :
            base(original is CellWithOriginalReference alreadyWrapped ?
                    (CellType)alreadyWrapped.OriginalFrom.CellType :
                    (CellType)original.CellType,
                original.Position, original.Orientation)
        {
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

        public IReadOnlyCellTemplate OriginalFrom { get; private set; }

        public override CellType ModifiableCellType
        {
            get
            {
                // Cast likely fails, but we would Throw an exception anyway here
                return (CellType)usingCustomType;
            }
            protected set => throw new NotImplementedException("This doesn't support setting modifiable type");
        }

        public override IReadOnlyCellTypeDefinition CellType => usingCustomType;

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

            // Same cast reasoning as in the constructor
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
            originalReader = dataSource.multicellularSpecies.Cells.GetEnumerator();
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

    private class CellTypeEnumerator : IEnumerator<IReadOnlyCellTypeDefinition>
    {
        private readonly MulticellularEditsFacade dataSource;

        private readonly IEnumerator<IReadOnlyCellTypeDefinition> originalReader;

        private int readIndex = -1;

        private IReadOnlyCellTypeDefinition? current;

        public CellTypeEnumerator(MulticellularEditsFacade dataSource)
        {
            this.dataSource = dataSource;
            originalReader = dataSource.multicellularSpecies.CellTypes.GetEnumerator();
        }

        IReadOnlyCellTypeDefinition IEnumerator<IReadOnlyCellTypeDefinition>.Current =>
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
                        if (dataSource.removedCellTypes.Contains(current))
                            continue;

                        return true;
                    }

                    // Original items ended
                    break;
                }
            }

            // Reading extra items now
            ++readIndex;

            if (readIndex >= dataSource.addedCellTypes.Count)
            {
                current = null;
                return false;
            }

            current = dataSource.addedCellTypes[readIndex];
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
