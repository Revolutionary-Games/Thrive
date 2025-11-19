using Godot;

public class MulticellularSpeciesComparer
{
    // private readonly MicrobeSpeciesComparer cellTypeComparer = new();

    public double Compare(IReadOnlyMulticellularSpecies speciesA, IReadOnlyMulticellularSpecies speciesB)
    {
        // Base cost
        double cost = SpeciesComparer.GetRequiredMutationPoints(speciesA, speciesB);

        GD.PrintErr("TODO: REIMPLEMENT MACROSCOPIC MP");

        return cost;

        /*
        // Cost from each cell type change
        foreach (var cellTypeA in speciesA.CellTypes)
        {
            // TODO: match cell types more intelligently
            foreach (var cellTypeB in speciesB.CellTypes)
            {
                cost += cellTypeComparer.CompareCellType(cellTypeA, cellTypeB, false);
            }
        }

        // TODO: cost for added new cell types

        // Removing cell types doesn't have a cost

        // TODO: body plan change cost

        return cost;*/
    }
}
