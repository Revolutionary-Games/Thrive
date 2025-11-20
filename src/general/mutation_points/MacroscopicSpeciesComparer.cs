using System;
using Godot;

public class MacroscopicSpeciesComparer
{
    private readonly MicrobeSpeciesComparer cellTypeComparer = new();

    public double Compare(IReadOnlyMacroscopicSpecies speciesA, IReadOnlyMacroscopicSpecies speciesB)
    {
        // Base cost
        double cost = SpeciesComparer.GetRequiredMutationPoints(speciesA, speciesB);

        throw new NotImplementedException();

        // TODO: update based on multicellular species comparer

        // Cost from each cell type change
        foreach (var cellTypeA in speciesA.CellTypes)
        {
            // TODO: match cell types more intelligently
            foreach (var cellTypeB in speciesB.CellTypes)
            {
                cost += cellTypeComparer.CompareCellType(cellTypeA, cellTypeB, false);
            }
        }

        // For types:
        // Constants.MULTICELLULAR_EDITOR_COST_FACTOR

        // TODO: cost for added new cell types

        // Removing cell types doesn't have a cost

        // TODO: body plan change cost

        return cost;
    }
}
