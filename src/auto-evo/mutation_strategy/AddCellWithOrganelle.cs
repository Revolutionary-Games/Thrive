namespace AutoEvo;

using System;
using System.Collections.Generic;
using System.Linq;
using static CommonMutationFunctions;

/// <summary>
///   Adds a random, valid organelle to a valid position. Doesn't place multicellular or later organelles.
/// </summary>
public class AddCellWithOrganelle : IMutationStrategy<Species>
{
    private readonly Direction direction;
    private readonly OrganelleDefinition[] allOrganelles;

    public AddCellWithOrganelle(Func<OrganelleDefinition, bool> criteria, Direction direction = Direction.Neutral)
    {
        allOrganelles = SimulationParameters.Instance.GetAllOrganelles().Where(criteria).Where(IsOrganelleValid)
            .ToArray();

        this.direction = direction;
    }

    public bool Repeatable => true;

    // Formatter and inspect code disagree here
    // ReSharper disable InvokeAsExtensionMethod
    public static AddCellWithOrganelle ThatUseCompound(CompoundDefinition compound,
        Direction direction = Direction.Neutral)
    {
        return new AddCellWithOrganelle(
            organelle => Enumerable.Any(organelle.RunnableProcesses, proc => proc.Process.Inputs.ContainsKey(compound)),
            direction);
    }

    public static AddCellWithOrganelle ThatUseCompound(Compound compound, Direction direction = Direction.Neutral)
    {
        var compoundResolved = SimulationParameters.GetCompound(compound);

        return ThatUseCompound(compoundResolved, direction);
    }

    public static AddCellWithOrganelle ThatCreateCompound(CompoundDefinition compound,
        Direction direction = Direction.Neutral)
    {
        return new AddCellWithOrganelle(organelle =>
                Enumerable.Any(organelle.RunnableProcesses, proc => proc.Process.Outputs.ContainsKey(compound)),
            direction);
    }

    public static AddCellWithOrganelle ThatCreateCompound(Compound compound,
        Direction direction = Direction.Neutral)
    {
        var compoundResolved = SimulationParameters.GetCompound(compound);

        return ThatCreateCompound(compoundResolved, direction);
    }

    public static AddCellWithOrganelle ThatConvertBetweenCompounds(CompoundDefinition fromCompound,
        CompoundDefinition toCompound, Direction direction = Direction.Neutral)
    {
        return new AddCellWithOrganelle(organelle => Enumerable.Any(organelle.RunnableProcesses, proc =>
            proc.Process.Inputs.ContainsKey(fromCompound) &&
            proc.Process.Outputs.ContainsKey(toCompound)), direction);
    }

    // ReSharper restore InvokeAsExtensionMethod

    public static AddCellWithOrganelle ThatConvertBetweenCompounds(Compound fromCompound, Compound toCompound,
        Direction direction = Direction.Neutral)
    {
        var fromCompoundResolved = SimulationParameters.GetCompound(fromCompound);
        var toCompoundResolved = SimulationParameters.GetCompound(toCompound);

        return ThatConvertBetweenCompounds(fromCompoundResolved, toCompoundResolved, direction);
    }

