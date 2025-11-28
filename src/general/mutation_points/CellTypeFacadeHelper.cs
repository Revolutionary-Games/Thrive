using System;
using System.Collections;
using System.Collections.Generic;

/// <summary>
///   Helps to manage the used cell types for more advanced species that have multiple cell types
/// </summary>
public class CellTypeFacadeHelper
{
    private readonly List<IReadOnlyCellTypeDefinition> removedCellTypes = new();
    private readonly List<CellTypeEditsFacade> addedCellTypes = new();

    private readonly Dictionary<IReadOnlyCellTypeDefinition, CellTypeEditsFacade> activeCellTypes = new();
    private readonly Stack<CellTypeEditsFacade> unusedCellTypes = new();

    public int ApproximateCount => addedCellTypes.Count - removedCellTypes.Count;

    /// <summary>
    ///   Prepares this for reuse
    /// </summary>
    public void ClearUsed()
    {
        foreach (var addedCellType in activeCellTypes)
        {
            // Reset previous changes so this is ready for new changes
            addedCellType.Value.OnStartApplyChanges();
            unusedCellTypes.Push(addedCellType.Value);
        }

        activeCellTypes.Clear();

        removedCellTypes.Clear();
        addedCellTypes.Clear();
    }

    public bool HandleAction(EditorCombinableActionData actionData)
    {
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

        return false;
    }

    public void OnEditOnType(CellTypeEditsFacade targetType, CellType context)
    {
        // Make sure the original is suppressed
        if (!removedCellTypes.Contains(context))
            removedCellTypes.Add(context);

        // And that the new one is added
        if (!addedCellTypes.Contains(targetType))
            addedCellTypes.Add(targetType);
    }

    /// <summary>
    ///   Only resolves a type if we have overwritten it. So a light version of <see cref="GetOrCreateCellType"/>
    /// </summary>
    /// <param name="cellDefinition">Raw type definition</param>
    /// <returns>Either a wrapped value or the plain value if it is not overwritten</returns>
    public IReadOnlyCellDefinition? ResolveCellDefinition(IReadOnlyCellTypeDefinition? cellDefinition)
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

    public CellTypeEditsFacade GetOrCreateCellType(IReadOnlyCellTypeDefinition typeDefinition)
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

    public sealed class CellTypeEnumerator : IEnumerator<IReadOnlyCellTypeDefinition>
    {
        private readonly CellTypeFacadeHelper dataSource;

        private readonly IEnumerator<IReadOnlyCellTypeDefinition> originalReader;

        private int readIndex = -1;

        private IReadOnlyCellTypeDefinition? current;

        public CellTypeEnumerator(CellTypeFacadeHelper dataSource,
            IEnumerator<IReadOnlyCellTypeDefinition> originalReader)
        {
            this.dataSource = dataSource;
            this.originalReader = originalReader;
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
