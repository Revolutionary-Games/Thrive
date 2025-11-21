using System;
using System.Collections.Generic;

public class MacroscopicSpeciesComparer
{
    private readonly MicrobeSpeciesComparer cellTypeComparer = new();

    private readonly List<IReadOnlyCellTypeDefinition> originalCellTypes = new();
    private readonly List<IReadOnlyCellTypeDefinition> newCellTypes = new();

    private readonly List<IReadonlyMacroscopicMetaball> unusedOldMetaballs = new();
    private readonly List<IReadonlyMacroscopicMetaball> unusedNewMetaballs = new();

    public double Compare(IReadOnlyMacroscopicSpecies speciesA, IReadOnlyMacroscopicSpecies speciesB)
    {
        // Base cost
        double cost = SpeciesComparer.GetRequiredMutationPoints(speciesA, speciesB);

        originalCellTypes.AddRange(speciesA.CellTypes);
        newCellTypes.AddRange(speciesB.CellTypes);

        // Cost from each cell type change
        cost += MulticellularSpeciesComparer.CompareCellTypes(originalCellTypes, newCellTypes, cellTypeComparer) *
            Constants.MULTICELLULAR_EDITOR_COST_FACTOR;
        originalCellTypes.Clear();
        newCellTypes.Clear();

        unusedOldMetaballs.AddRange(speciesA.BodyLayout);
        unusedNewMetaballs.AddRange(speciesB.BodyLayout);

        // Then body plan change costs
        var rootA = GetRoot(unusedOldMetaballs);
        var rootB = GetRoot(unusedNewMetaballs);

        cost += RecursiveCompareChanges(rootA, rootB);

        // All still left metaballs that are unused were removed
        cost += unusedOldMetaballs.Count * Constants.METABALL_REMOVE_COST;
        unusedOldMetaballs.Clear();

        // And added unused are new metaballs
        cost += unusedNewMetaballs.Count * Constants.METABALL_ADD_COST;
        unusedNewMetaballs.Clear();

        return cost;
    }

    private double RecursiveCompareChanges(IReadonlyMacroscopicMetaball metaballA,
        IReadonlyMacroscopicMetaball metaballB)
    {
        // Mark both as consumed
        unusedOldMetaballs.Remove(metaballA);
        unusedNewMetaballs.Remove(metaballB);

        double cost = 0;

        if (Math.Abs(metaballA.Size - metaballB.Size) > 0.01f)
        {
            // TODO: scale cost based on the size change
            cost += Constants.METABALL_RESIZE_COST;
        }

        var basePositionA = metaballA.Position;
        var basePositionB = metaballB.Position;

        // Compare their children

        // As this is recursive, we need new lists. Using a stack allocated list might be doable, but maybe the entire
        // algorithm needs more help from the metaball structure instead?
        // Span<int> childIndicesA = stackalloc int[250];
        // Span<int> childIndicesB = stackalloc int[250];

        var childListA = GetChildren(metaballA, unusedOldMetaballs);

        // If one of the lists is empty, then the other doesn't matter, because we can just let the metaballs go
        // unused, and the final step of the cost calculation will take care of that.
        if (childListA == null)
            return cost;

        var childListB = GetChildren(metaballB, unusedNewMetaballs);

        if (childListB == null)
            return cost;

        foreach (var childA in childListA)
        {
            var childARelativePosition = childA.Position - basePositionA;

            IReadonlyMacroscopicMetaball? matchingChildB = null;
            float previousBestDistance = float.MaxValue;

            // TODO: should there be a cost for metaballs to change type?

            // Find the closest relative position to anchor the layouts together even when the object "pointers"
            // do not match
            foreach (var childB in childListB)
            {
                var childBRelativePosition = childB.Position - basePositionB;
                var distance = childARelativePosition.DistanceSquaredTo(childBRelativePosition);

                if (matchingChildB == null)
                {
                    matchingChildB = childB;
                    previousBestDistance = distance;
                }
                else
                {
                    if (distance < previousBestDistance)
                    {
                        previousBestDistance = distance;
                        matchingChildB = childB;
                    }
                }
            }

            if (matchingChildB == null)
            {
                // Nothing matched, so child A was removed
                // TODO: is it more efficient to take the cost here rather than at the end of the algorithm due to the
                // leftover items in the list?
                // cost += Constants.METABALL_REMOVE_COST;
                // unusedOldMetaballs.Remove(childA);
                continue;
            }

            // Consume childB
            childListB.Remove(matchingChildB);

            // If their relative positions differ, then the child was moved
            if (childARelativePosition.DistanceSquaredTo(matchingChildB.Position - basePositionB) > 0.01f)
            {
                cost += Constants.METABALL_MOVE_COST;
            }

            // Do the recursive comparison
            cost += RecursiveCompareChanges(childA, matchingChildB);
        }

        return cost;
    }

    private IReadonlyMacroscopicMetaball GetRoot(List<IReadonlyMacroscopicMetaball> metaballs)
    {
        foreach (var metaball in metaballs)
        {
            if (metaball.Parent == null)
                return metaball;
        }

        throw new ArgumentException("Metaballs has no root");
    }

    private List<IReadonlyMacroscopicMetaball>? GetChildren(IReadonlyMacroscopicMetaball parent,
        List<IReadonlyMacroscopicMetaball> potentialChildren)
    {
        List<IReadonlyMacroscopicMetaball>? children = null;

        // TODO: optimize this whole traversal. Do we need to store also child lists for each metaball?
        foreach (var metaball in potentialChildren)
        {
            if (ReferenceEquals(metaball.Parent, parent))
            {
                children ??= new List<IReadonlyMacroscopicMetaball>();
                children.Add(metaball);
            }
        }

        return children;
    }
}
