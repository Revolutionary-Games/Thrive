using System;
using System.Collections.Generic;
using System.Linq;
using Godot;

public class MulticellularSpeciesComparer
{
    private readonly MicrobeSpeciesComparer cellTypeComparer = new();

    private readonly List<IReadOnlyCellTypeDefinition> originalCellTypes = new();
    private readonly List<IReadOnlyCellTypeDefinition> newCellTypes = new();

    private readonly List<IReadOnlyCellTemplate> newCells = new();
    private readonly List<IReadOnlyCellTemplate> oldCells = new();

    public static double CompareCellTypes(List<IReadOnlyCellTypeDefinition> originalCellTypes,
        List<IReadOnlyCellTypeDefinition> newCellTypes, MicrobeSpeciesComparer typeComparer)
    {
        double cost = 0;

        foreach (var newCellType in newCellTypes)
        {
            IReadOnlyCellDefinition? original = null;

            // Match based on name to the old types. This should be fine if a player recreates a type with a new name
            // that could be a bit problematic, but the only way around that would be to check each type against each
            // other type and find the minimal cost, which could require a ton of operations.
            foreach (var originalType in originalCellTypes)
            {
                if (originalType.CellTypeName == newCellType.CellTypeName)
                {
                    original = originalType;
                    break;
                }
            }

            if (original == null)
            {
                // Couldn't match immediately so need to do some other searches
                foreach (var originalType in originalCellTypes)
                {
                    if (StringComparer.InvariantCultureIgnoreCase.Equals(originalType.CellTypeName,
                            newCellType.CellTypeName))
                    {
                        original = originalType;
                        break;
                    }
                }
            }

            // If still null, grab the first old type as the player is likely to duplicate from the stem type
            // If we need more control cell types would need to store the name of the type they are duplicated from
            original ??= originalCellTypes.FirstOrDefault();

            if (original == null)
            {
                GD.PrintErr("Using safety fallback for matching cell type change count");
                var temp = newCellType;
                original = newCellTypes.FirstOrDefault(c => c != temp) ?? newCellType;
            }

            cost += typeComparer.CompareCellType(original, newCellType, true);
        }

        // We don't need to process removed cell types as they are free to remove

        return cost;
    }

    public double Compare(IReadOnlyMulticellularSpecies speciesA, IReadOnlyMulticellularSpecies speciesB)
    {
        // Base cost
        double cost = SpeciesComparer.GetRequiredMutationPoints(speciesA, speciesB);

        originalCellTypes.AddRange(speciesA.CellTypes);
        newCellTypes.AddRange(speciesB.CellTypes);

        // Cost from each cell type change
        cost += CompareCellTypes(originalCellTypes, newCellTypes, cellTypeComparer) *
            Constants.MULTICELLULAR_EDITOR_COST_FACTOR;
        originalCellTypes.Clear();
        newCellTypes.Clear();

        // Then body plan change costs
        newCells.AddRange(speciesB.Cells);
        oldCells.AddRange(speciesA.Cells);

        // TODO: should this go in reverse order for more efficient removes?
        foreach (var newCell in newCells)
        {
            bool match = false;

            foreach (var oldCell in oldCells)
            {
                // TODO: should we add leniency here on the name?
                if (oldCell.Position == newCell.Position &&
                    StringComparer.InvariantCultureIgnoreCase.Equals(oldCell.CellType.CellTypeName,
                        newCell.CellType.CellTypeName))
                {
                    if (!oldCells.Remove(oldCell))
                        throw new Exception("Expected remove failed");

                    match = true;
                    break;
                }
            }

            if (!match)
            {
                // Turn into a move, as those are cheaper than placing
                // Unlike in microbe, it should be safe to "steal" a cell from a placed location as anyway we would
                // end up with a single move and add operation in the end anyway
                foreach (var oldCell in oldCells)
                {
                    if (StringComparer.InvariantCultureIgnoreCase.Equals(oldCell.CellType.CellTypeName,
                            newCell.CellType.CellTypeName))
                    {
                        if (!oldCells.Remove(oldCell))
                            throw new Exception("Expected remove failed");

                        match = true;

                        // A move
                        cost += Constants.ORGANELLE_MOVE_COST;
                        break;
                    }
                }
            }

            if (!match)
            {
                // Added a new cell
                cost += newCell.CellType.MPCost;
            }
        }

        // Finally, calculate removal costs (as we removed stuff already, everything left was removed)
        /*foreach (var old in oldCells)
        {
            cost += Constants.ORGANELLE_REMOVE_COST;
        }*/

        cost += oldCells.Count * Constants.METABALL_REMOVE_COST;

        oldCells.Clear();
        newCells.Clear();

        return cost;
    }
}
