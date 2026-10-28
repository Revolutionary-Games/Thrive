using System;
using System.Collections.Generic;
using Systems;

/// <summary>
///   Infrequent achievement checks on accepted multicellular body plans.
/// </summary>
public static class MulticellularAchievementConditions
{
    public static bool UsesOnlyGlucoseForEnergy(MulticellularSpecies species)
    {
        var glucoseDefinition = SimulationParameters.GetCompound(Compound.Glucose);
        var atpDefinition = SimulationParameters.GetCompound(Compound.ATP);
        var hasGlucoseEnergy = false;

        List<TweakedProcess>? processes = null;

        foreach (var cell in species.ModifiableGameplayCells)
        {
            ProcessSystem.ComputeActiveProcessList(cell.ModifiableOrganelles, ref processes);

            foreach (var process in processes)
            {
                if (process.Rate <= 0)
                    continue;

                if (process.Process.Outputs.TryGetValue(glucoseDefinition, out var glucose) && glucose > 0)
                    return false;

                if (!process.Process.Outputs.TryGetValue(atpDefinition, out var atp) || atp <= 0)
                    continue;

                // Only processes that consume glucose can produce ATP
                if (!process.Process.Inputs.TryGetValue(glucoseDefinition, out glucose) || glucose <= 0)
                    return false;

                hasGlucoseEnergy = true;
            }
        }

        return hasGlucoseEnergy;
    }

    public static void CheckEditorBodyPlan(MulticellularSpecies species)
    {
        var usedTypes = new HashSet<CellType>();
        foreach (var cell in species.ModifiableGameplayCells)
            usedTypes.Add(cell.ModifiableCellType);

        var specializations = new HashSet<OrganelleDefinition>();
        var counts = new Dictionary<OrganelleDefinition, int>();
        int armedTypes = 0;
        int lightlyArmedTypes = 0;
        foreach (var type in usedTypes)
        {
            int weapons = 0;
            foreach (var organelle in type.Organelles)
            {
                if (organelle.Definition.HasPilusComponent || organelle.Definition.HasAgentVacuoleComponent)
                    ++weapons;
            }

            if (weapons >= 5)
                ++armedTypes;
            if (weapons <= 1)
                ++lightlyArmedTypes;

            if (MicrobeInternalCalculations.CalculateSpecializationBonus(type.ModifiableOrganelles.Organelles,
                    counts) <= 1.5f)
            {
                continue;
            }

            OrganelleDefinition? mostCommon = null;
            int largestCount = 0;
            foreach (var entry in counts)
            {
                if (entry.Value > largestCount)
                {
                    mostCommon = entry.Key;
                    largestCount = entry.Value;
                }
                else if (entry.Value == largestCount)
                {
                    mostCommon = null;
                }
            }

            if (mostCommon != null)
                specializations.Add(mostCommon.IsSameInSpecializationAs ?? mostCommon);
        }

        if (armedTypes == 1 && lightlyArmedTypes == usedTypes.Count - 1 && lightlyArmedTypes > 0)
            AchievementEvents.ReportSpecializedWeapons();
        if (specializations.Count > 0)
            AchievementEvents.ReportSpecializationSpecialist(specializations.Count);

        if (species.ModifiableGameplayCells.Count >= 10 && HasNoEnclosedGaps(species.ModifiableGameplayCells) &&
            HasNoDeepCrevices(species.ModifiableGameplayCells))
        {
            AchievementEvents.ReportPerfectlyPacked();
        }
    }

    /// <summary>
    ///   Floods the empty exterior of the full organelle footprint; unreached empty hexes are enclosed gaps.
    /// </summary>
    public static bool HasNoEnclosedGaps(CellLayout<CellTemplate> layout)
    {
        var occupied = layout.ComputeHexCache();
        if (occupied.Count == 0)
            return false;

        int minQ = int.MaxValue;
        int maxQ = int.MinValue;
        int minR = int.MaxValue;
        int maxR = int.MinValue;
        foreach (var hex in occupied)
        {
            minQ = Math.Min(minQ, hex.Q - 1);
            maxQ = Math.Max(maxQ, hex.Q + 1);
            minR = Math.Min(minR, hex.R - 1);
            maxR = Math.Max(maxR, hex.R + 1);
        }

        var exterior = new HashSet<Hex>();
        var pending = new Queue<Hex>();
        var start = new Hex(minQ, minR);
        exterior.Add(start);
        pending.Enqueue(start);
        while (pending.TryDequeue(out var current))
        {
            foreach (var offset in Hex.HexNeighbourOffset.Values)
            {
                var next = current + offset;
                if (next.Q < minQ || next.Q > maxQ || next.R < minR || next.R > maxR ||
                    occupied.Contains(next) || !exterior.Add(next))
                {
                    continue;
                }

                pending.Enqueue(next);
            }
        }

        return occupied.Count + exterior.Count == (maxQ - minQ + 1) * (maxR - minR + 1);
    }

    /// <summary>
    ///   Rolls a hex with its six neighbours around the exterior of the body plan. Broad concavities and
    ///   protrusions and one extra hex of shallow notches are allowed. Empty hexes beyond this outline are
    ///   gaps or deep, narrow crevices.
    /// </summary>
    public static bool HasNoDeepCrevices(CellLayout<CellTemplate> layout)
    {
        var occupied = layout.ComputeHexCache();
        if (occupied.Count == 0)
            return false;

        // A probe centre must stay at least one empty hex away from the occupied footprint
        var blocked = new HashSet<Hex>(occupied);
        int minQ = int.MaxValue;
        int maxQ = int.MinValue;
        int minR = int.MaxValue;
        int maxR = int.MinValue;
        foreach (var hex in occupied)
        {
            foreach (var offset in Hex.HexNeighbourOffset.Values)
            {
                blocked.Add(hex + offset);
            }

            // Leave room for the probe centre to travel all the way around the colony
            minQ = Math.Min(minQ, hex.Q - 2);
            maxQ = Math.Max(maxQ, hex.Q + 2);
            minR = Math.Min(minR, hex.R - 2);
            maxR = Math.Max(maxR, hex.R + 2);
        }

        // After calculating the bounds, initialize the reachable set and run a search algorithm
        var visited = new HashSet<Hex>();
        var reachable = new HashSet<Hex>();
        var pending = new Queue<Hex>();

        var start = new Hex(minQ, minR);
        visited.Add(start);
        pending.Enqueue(start);
        while (pending.TryDequeue(out var current))
        {
            reachable.Add(current);
            foreach (var offset in Hex.HexNeighbourOffset.Values)
            {
                var next = current + offset;

                // Allow a shallow notch beyond the probe edge, following empty hexes so this cannot
                // reach through a thin wall into a deep crevice on its other side
                if (!occupied.Contains(next))
                {
                    reachable.Add(next);
                    foreach (var edgeOffset in Hex.HexNeighbourOffset.Values)
                    {
                        reachable.Add(next + edgeOffset);
                    }
                }

                if (next.Q < minQ || next.Q > maxQ || next.R < minR || next.R > maxR ||
                    blocked.Contains(next) || !visited.Add(next))
                {
                    continue;
                }

                pending.Enqueue(next);
            }
        }

        // Then finally check the results
        foreach (var hex in blocked)
        {
            if (!occupied.Contains(hex) && !reachable.Contains(hex))
                return false;
        }

        return true;
    }
}
