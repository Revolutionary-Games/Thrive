namespace AutoEvo;

using System;
using System.Collections.Generic;
using static CommonMutationFunctions;

/// <summary>
///   Adds new adjacent cells in the given direction for each cellType (for testing mutation principle)
/// </summary>
public class ProliferateCells : IMutationStrategy<Species>
{
    private readonly AdjacencyDirection direction;

    public ProliferateCells(AdjacencyDirection direction)
    {
        this.direction = direction;
    }

    public bool Repeatable => false;

    public List<Mutant>? MutationsOf(Species baseSpecies, double mp, bool lawk,
        Random random, BiomeConditions biomeToConsider)
    {
        if (baseSpecies is not MulticellularSpecies baseMulticellularSpecies)
            return null;

        var mutated = new List<Mutant>();

        var workMemory1 = new List<Hex>();
        var workMemory2 = new List<Hex>();

        var baseCellTypes = baseMulticellularSpecies.ModifiableCellTypes;
        int baseCellTypesCount = baseCellTypes.Count;

        var baseCells = baseMulticellularSpecies.ModifiableEditorCells;
        int baseCellsCount = baseCells.Count;

        for (int i = 0; i < baseCellTypesCount; ++i)
        {
            var baseCellType = baseCellTypes[i];
            var mpCost = baseCellType.MPCost;
            if (mpCost > mp)
                continue;

            var newSpecies = (MulticellularSpecies)baseMulticellularSpecies.Clone();

            var newCellType = newSpecies.ModifiableCellTypes[i];

            if (AddCellsAdjacent(newSpecies, ref mp, baseCells, baseCellsCount, baseCellType, newCellType,
                    newCellType.MPCost, direction, workMemory1, workMemory2))
            {
                mutated.Add(new Mutant(newSpecies, mp));
            }
        }

        return mutated;
    }
}
