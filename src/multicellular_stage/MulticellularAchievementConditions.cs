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
    ///   Checks that no open indentation inside the body plan's convex hull is at least two hexes deep.
    /// </summary>
    public static bool HasNoDeepCrevices(CellLayout<CellTemplate> layout)
    {
        var occupied = layout.ComputeHexCache();
        if (occupied.Count == 0)
            return false;

        var convexHull = ComputeConvexHull(occupied);
        if (convexHull.Count < 3)
            return true;

        int minQ = int.MaxValue;
        int maxQ = int.MinValue;
        int minR = int.MaxValue;
        int maxR = int.MinValue;
        foreach (var hex in convexHull)
        {
            minQ = Math.Min(minQ, hex.Q);
            maxQ = Math.Max(maxQ, hex.Q);
            minR = Math.Min(minR, hex.R);
            maxR = Math.Max(maxR, hex.R);
        }

        var insideHull = new HashSet<Hex>();
        var emptyInsideHull = new HashSet<Hex>();
        for (int q = minQ; q <= maxQ; ++q)
        {
            for (int r = minR; r <= maxR; ++r)
            {
                var candidate = new Hex(q, r);
                if (!IsInsideConvexHull(candidate, convexHull))
                    continue;

                insideHull.Add(candidate);
                if (!occupied.Contains(candidate))
                    emptyInsideHull.Add(candidate);
            }
        }

        var pending = new Queue<(Hex Position, int Depth)>();
        var visited = new HashSet<Hex>();
        foreach (var emptyHex in emptyInsideHull)
        {
            foreach (var offset in Hex.HexNeighbourOffset.Values)
            {
                if (insideHull.Contains(emptyHex + offset))
                    continue;

                visited.Add(emptyHex);
                pending.Enqueue((emptyHex, 1));
                break;
            }
        }

        while (pending.TryDequeue(out var current))
        {
            if (current.Depth >= 2)
                return false;

            foreach (var offset in Hex.HexNeighbourOffset.Values)
            {
                var next = current.Position + offset;
                if (emptyInsideHull.Contains(next) && visited.Add(next))
                    pending.Enqueue((next, current.Depth + 1));
            }
        }

        return true;
    }

    private static List<Hex> ComputeConvexHull(HashSet<Hex> hexes)
    {
        var sortedHexes = new List<Hex>(hexes);
        sortedHexes.Sort(CompareHexes);

        if (sortedHexes.Count <= 2)
            return sortedHexes;

        var lowerHull = new List<Hex>();
        foreach (var hex in sortedHexes)
        {
            while (lowerHull.Count >= 2 &&
                   CrossProduct(lowerHull[^2], lowerHull[^1], hex) <= 0)
            {
                lowerHull.RemoveAt(lowerHull.Count - 1);
            }

            lowerHull.Add(hex);
        }

        var upperHull = new List<Hex>();
        for (int i = sortedHexes.Count - 1; i >= 0; --i)
        {
            var hex = sortedHexes[i];
            while (upperHull.Count >= 2 &&
                   CrossProduct(upperHull[^2], upperHull[^1], hex) <= 0)
            {
                upperHull.RemoveAt(upperHull.Count - 1);
            }

            upperHull.Add(hex);
        }

        lowerHull.RemoveAt(lowerHull.Count - 1);
        upperHull.RemoveAt(upperHull.Count - 1);
        lowerHull.AddRange(upperHull);
        return lowerHull;
    }

    private static int CompareHexes(Hex left, Hex right)
    {
        int qComparison = left.Q.CompareTo(right.Q);
        return qComparison != 0 ? qComparison : left.R.CompareTo(right.R);
    }

    private static long CrossProduct(Hex first, Hex second, Hex third)
    {
        return ((long)second.Q - first.Q) * ((long)third.R - second.R) -
            ((long)second.R - first.R) * ((long)third.Q - second.Q);
    }

    private static bool IsInsideConvexHull(Hex candidate, List<Hex> convexHull)
    {
        for (int i = 0; i < convexHull.Count; ++i)
        {
            var first = convexHull[i];
            var second = convexHull[(i + 1) % convexHull.Count];
            if (CrossProduct(first, second, candidate) < 0)
                return false;
        }

        return true;
    }
}
