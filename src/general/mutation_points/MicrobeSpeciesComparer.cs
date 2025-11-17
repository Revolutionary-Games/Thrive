using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Godot;

/// <summary>
///   Compares microbe species to each other to determine total necessary mutation points. This is not static as this
///   needs so much temporary memory.
/// </summary>
public class MicrobeSpeciesComparer
{
    private readonly OrganelleDefinition cytoplasm;

    private readonly IReadOnlyList<string> emptyList = new List<string>();

    private readonly List<Hex> workMemory = new();

    private readonly List<IReadOnlyOrganelleTemplate> usedNewOrganelles = new();
    private readonly List<IReadOnlyOrganelleTemplate> unresolvedMoves = new();
    private readonly List<IReadOnlyOrganelleTemplate> unusedOldOrganelles = new();

    public MicrobeSpeciesComparer(OrganelleDefinition? cytoplasm = null)
    {
        this.cytoplasm = cytoplasm ?? SimulationParameters.Instance.GetOrganelleType("cytoplasm");
    }

    public static double CalculateRigidityCost(float newRigidity, float previousRigidity)
    {
        return Math.Abs(newRigidity - previousRigidity) * Constants.MEMBRANE_RIGIDITY_SLIDER_TO_VALUE_RATIO *
            Constants.MEMBRANE_RIGIDITY_COST_PER_STEP;
    }

    public static double CalculateUpgradeCost(Dictionary<string, AvailableUpgrade> availableUpgrades,
        IReadOnlyList<string> newUpgrades, IReadOnlyList<string> oldUpgrades, bool refund = false)
    {
        int cost = 0;

        // TODO: allow custom upgrades to have a cost (should also add a test in EditorMPTests)
        // TODO: also each upgrade will need to have handling code added, see all implementations of
        // IComponentSpecificUpgrades.CalculateCost

        // Calculate the costs of the selected new general upgrades

        int count = newUpgrades.Count;
        for (int i = 0; i < count; ++i)
        {
            var newUpgrade = newUpgrades[i];

            if (oldUpgrades.Contains(newUpgrade))
                continue;

            if (!availableUpgrades.TryGetValue(newUpgrade, out var upgrade))
            {
                // TODO: this probably should be suppressed in cases where we have dynamically called upgrade names,
                // which we might do in the future
                GD.PrintErr("Cannot calculate cost for an unknown upgrade: ", newUpgrade);
            }
            else
            {
                cost += upgrade.MPCost;
            }
        }

        if (refund)
        {
            // Refund removed upgrades
            count = oldUpgrades.Count;
            for (int i = 0; i < count; ++i)
            {
                var oldUpgrade = oldUpgrades[i];

                if (newUpgrades.Contains(oldUpgrade))
                    continue;

                if (!availableUpgrades.TryGetValue(oldUpgrade, out var upgrade))
                {
                    // See the TODO above
                    GD.PrintErr("Cannot calculate cost for an unknown upgrade: ", oldUpgrade);
                }
                else
                {
                    cost -= upgrade.MPCost;
                }
            }
        }

        // else
        {
            // TODO: Removals should cost MP: https://github.com/Revolutionary-Games/Thrive/issues/4095
            // var removedUpgrades = OldUpgrades.UnlockedFeatures.Except(NewUpgrades.UnlockedFeatures)
            //     .Where(u => availableUpgrades.ContainsKey(u)).Select(u => availableUpgrades[u]);
            // ? removedUpgrades.Sum(u => u.MPCost);
        }

        return cost;
    }

    public double Compare(IReadOnlyMicrobeSpecies speciesA, IReadOnlyMicrobeSpecies speciesB)
    {
        // Base cost
        double cost = SpeciesComparer.GetRequiredMutationPoints(speciesA, speciesB);

        // Microbe species specific costs
        // Colour comparison would be a double cost here, so avoid it
        cost += CompareCellType(speciesA, speciesB, false);

        return cost;
    }

