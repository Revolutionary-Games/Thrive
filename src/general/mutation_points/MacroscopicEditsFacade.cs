using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

public sealed class MacroscopicEditsFacade : SpeciesEditsFacade, IReadOnlyMacroscopicSpecies,
    IReadOnlyList<IReadOnlyCellTypeDefinition>, IReadOnlyMacroscopicMetaballLayout
{
    private readonly IReadOnlyMacroscopicSpecies macroscopicSpecies;

    private readonly List<IReadonlyMacroscopicMetaball> removedMetaballs = new();
    private readonly List<MetaballWithOriginalReference> addedMetaballs = new();

    private readonly Stack<MetaballWithOriginalReference> unusedMetaballs = new();

    private readonly CellTypeFacadeHelper cellTypes = new();

    public MacroscopicEditsFacade(MacroscopicSpecies species) : base(species)
    {
        macroscopicSpecies = species;
    }

    public IReadOnlyList<IReadOnlyCellTypeDefinition> CellTypes => this;
    public IReadOnlyMacroscopicMetaballLayout BodyLayout => this;

    // Approximate counts
    int IReadOnlyCollection<IReadOnlyCellTypeDefinition>.Count =>
        macroscopicSpecies.CellTypes.Count + cellTypes.ApproximateCount;

    int IReadOnlyCollection<IReadonlyMacroscopicMetaball>.Count =>
        macroscopicSpecies.BodyLayout.Count + addedMetaballs.Count - removedMetaballs.Count;

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

    IEnumerator<IReadonlyMacroscopicMetaball> IEnumerable<IReadonlyMacroscopicMetaball>.GetEnumerator()
    {
        ResolveDataIfDirty();
        return new MetaballEnumerator(this);
    }

    IEnumerator<IReadOnlyCellTypeDefinition> IEnumerable<IReadOnlyCellTypeDefinition>.GetEnumerator()
    {
        ResolveDataIfDirty();
        return new CellTypeFacadeHelper.CellTypeEnumerator(cellTypes, macroscopicSpecies.CellTypes.GetEnumerator());
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return ((IEnumerable<IReadOnlyCellDefinition>)this).GetEnumerator();
    }

    internal override void OnStartApplyChanges()
    {
        base.OnStartApplyChanges();

        // Capture temporaries back
        foreach (var addedCell in addedMetaballs)
        {
            unusedMetaballs.Push(addedCell);
        }

        removedMetaballs.Clear();
        addedMetaballs.Clear();

        cellTypes.ClearUsed();
    }

    internal override bool ApplyAction(EditorCombinableActionData actionData)
    {
        if (actionData is MetaballPlacementActionData<MacroscopicMetaball> metaballPlacementActionData)
        {
            var newMetaball = GetModifiable(metaballPlacementActionData.PlacedMetaball,
                metaballPlacementActionData.Parent);

            newMetaball.Position = metaballPlacementActionData.Position;
            newMetaball.Size = metaballPlacementActionData.Size;

            addedMetaballs.Add(newMetaball);

            return true;
        }

        if (actionData is MetaballResizeActionData<MacroscopicMetaball> metaballResizeActionData)
        {
            // Find a match first if we have done something on this before
            foreach (var addedMetaball in addedMetaballs)
            {
                if (ReferenceEquals(addedMetaball.OriginalFrom, metaballResizeActionData.ResizedMetaball))
                {
                    // This is a different approach from earlier species type facades as this edits the already
                    // modified things. Which is the approach here as we already probably create a wrapper for each
                    // original body plan metaball due to the parent etc. references. So we save on that step by
                    // modifying this, which should be safe as long as all edits are in order.
                    addedMetaball.Size = metaballResizeActionData.NewSize;
                    return true;
                }
            }

            // Then need to create a new metaball as this hasn't been edited yet
            // TODO: the parent is the *final* parent here and not the exact parent at this edit's point in time
            var newMetaball = GetModifiable(metaballResizeActionData.ResizedMetaball,
                metaballResizeActionData.ResizedMetaball.ModifiableParent);

            newMetaball.Size = metaballResizeActionData.NewSize;
            addedMetaballs.Add(newMetaball);

            return true;
        }

        if (actionData is MetaballRemoveActionData<MacroscopicMetaball> metaballRemoveActionData)
        {
            if (!removedMetaballs.Contains(metaballRemoveActionData.RemovedMetaball))
                removedMetaballs.Add(metaballRemoveActionData.RemovedMetaball);

            foreach (var addedMetaball in addedMetaballs)
            {
                if (ReferenceEquals(addedMetaball.OriginalFrom, metaballRemoveActionData.RemovedMetaball))
                {
                    addedMetaballs.Remove(addedMetaball);
                    break;
                }
            }

            if (metaballRemoveActionData.ReParentedMetaballs != null)
            {
                // TODO: does this need to handle ReParentedMetaballs?
            }

            return true;
        }

        if (actionData is MetaballMoveActionData<MacroscopicMetaball> metaballMoveActionData)
        {
            // Remove if already created a metaball
            foreach (var addedMetaball in addedMetaballs)
            {
                if (ReferenceEquals(addedMetaball.OriginalFrom, metaballMoveActionData.MovedMetaball))
                {
                    addedMetaballs.Remove(addedMetaball);
                    break;
                }
            }

            // Then create a new target with the updated parent and position
            var newMetaball = GetModifiable(metaballMoveActionData.MovedMetaball, metaballMoveActionData.NewParent);
            newMetaball.Position = metaballMoveActionData.NewPosition;

            addedMetaballs.Add(newMetaball);

            // Child positions need to be applied as well
            if (metaballMoveActionData.MovedChildMetaballs != null)
            {
                foreach (var childAction in metaballMoveActionData.MovedChildMetaballs)
                {
                    if (!ApplyAction(childAction))
                    {
                        throw new Exception("Failed to apply child metaball move action");
                    }
                }
            }

            return true;
        }

        if (cellTypes.HandleAction(actionData))
            return true;

        // Need to handle edits to the cell types (by forwarding to the right facade)
        if (actionData is EditorCombinableActionData<CellType> cellTypeEdit && cellTypeEdit.Context != null)
        {
            // Get the cell type edit that matches the context
            var targetType = cellTypes.GetOrCreateCellType(cellTypeEdit.Context);

            cellTypes.OnEditOnType(targetType, cellTypeEdit.Context);

            // And then apply the change. The overall start applying changes has been called already
            if (!targetType.ApplyAction(cellTypeEdit))
                throw new Exception("Failed to apply cell type edit");

            return true;
        }

        return base.ApplyAction(actionData);
    }

    private MetaballWithOriginalReference GetModifiable(IReadonlyMacroscopicMetaball metaball, Metaball? parent)
    {
        // If this metaball is the parent of something, that needs to be handled so that it also gets the parent
        // reference correct
        UpdateParentReferencesFor(metaball);

        // Resolve the parent before creating the metaball
        var resolvedParent = parent == null ? null : ResolveParentReference(parent);

        if (unusedMetaballs.TryPop(out var existing))
        {
            existing.ReuseFor(metaball, resolvedParent);
            return existing;
        }

        return new MetaballWithOriginalReference(metaball, resolvedParent);
    }

    private MetaballWithOriginalReference ResolveParentReference(Metaball parent)
    {
        // Find a parent metaball from already added
        foreach (var alreadyAdded in addedMetaballs)
        {
            if (ReferenceEquals(alreadyAdded.OriginalFrom, parent))
                return alreadyAdded;
        }

        // Or create a new one
        // We assume all metaballs we receive are at least macroscopic so that we can cast like this here
        var macroscopicParent = (MacroscopicMetaball)parent;
        var newParent = GetModifiable(macroscopicParent, parent.ModifiableParent);

#if DEBUG
        if (addedMetaballs.Contains(newParent))
            throw new Exception("Somehow parent is already added to new");

        if (removedMetaballs.Contains(macroscopicParent))
            throw new Exception("Somehow parent is already removed");
#endif

        // Need to add it to the already added and suppress from original
        addedMetaballs.Add(newParent);
        removedMetaballs.Add(macroscopicParent);

        return newParent;
    }

    private void UpdateParentReferencesFor(IReadonlyMacroscopicMetaball metaball)
    {
        // TODO: should this enumerator here be avoided somehow?
        foreach (var oldMetaball in macroscopicSpecies.BodyLayout)
        {
            if (oldMetaball.Parent == null)
                continue;

            if (ReferenceEquals(oldMetaball.Parent, metaball))
            {
                if (removedMetaballs.Contains(oldMetaball))
                    continue;

                // Need to update this metaball as well
                // As we require a macroscopic species as input, this check should be fine
                var newData = GetModifiable(oldMetaball, (Metaball)oldMetaball.Parent);

#if DEBUG
                if (removedMetaballs.Contains(oldMetaball))
                    throw new Exception("Something unexpectedly added a processing metaball to removed");

                if (addedMetaballs.Contains(newData))
                    throw new Exception("New data somehow was already added");
#endif

                removedMetaballs.Add(oldMetaball);
                addedMetaballs.Add(newData);
            }
        }
    }

    private sealed class MetaballWithOriginalReference : MacroscopicMetaball
    {
        private MetaballWithOriginalReference? parent;

        // The underlying type should always be the same, so for now we just cast (also in ReuseFor)
        public MetaballWithOriginalReference(IReadonlyMacroscopicMetaball original,
            MetaballWithOriginalReference? parent) : base((CellType)original.CellType)
        {
            // Make sure creating further reference objects keeps the original reference
            if (original is MetaballWithOriginalReference withAncestorReference)
            {
                OriginalFrom = withAncestorReference.OriginalFrom;
            }
            else
            {
                OriginalFrom = original;
            }

            this.parent = parent;
        }

        public IReadonlyMacroscopicMetaball OriginalFrom { get; private set; }

        public override Metaball? ModifiableParent
        {
            get
            {
                if (parent == null)
                    return null;

                throw new NotImplementedException("This class cannot have modifiable parent accessed");
            }
            set => throw new NotImplementedException("This class cannot have modifiable parent set");
        }

        public override IReadOnlyMetaball? Parent => parent;

        internal void ReuseFor(IReadonlyMacroscopicMetaball original, MetaballWithOriginalReference? newParent)
        {
            if (original is MetaballWithOriginalReference withAncestorReference)
            {
                OriginalFrom = withAncestorReference.OriginalFrom;
            }
            else
            {
                OriginalFrom = original;
            }

            // Same cast reasoning as in the constructor
            ModifiableCellType = (CellType)original.CellType;
            Position = original.Position;
            Size = original.Size;
            parent = newParent;
        }
    }

    private class MetaballEnumerator : IEnumerator<IReadonlyMacroscopicMetaball>
    {
        private readonly MacroscopicEditsFacade dataSource;

        private readonly IEnumerator<IReadonlyMacroscopicMetaball> originalReader;

        private int readIndex = -1;

        private IReadonlyMacroscopicMetaball? current;

        public MetaballEnumerator(MacroscopicEditsFacade dataSource)
        {
            this.dataSource = dataSource;
            originalReader = dataSource.macroscopicSpecies.BodyLayout.GetEnumerator();
        }

        IReadonlyMacroscopicMetaball IEnumerator<IReadonlyMacroscopicMetaball>.Current =>
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
                        if (dataSource.removedMetaballs.Contains(current))
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

            if (readIndex >= dataSource.addedMetaballs.Count)
            {
                current = null;
                return false;
            }

            current = dataSource.addedMetaballs[readIndex];
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
