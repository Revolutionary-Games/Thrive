using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Godot;

public sealed class MacroscopicEditsFacade : SpeciesEditsFacade, IReadOnlyMacroscopicSpecies,
    IReadOnlyList<IReadOnlyCellTypeDefinition>, IReadOnlyMacroscopicMetaballLayout
{
    private readonly IReadOnlyMacroscopicSpecies macroscopicSpecies;

    // Due to the complexity of the parenting, we basically always build a full shadow tree
    private readonly List<MetaballWithOriginalReference> newMetaballStructure = new();

    private readonly Stack<MetaballWithOriginalReference> unusedMetaballs = new();

    private readonly CellTypeFacadeHelper cellTypes = new();

    public MacroscopicEditsFacade(IReadOnlyMacroscopicSpecies species) : base(species)
    {
        macroscopicSpecies = species;
    }

    public IReadOnlyList<IReadOnlyCellTypeDefinition> CellTypes => this;
    public IReadOnlyMacroscopicMetaballLayout BodyLayout => this;

    // Approximate counts
    int IReadOnlyCollection<IReadOnlyCellTypeDefinition>.Count =>
        macroscopicSpecies.CellTypes.Count + cellTypes.ApproximateCount;

    int IReadOnlyCollection<IReadonlyMacroscopicMetaball>.Count => newMetaballStructure.Count;

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
        return newMetaballStructure.GetEnumerator();
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
        foreach (var addedCell in newMetaballStructure)
        {
            unusedMetaballs.Push(addedCell);
        }

        newMetaballStructure.Clear();

        cellTypes.ClearUsed();

        // Populate the full metaball structure starting from the root
        foreach (var metaball in macroscopicSpecies.BodyLayout)
        {
            PopulateMetaballStructure(metaball);
        }

        // Ensure the structure got built correctly
#if DEBUG
        int roots = 0;

        foreach (var metaball in newMetaballStructure)
        {
            if (metaball.Parent == null)
            {
                ++roots;
                continue;
            }

            // Ensure all parent references exists
            bool found = false;
            foreach (var newMetaball2 in newMetaballStructure)
            {
                if (ReferenceEquals(newMetaball2, metaball.Parent))
                {
                    found = true;
                    break;
                }
            }

            if (!found)
                throw new Exception("Failed to build new metaball structure correctly");
        }

        if (roots != 1)
            throw new Exception("There should be exactly one root metaball");
#endif
    }

    internal override bool ApplyAction(EditorCombinableActionData actionData)
    {
        if (actionData is MetaballPlacementActionData<MacroscopicMetaball> metaballPlacementActionData)
        {
            var newMetaball = GetModifiable(metaballPlacementActionData.PlacedMetaball,
                metaballPlacementActionData.Parent, true);

            newMetaball.Position = metaballPlacementActionData.Position;
            newMetaball.Size = metaballPlacementActionData.Size;

#if DEBUG
            if (newMetaballStructure.Contains(newMetaball))
                throw new Exception("Somehow new metaball is already added to structure");
#endif

            newMetaballStructure.Add(newMetaball);

            return true;
        }

        if (actionData is MetaballResizeActionData<MacroscopicMetaball> metaballResizeActionData)
        {
            // Due to the full tree build, there should always be a metaball to edit
            var addedMetaball = FindMatching(metaballResizeActionData.ResizedMetaball, true);

            if (addedMetaball != null)
            {
                // This is a different approach from earlier species type facades as this edits the already
                // modified things. Which is the approach here as we already create a wrapper for each
                // original body plan metaball due to the parent etc. references.
                addedMetaball.Size = metaballResizeActionData.NewSize;
                return true;
            }

            throw new Exception("Failed to find metaball to resize");
        }

        if (actionData is MetaballRemoveActionData<MacroscopicMetaball> metaballRemoveActionData)
        {
            var removedMetaball = FindMatching(metaballRemoveActionData.RemovedMetaball, true);

            if (removedMetaball != null)
            {
                if (!newMetaballStructure.Remove(removedMetaball))
                    throw new Exception("Failed to remove metaball from structure");

                if (metaballRemoveActionData.ReParentedMetaballs != null)
                {
                    foreach (var childAction in metaballRemoveActionData.ReParentedMetaballs)
                    {
                        if (!ApplyAction(childAction))
                            throw new Exception("Failed to apply child metaball move action for a delete");
                    }
                }

                return true;
            }

            throw new Exception("Failed to find metaball to remove");
        }

        if (actionData is MetaballMoveActionData<MacroscopicMetaball> metaballMoveActionData)
        {
            var movedMetaball = FindMatching(metaballMoveActionData.MovedMetaball, true);

            if (movedMetaball == null)
            {
                // Use the actual old position to find the metaball
                foreach (var alreadyAdded in newMetaballStructure)
                {
                    // TODO: do we need inaccuracy check?
                    if (metaballMoveActionData.OldPosition == alreadyAdded.Position)
                    {
                        if ((metaballMoveActionData.OldParent == null) == (alreadyAdded.Parent == null))
                        {
                            // TODO: type check? / size check? (though those might change). Or parent's parent check?
                            movedMetaball = alreadyAdded;
                            break;
                        }
                    }
                }
            }

            if (movedMetaball != null)
            {
                // Update the positioning
                movedMetaball.Position = metaballMoveActionData.NewPosition;

                if (metaballMoveActionData.NewParent == null)
                {
                    movedMetaball.UpdateParent(null);
                }
                else
                {
                    movedMetaball.UpdateParent(
                        ResolveParentReference((IReadonlyMacroscopicMetaball)metaballMoveActionData.NewParent, true));
                }

                // Child positions need to be applied as well
                if (metaballMoveActionData.MovedChildMetaballs != null)
                {
                    foreach (var childAction in metaballMoveActionData.MovedChildMetaballs)
                    {
                        if (!ApplyAction(childAction))
                            throw new Exception("Failed to apply child metaball move action");
                    }
                }

                return true;
            }

            throw new Exception("Failed to find metaball to move");
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

    private MetaballWithOriginalReference GetModifiable(IReadonlyMacroscopicMetaball metaball, Metaball? parent,
        bool positionMatchParent)
    {
        // Resolve the parent before creating the metaball
        var resolvedParent = parent == null ?
            null :
            ResolveParentReference((IReadonlyMacroscopicMetaball)parent, positionMatchParent);

        if (unusedMetaballs.TryPop(out var existing))
        {
            existing.ReuseFor(metaball, resolvedParent);

            // As this can be called for creating a parent metaball, we need to make sure we copy the initial
            // properties always
            CopyMetaballProperties(existing, metaball);
            return existing;
        }

        var result = new MetaballWithOriginalReference(metaball, resolvedParent);

        CopyMetaballProperties(result, metaball);
        return result;
    }

    /// <summary>
    ///   Due to the way the actions are structure the new metaball layout in the editor is not necessarily fully
    ///   related in terms of objects to the original, so we need to do some pretty complex matching to find the
    ///   related object in our new metaball structure.
    /// </summary>
    /// <returns>Found metaball or null</returns>
    private MetaballWithOriginalReference? FindMatching(IReadonlyMacroscopicMetaball original, bool positionMatch)
    {
        foreach (var alreadyAdded in newMetaballStructure)
        {
            if (ReferenceEquals(alreadyAdded.OriginalFrom, original))
                return alreadyAdded;
        }

        if (positionMatch)
        {
            foreach (var alreadyAdded in newMetaballStructure)
            {
                // TODO: do we need inaccuracy check?
                if (original.Position == alreadyAdded.Position)
                {
                    if ((original.Parent == null) == (alreadyAdded.Parent == null))
                    {
                        // TODO: type check? / size check? (though those might change). Or parent's parent check?
                        return alreadyAdded;
                    }
                }
            }
        }

        return null;
    }

    private MetaballWithOriginalReference ResolveParentReference(IReadonlyMacroscopicMetaball parent, bool fuzzyMatch)
    {
        // Find a parent metaball from already added
        var existing = FindMatching(parent, fuzzyMatch);

        if (existing != null)
            return existing;

#if DEBUG
        foreach (var alreadyAdded in newMetaballStructure)
        {
            if (alreadyAdded.Position == parent.Position)
            {
                throw new Exception("Metaball position is already used, but reference did not match for some reason");
            }
        }
#endif

        // Or create a new one
        // Kind of dirty to cast away the readonly here, but we don't need to modify the original anyway
        var macroscopicParent = (MacroscopicMetaball)parent;
        var newParent = GetModifiable(macroscopicParent, macroscopicParent.ModifiableParent, fuzzyMatch);

#if DEBUG
        if (newMetaballStructure.Contains(newParent))
            throw new Exception("Somehow parent is already added to new");

        if (!ReferenceEquals(GetModifiable(macroscopicParent, macroscopicParent.ModifiableParent, fuzzyMatch),
                newParent))
        {
            throw new Exception("Failed to create new parent metaball correctly");
        }
#endif

        // Need to add it to the already added
        newMetaballStructure.Add(newParent);

#if DEBUG
        if (!ReferenceEquals(ResolveParentReference(parent, fuzzyMatch), newParent))
        {
            throw new Exception("Failed to resolve parent reference correctly");
        }
#endif

        return newParent;
    }

    private void PopulateMetaballStructure(IReadonlyMacroscopicMetaball start)
    {
        // Skip already seen ones
        foreach (var alreadyCreated in newMetaballStructure)
        {
            if (ReferenceEquals(alreadyCreated.OriginalFrom, start))
                return;
        }

        // We only work with macroscopic species
        var newMetaball = GetModifiable(start, (MacroscopicMetaball?)start.Parent, false);

        CopyMetaballProperties(newMetaball, start);

        newMetaballStructure.Add(newMetaball);
    }

    /// <summary>
    ///   Copies properties from the source metaball to the target metaball except the parent
    /// </summary>
    private void CopyMetaballProperties(MetaballWithOriginalReference target, IReadonlyMacroscopicMetaball source)
    {
        target.Size = source.Size;
        target.Position = source.Position;

        // TODO: once type comparisons are done, we need to copy the type here as well
        // Might need to do something like cellTypes.GetOrCreateCellType(source.CellType)
        // target.ModifiableCellType = source.CellType;
    }

    private sealed class MetaballWithOriginalReference : MacroscopicMetaball
    {
        private MetaballWithOriginalReference? parent;

        // The underlying type should always be the same, so for now we just cast (also in ReuseFor)
        public MetaballWithOriginalReference(IReadonlyMacroscopicMetaball original,
            MetaballWithOriginalReference? parent) : base((CellType)original.CellType)
        {
            if (original is MetaballWithOriginalReference)
            {
                throw new ArgumentException("This shouldn't be made recursively");
            }

            OriginalFrom = original;
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

        public void UpdateParent(MetaballWithOriginalReference? newParent)
        {
            parent = newParent;
        }

        internal void ReuseFor(IReadonlyMacroscopicMetaball original, MetaballWithOriginalReference? newParent)
        {
            if (original is MetaballWithOriginalReference)
            {
                throw new ArgumentException("This shouldn't be made recursively");
            }

            OriginalFrom = original;

            // Same cast reasoning as in the constructor
            ModifiableCellType = (CellType)original.CellType;
            Position = original.Position;
            Size = original.Size;
            parent = newParent;

            // TODO: type once that is tracked
        }
    }
}