    public double CompareCellType(IReadOnlyCellDefinition cellTypeA, IReadOnlyCellDefinition cellTypeB, bool colour)
    {
        if (colour)
        {
            // TODO: calculate colour cost once that costs something
        }

        double cost = 0;

        // First, check the changed easy properties
        if (cellTypeA.MembraneType != cellTypeB.MembraneType)
        {
            cost += cellTypeB.MembraneType.EditorCost;
        }

        cost += CalculateRigidityCost(cellTypeB.MembraneRigidity, cellTypeA.MembraneRigidity);

        // And then figure out all organelle differences

        // As the data can come from facades, we want to avoid index lookups, as such we need to allocate enumerators,
        // but that's the lesser evil here
        // List<Hex> workMemory1 = new();

        unusedOldOrganelles.AddRange(cellTypeB.Organelles);

        foreach (var originalOrganelle in cellTypeA.Organelles)
        {
            // First match existing organelles that are still there
            var newOrganelle = cellTypeB.Organelles.GetByExactElementRootPosition(originalOrganelle.Position);

            if (newOrganelle != null && originalOrganelle.Definition == newOrganelle.Definition &&
                originalOrganelle.Orientation == newOrganelle.Orientation)
            {
                // Found a match. This organelle is still here in the exact same position.
                // Add any cost from upgrades
                cost += AddUpgradeCost(originalOrganelle, newOrganelle);

                usedNewOrganelles.Add(newOrganelle);
                unusedOldOrganelles.Remove(newOrganelle);
                continue;
            }

            // Organelle is not where it was before

            // Check for in-place rotations to resolve the difference
            newOrganelle = cellTypeB.Organelles.GetElementAt(originalOrganelle.Position, workMemory);

            if (newOrganelle != null && originalOrganelle.Definition == newOrganelle.Definition)
            {
                // There is a potential candidate at this position, but check that the exact hexes after applying
                // the rotations match
                if (RotatedInPlace(originalOrganelle, newOrganelle, workMemory))
                {
                    // Rotated in-place, which is free, but upgrade changes will cost

                    cost += AddUpgradeCost(originalOrganelle, newOrganelle);

                    usedNewOrganelles.Add(newOrganelle);
                    unusedOldOrganelles.Remove(newOrganelle);
                    continue;
                }
            }

            // Can't resolve simply, store for later
            unresolvedMoves.Add(originalOrganelle);
        }

        // Once everything is processed, try to match organelles of the same type into move operations
        // This goes in reverse order for more efficient removes
        for (var i = unresolvedMoves.Count - 1; i >= 0; --i)
        {
            var originalOrganelle = unresolvedMoves[i];

            // Match to the cheapest upgrade
            IReadOnlyOrganelleTemplate? cheapestUpgrade = null;
            double currentCheapestPrice = double.MaxValue;

            int count = unusedOldOrganelles.Count;
            for (int j = 0; j < count; ++j)
            {
                var newOrganelle = unusedOldOrganelles[j];

                if (originalOrganelle.Definition == newOrganelle.Definition)
                {
                    var upgradePrice = AddUpgradeCost(originalOrganelle, newOrganelle);
                    if (upgradePrice < currentCheapestPrice)
                    {
                        cheapestUpgrade = newOrganelle;
                        currentCheapestPrice = upgradePrice;
                    }
                }
            }

            // TODO: does this need to check if the move is actually cheaper than a new placement?
            if (cheapestUpgrade != null && currentCheapestPrice + Constants.ORGANELLE_MOVE_COST <
                originalOrganelle.Definition.MPCost + CalculateUpgradeCost(cheapestUpgrade.Definition.AvailableUpgrades,
                    cheapestUpgrade.Upgrades?.UnlockedFeatures ?? emptyList,
                    emptyList))
            {
                // Found a move that leads to a cheaper upgrade (than placing from scratch)
                usedNewOrganelles.Add(cheapestUpgrade);
                unusedOldOrganelles.Remove(cheapestUpgrade);
                unresolvedMoves.RemoveAt(i);

                cost += currentCheapestPrice;
                cost += Constants.ORGANELLE_MOVE_COST;
                continue;
            }

            // Couldn't make it into a move after all, so add the remove cost

            // Except it is free to replace a cytoplasm by placing something on top, so those need to be resolved later
            if (originalOrganelle.Definition != cytoplasm)
            {
                cost += Constants.ORGANELLE_REMOVE_COST;
                unresolvedMoves.RemoveAt(i);
            }
        }

        unusedOldOrganelles.Clear();

        // Find added organelles that weren't used already
        foreach (var newOrganelle in cellTypeB.Organelles)
        {
            // If already processed, skip. As positions are checked from old to new, we know for sure that if something
            // is not in the processed list, it is at a position that doesn't match any old organelle
            if (usedNewOrganelles.Contains(newOrganelle))
                continue;

            // This is a new organelle so add the cost
            cost += newOrganelle.Definition.MPCost;

            // If placed and upgraded at once, add that cost as well
            cost += CalculateUpgradeCost(newOrganelle.Definition.AvailableUpgrades,
                newOrganelle.Upgrades?.UnlockedFeatures ?? emptyList,
                emptyList);

            if (newOrganelle.Definition != cytoplasm && unresolvedMoves.Count > 0)
            {
                // Remove freely removed cytoplasm to not count their costs in the later loop
                var hexes = newOrganelle.Definition.GetRotatedHexes(newOrganelle.Orientation);
                int hexesCount = hexes.Count;

                for (int i = unresolvedMoves.Count - 1; i >= 0; --i)
                {
                    var unresolvedMove = unresolvedMoves[i];

                    for (int j = 0; j < hexesCount; ++j)
                    {
                        // Don't need to check rotation as cytoplasm is just a single hex
                        if (unresolvedMove.Position == hexes[j] + newOrganelle.Position)
                        {
                            unresolvedMoves.RemoveAt(i);
                            break;
                        }
                    }
                }
            }
        }

        // Add purely removed cytoplasm cost
        foreach (var originalOrganelle in unresolvedMoves)
        {
            if (originalOrganelle.Definition == cytoplasm)
            {
                cost += Constants.ORGANELLE_REMOVE_COST;
            }
            else
            {
                throw new Exception("There shouldn't be non-cytoplasm organelles left in unresolved moves");
            }
        }

        unresolvedMoves.Clear();
        usedNewOrganelles.Clear();
        return cost;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private double AddUpgradeCost(IReadOnlyOrganelleTemplate originalOrganelle, IReadOnlyOrganelleTemplate newOrganelle)
    {
        return CalculateUpgradeCost(originalOrganelle.Definition.AvailableUpgrades,
            newOrganelle.Upgrades?.UnlockedFeatures ?? emptyList,
            originalOrganelle.Upgrades?.UnlockedFeatures ?? emptyList);
    }

    private bool RotatedInPlace(IReadOnlyOrganelleTemplate originalOrganelle, IReadOnlyOrganelleTemplate newOrganelle,
        List<Hex> workMemory1)
    {
        workMemory1.Clear();

        var pos = originalOrganelle.Position;

        // Add first original hexes
        var rotated = originalOrganelle.Definition.GetRotatedHexes(originalOrganelle.Orientation);
        int count = rotated.Count;
        for (int i = 0; i < count; ++i)
        {
            workMemory1.Add(rotated[i] + pos);
        }

        // And then check that new hexes match exactly
        rotated = newOrganelle.Definition.GetRotatedHexes(newOrganelle.Orientation);
        count = rotated.Count;
        for (int i = 0; i < count; ++i)
        {
            var hex = rotated[i] + pos;

            if (!workMemory1.Contains(hex))
                return false;
        }

        return true;
    }
}
