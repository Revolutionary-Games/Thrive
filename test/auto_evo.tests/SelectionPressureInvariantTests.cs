using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using AutoEvo;
using GdUnit4;
using static GdUnit4.Assertions;

/// <summary>
///   A cross-cutting safety net for every selection pressure type used by auto-evo. Each pressure must return a
///   finite, non-negative score for a valid species and must not depend on hidden ordering or cache state.
/// </summary>
/// <remarks>
///   <para>
///     This is the first line of the "every pressure has invariants" rule: hidden division by zero, NaN propagation or
///     an uninitialised cache ingredient shows up here instead of in a long auto-evo run.
///   </para>
/// </remarks>
[TestSuite]
[RequireGodotRuntime]
public class SelectionPressureInvariantTests
{
    private const float EPSILON = 0.0001f;

    /// <summary>
    ///   Every cached pressure must score a plain microbe with a finite, non-negative, reproducible value.
    /// </summary>
    [TestCase]
    public void EveryCachedPressureScoresMicrobesFiniteAndStable()
    {
        var species = SimulationCacheTestFixtures.CreateMicrobe(3001, "pressure", "single", "cytoplasm",
            "flagellum");

        AssertPressureInvariants(species, "microbe");
    }

    /// <summary>
    ///   The same guarantees must hold for multicellular species, which go through the structural miche branch.
    /// </summary>
    [TestCase]
    public void EveryCachedPressureScoresMulticellularFiniteAndStable()
    {
        var cellType = SimulationCacheTestFixtures.CreateCellType("InvariantCell", "single",
            SimulationCacheTestFixtures.CreateOrganelle("cytoplasm", new Hex(0, 0)),
            SimulationCacheTestFixtures.CreateOrganelle("flagellum", new Hex(1, 0)));

        var species = SimulationCacheTestFixtures.CreateMulticellular(3002, "pressure",
            (cellType, new Hex(0, 0)), (cellType, new Hex(2, 0)));

        AssertPressureInvariants(species, "multicellular");
    }

    private static void AssertPressureInvariants(Species species, string speciesKind)
    {
        var globalCache = new AutoEvoGlobalCache(new WorldGenerationSettings { Seed = 7 });
        var pressures = CollectPressures(globalCache);
        var patch = CreatePatch();
        var failures = new List<string>();

        AssertThat(pressures.Count).IsGreater(10);

        foreach (var pressure in pressures)
        {
            var pressureName = $"{pressure.GetType().Name} ({speciesKind})";

            try
            {
                var cache = SimulationCacheTestFixtures.CreateCache();
                var score = pressure.Score(species, patch, cache);

                if (!float.IsFinite(score))
                    failures.Add($"{pressureName}: score is not finite ({score})");

                if (score < 0)
                    failures.Add($"{pressureName}: score is negative ({score})");

                // Same inputs on the same cache must give the same answer
                var repeated = pressure.Score(species, patch, cache);
                if (BitConverter.SingleToInt32Bits(repeated) != BitConverter.SingleToInt32Bits(score))
                    failures.Add($"{pressureName}: score changed on repeat {score} -> {repeated}");

                // And a cold cache must reproduce the same value
                var coldCache = SimulationCacheTestFixtures.CreateCache();
                var cold = pressure.Score(species, patch, coldCache);
                if (Math.Abs(cold - score) > EPSILON)
                    failures.Add($"{pressureName}: cold cache score differs {score} -> {cold}");

                var energy = pressure.GetEnergy(patch);
                if (!float.IsFinite(energy))
                    failures.Add($"{pressureName}: energy is not finite ({energy})");

                if (energy < 0)
                    failures.Add($"{pressureName}: energy is negative ({energy})");

                // The relative comparison used to decide mutations must never produce a non-finite value
                var compared = pressure.WeightedComparedScores(score, score);
                if (!float.IsFinite(compared))
                    failures.Add($"{pressureName}: weighted comparison is not finite ({compared})");

                if (!float.IsFinite(pressure.WeightedComparedScores(score, score + 1)))
                    failures.Add($"{pressureName}: weighted comparison is not finite for an improved score");
            }
            catch (Exception exception)
            {
                failures.Add($"{pressureName}: threw {exception.GetType().Name}: {exception.Message}");
            }
        }

        AssertThat(failures.Count == 0)
            .OverrideFailureMessage($"Pressure invariant failures:\n{string.Join("\n", failures)}")
            .IsTrue();
    }

    private static List<SelectionPressure> CollectPressures(AutoEvoGlobalCache globalCache)
    {
        var result = new List<SelectionPressure>();

        foreach (var field in typeof(AutoEvoGlobalCache).GetFields(BindingFlags.Public | BindingFlags.Instance))
        {
            if (field.GetValue(globalCache) is SelectionPressure pressure && !result.Contains(pressure))
                result.Add(pressure);
        }

        // Sorting keeps the failure output stable between runs
        return result.OrderBy(pressure => pressure.GetType().Name, StringComparer.Ordinal)
            .ThenBy(pressure => pressure.Weight).ToList();
    }

    private static Patch CreatePatch()
    {
        var template = SimulationParameters.Instance.GetBiome("aavolcanic_vent");
        var biome = (BiomeConditions)template.Conditions.Clone();

        return new Patch(new LocalizedString("InvariantPatch"), 1, template, BiomeType.Vents,
            new PatchSnapshot(biome, template.Background), 1);
    }
}