    public List<Mutant>? MutationsOf(Species baseSpecies, double mp, bool lawk, Random random,
        BiomeConditions biomeToConsider)
    {
        if (baseSpecies is not MulticellularSpecies baseMulticellularSpecies)
            return null;

        if (mp < Constants.ORGANELLE_CHEAPEST_COST * Constants.MULTICELLULAR_EDITOR_COST_FACTOR)
            return null;

        var organelles = allOrganelles.OrderBy(_ => random.Next())
            .Take(Constants.AUTO_EVO_ORGANELLE_ADD_ATTEMPTS);

        var mutated = new List<Mutant>();

        var workMemory1 = new List<Hex>();
        var workMemory2 = new List<Hex>();
        var workMemory3 = new HashSet<Hex>();

        foreach (var organelle in organelles)
        {
            // Important to not accidentally add non-LAWK organelles in a LAWK game
            if (!organelle.LAWK && lawk)
                continue;

            var baseCellTypes = baseMulticellularSpecies.CellTypes;
            var baseCellTypesCount = baseCellTypes.Count;

            var highestTargetOrganelleCount = 0;
            var mostSuitableIndex = 0;

            // Determine which Cell Type carries the most of this organelle
            for (int i = 0; i < baseCellTypesCount; ++i)
            {
                var baseCellType = baseCellTypes[i];
                var targetOrganelleCount = 0;

                foreach (var placedOrganelle in baseCellType.Organelles)
                {
                    if (placedOrganelle.Definition == organelle)
                        ++targetOrganelleCount;
                }

                if (targetOrganelleCount > highestTargetOrganelleCount)
                {
                    highestTargetOrganelleCount = targetOrganelleCount;
                    mostSuitableIndex = i;
                }
            }

            var baseCells = baseMulticellularSpecies.ModifiableEditorCells;
            int baseCellsCount = baseCells.Count;

            // If there is already a Cell Type with this organelle, add more of this type adjacent to existing,
            // trying multiple configurations.
            // Otherwise, create a new Cell Type with this Organelle, and place a new cell on the center line.
            if (highestTargetOrganelleCount > 0)
            {
                var newSpecies = (MulticellularSpecies)baseMulticellularSpecies.Clone();
                var baseCellType = baseCellTypes[mostSuitableIndex];
                var newCellType = newSpecies.ModifiableCellTypes[mostSuitableIndex];

                if (AddCellsAdjacent(newSpecies, ref mp, baseCells, baseCellsCount, baseCellType, newCellType,
                        newCellType.MPCost, AdjacencyDirection.Front, workMemory1, workMemory2))
                {
                    mutated.Add(new Mutant(newSpecies, mp));
                }

                newSpecies = (MulticellularSpecies)baseMulticellularSpecies.Clone();
                newCellType = newSpecies.ModifiableCellTypes[mostSuitableIndex];
                if (AddCellsAdjacent(newSpecies, ref mp, baseCells, baseCellsCount, baseCellType, newCellType,
                        newCellType.MPCost, AdjacencyDirection.SideFront, workMemory1, workMemory2))
                {
                    mutated.Add(new Mutant(newSpecies, mp));
                }

                newSpecies = (MulticellularSpecies)baseMulticellularSpecies.Clone();
                newCellType = newSpecies.ModifiableCellTypes[mostSuitableIndex];
                if (AddCellsAdjacent(newSpecies, ref mp, baseCells, baseCellsCount, baseCellType, newCellType,
                        newCellType.MPCost, AdjacencyDirection.SideRear, workMemory1, workMemory2))
                {
                    mutated.Add(new Mutant(newSpecies, mp));
                }

                newSpecies = (MulticellularSpecies)baseMulticellularSpecies.Clone();
                newCellType = newSpecies.ModifiableCellTypes[mostSuitableIndex];
                if (AddCellsAdjacent(newSpecies, ref mp, baseCells, baseCellsCount, baseCellType, newCellType,
                        newCellType.MPCost, AdjacencyDirection.Rear, workMemory1, workMemory2))
                {
                    mutated.Add(new Mutant(newSpecies, mp));
                }
            }
            else
            {
                var smallestCellSize = baseMulticellularSpecies.CellTypes[0].BaseHexSize;
                var smallestCellIndex = 0;
                var newSpecies = (MulticellularSpecies)baseMulticellularSpecies.Clone();

                for (int i = 0; i < baseCellTypesCount; ++i)
                {
                    if (baseCellTypes[i].BaseHexSize >= smallestCellSize)
                        continue;

                    smallestCellIndex = i;
                    smallestCellSize = baseCellTypes[i].BaseHexSize;
                }

                var templateCellType = baseMulticellularSpecies.ModifiableCellTypes[smallestCellIndex];

                if (organelle.IsIncompatibleWithMembrane(templateCellType.MembraneType))
                    continue;

                // Don't add duplicate unique organelles
                if (organelle.Unique && templateCellType.Organelles.Select(x => x.Definition).Contains(organelle))
                    continue;

                var newCellType = (CellType)templateCellType.Clone();

                // In the rare case that adding the organelle fails, this can skip adding it to be tested as the species
                // is not any different
                if (!AddOrganelle(organelle, direction, newCellType, workMemory1, workMemory2, workMemory3, random))
                    continue;

                var cellTypes = newSpecies.ModifiableCellTypes;
                newCellType.CellTypeName = organelle.Name;

                // This part is used for MP calculations in the player editor, so might not be required?
                newCellType.SplitFromTypeName = templateCellType.CellTypeName;

                switch (direction)
                {
                    case Direction.Front:
                        cellTypes.Add(newCellType);
                        if (AddCellCenterline(Direction.Front, newCellType, newSpecies,
                                newSpecies.ModifiableEditorCells, workMemory1, workMemory2, random))
                        {
                            mutated.Add(new Mutant(newSpecies, mp
                                - Constants.MULTICELLULAR_EDITOR_COST_FACTOR * organelle.MPCost - newCellType.MPCost));
                        }

                        break;
                    case Direction.Rear:
                        cellTypes.Add(newCellType);
                        if (AddCellCenterline(Direction.Rear, newCellType, newSpecies,
                                newSpecies.ModifiableEditorCells, workMemory1, workMemory2, random))
                        {
                            mutated.Add(new Mutant(newSpecies, mp
                                - Constants.MULTICELLULAR_EDITOR_COST_FACTOR * organelle.MPCost - newCellType.MPCost));
                        }

                        break;
                    case Direction.Neutral:
                        var newSpeciesFront = (MulticellularSpecies)newSpecies.Clone();
                        newSpeciesFront.ModifiableCellTypes.Add(newCellType);
                        if (AddCellCenterline(Direction.Front, newCellType, newSpeciesFront,
                                newSpeciesFront.ModifiableEditorCells, workMemory1, workMemory2, random))
                        {
                            mutated.Add(new Mutant(newSpeciesFront, mp
                                - Constants.MULTICELLULAR_EDITOR_COST_FACTOR * organelle.MPCost - newCellType.MPCost));
                        }

                        cellTypes.Add(newCellType);
                        if (AddCellCenterline(Direction.Rear, newCellType, newSpecies,
                                newSpecies.ModifiableEditorCells, workMemory1, workMemory2, random))
                        {
                            mutated.Add(new Mutant(newSpecies, mp
                                - Constants.MULTICELLULAR_EDITOR_COST_FACTOR * organelle.MPCost - newCellType.MPCost));
                        }

                        break;
                    default:
                        throw new ArgumentOutOfRangeException(nameof(direction), direction, null);
                }
            }
        }

        return mutated;
    }

    /// <summary>
    ///   Macroscopic, and non-placeable organelles are invalid and so won't be considered.
    /// </summary>
    private static bool IsOrganelleValid(OrganelleDefinition organelle)
    {
        return organelle.AutoEvoCanPlace &&
            organelle.EditorButtonGroup != OrganelleDefinition.OrganelleGroup.Macroscopic;
    }
}
