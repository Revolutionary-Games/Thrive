using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

public sealed class MacroscopicEditsFacade : SpeciesEditsFacade, IReadOnlyMacroscopicSpecies,
    IReadOnlyList<IReadOnlyCellDefinition>, IReadOnlyMacroscopicMetaballLayout
{
    private readonly IReadOnlyMacroscopicSpecies macroscopicSpecies;

    private readonly List<IReadonlyMacroscopicMetaball> removedMetaballs = new();
    private readonly List<MetaballWithOriginalReference> addedMetaballs = new();

    private readonly List<IReadOnlyCellDefinition> removedCellTypes = new();
    private readonly List<IReadOnlyCellDefinition> addedCellTypes = new();

    private readonly Stack<MetaballWithOriginalReference> unusedMetaballs = new();

    public MacroscopicEditsFacade(MacroscopicSpecies species) : base(species)
    {
        macroscopicSpecies = species;
    }

    public IReadOnlyList<IReadOnlyCellDefinition> CellTypes => this;
    public IReadOnlyMacroscopicMetaballLayout BodyLayout => this;

    // Approximate counts
    int IReadOnlyCollection<IReadOnlyCellDefinition>.Count =>
        macroscopicSpecies.CellTypes.Count + addedCellTypes.Count - removedCellTypes.Count;

    int IReadOnlyCollection<IReadonlyMacroscopicMetaball>.Count =>
        macroscopicSpecies.BodyLayout.Count + addedMetaballs.Count - removedMetaballs.Count;

    public IReadOnlyCellDefinition this[int index]
    {
        // Extremely inefficient, but we don't need this really
        get
        {
            ResolveDataIfDirty();
            return ((IEnumerable<IReadOnlyCellDefinition>)this).Skip(index).First();
        }
        set => throw new NotSupportedException("Facade cannot set cells by index");
    }

    IEnumerator<IReadonlyMacroscopicMetaball> IEnumerable<IReadonlyMacroscopicMetaball>.GetEnumerator()
    {
        ResolveDataIfDirty();
        return new MetaballEnumerator(this);
    }

    IEnumerator<IReadOnlyCellDefinition> IEnumerable<IReadOnlyCellDefinition>.GetEnumerator()
    {
        ResolveDataIfDirty();
        return new CellTypeEnumerator(this);
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

        removedCellTypes.Clear();
        addedCellTypes.Clear();
    }

    internal override bool ApplyAction(EditorCombinableActionData actionData)
    {
        if (actionData is MetaballPlacementActionData<MacroscopicMetaball> metaballPlacementActionData)
        {
            throw new NotImplementedException();

            return true;
        }

        if (actionData is MetaballResizeActionData<MacroscopicMetaball> metaballResizeActionData)
        {
            throw new NotImplementedException();

            return true;
        }

        if (actionData is MetaballRemoveActionData<MacroscopicMetaball> metaballRemoveActionData)
        {
            throw new NotImplementedException();

            return true;
        }

        if (actionData is MetaballMoveActionData<MacroscopicMetaball> metaballMoveActionData)
        {
            throw new NotImplementedException();

            return true;
        }

        return base.ApplyAction(actionData);
    }

    private MetaballWithOriginalReference GetModifiable(IReadonlyMacroscopicMetaball metaball)
    {
        if (unusedMetaballs.TryPop(out var existing))
        {
            existing.ReuseFor(metaball);
            return existing;
        }

        return new MetaballWithOriginalReference(metaball);
    }

    private sealed class MetaballWithOriginalReference : MacroscopicMetaball
    {
        // The underlying type should always be the same, so for now we just cast (also in ReuseFor)
        public MetaballWithOriginalReference(IReadonlyMacroscopicMetaball original) : base((CellType)original.CellType)
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
        }

        public IReadonlyMacroscopicMetaball OriginalFrom { get; private set; }

        internal void ReuseFor(IReadonlyMacroscopicMetaball original)
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

            // TODO: reset other properties as well
            throw new NotImplementedException();
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

    private class CellTypeEnumerator : IEnumerator<IReadOnlyCellDefinition>
    {
        private readonly MacroscopicEditsFacade dataSource;

        private readonly IEnumerator<IReadOnlyCellDefinition> originalReader;

        private int readIndex = -1;

        private IReadOnlyCellDefinition? current;

        public CellTypeEnumerator(MacroscopicEditsFacade dataSource)
        {
            this.dataSource = dataSource;
            originalReader = dataSource.macroscopicSpecies.CellTypes.GetEnumerator();
        }

        IReadOnlyCellDefinition IEnumerator<IReadOnlyCellDefinition>.Current =>
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
