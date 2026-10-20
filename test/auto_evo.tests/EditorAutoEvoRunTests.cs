using System;
using System.Diagnostics;
using AutoEvo;
using GdUnit4;
using static GdUnit4.Assertions;

/// <summary>
///   Exercises editor predictions against the world map without applying the results to the world.
/// </summary>
[TestSuite]
[RequireGodotRuntime]
public class EditorAutoEvoRunTests
{
    /// <summary>
    ///   Verifies that a prediction populates an uninhabited target in the results while leaving its input
    ///   population unchanged. Population, energy, and miche results use the world map's patch references.
    /// </summary>
    [TestCase]
    public void PredictionUsesWorldMapForPopulationEnergyAndMiche()
    {
        var world = CreateWorld();
        var source = world.Map.CurrentPatch!;
        var original = world.PlayerSpecies;
        var target = new Patch(source.Name, source.ID + 1000, source.BiomeTemplate, source.BiomeType,
            new PatchSnapshot(source.Biome, source.Background), source.DynamicDataSeed);
        world.Map.AddPatch(target);
        AssertThat(target.GetSpeciesSimulationPopulation(original)).IsEqual(0L);

        var modified = (Species)original.Clone();
        modified.BecomePlayerSpecies();
        var run = new EditorAutoEvoRun(world, world.AutoEvoGlobalCache, original, modified, target)
        {
            ApplyPlayerPopulationChangeClamp = false,
        };
        var results = Complete(run);

        AssertThat(results.InspectPatchMicheData().Count).IsEqual(world.Map.Patches.Count);
        foreach (var patch in world.Map.Patches.Values)
        {
            AssertThat(results.InspectPatchMicheData().ContainsKey(patch)).IsTrue();
            AssertThat(results.GetPopulationInPatchIfExists(modified, patch, out _)).IsTrue();
        }

        // The target has the inhabited patch's environment. Check this live fixture can sustain the design.
        AssertThat(results.GetPopulationInPatch(modified, target)).IsGreater(0L);
        AssertThat(results.GetPatchEnergyResults(modified)[target].TotalEnergyGathered).IsGreater(0.0f);
        AssertThat(target.GetSpeciesSimulationPopulation(original)).IsEqual(0L);
        AssertThat(target.SpeciesInPatch.ContainsKey(original)).IsFalse();
        AssertThat(target.SpeciesInPatch.ContainsKey(modified)).IsFalse();
    }

    /// <summary>
    ///   Verifies that zero player Auto-Evo strength adjusts a surviving prediction back to the world's
    ///   simulation population rather than its distinct gameplay population. This detects a missing clamp
    ///   or an incorrect population source and checks that prediction leaves its inputs unchanged.
    /// </summary>
    [TestCase]
    public void PopulationClampUsesWorldSimulationPopulation()
    {
        const long simulationPopulation = 123;
        var world = CreateWorld();
        world.WorldSettings.Difficulty = new CustomDifficulty
        {
            EnergyCostMultiplier = world.WorldSettings.EnergyCostMultiplier,
            PlayerAutoEvoStrength = 0,
        };
        var source = world.Map.CurrentPatch!;
        var original = world.PlayerSpecies;

        // Distinct populations expose accidental reads from gameplay state.
        AssertThat(source.UpdateSpeciesSimulationPopulation(original, simulationPopulation)).IsTrue();
        AssertThat(source.UpdateSpeciesGameplayPopulation(original, 456)).IsTrue();

        var modified = (Species)original.Clone();
        modified.BecomePlayerSpecies();
        var unclamped = Complete(new EditorAutoEvoRun(world, world.AutoEvoGlobalCache, original, modified, source)
        {
            ApplyPlayerPopulationChangeClamp = false,
        });
        var unclampedPopulation = unclamped.GetPopulationInPatch(modified, source);

        // Extinction bypasses the clamp; an unchanged prediction would also hide a missing clamp.
        AssertThat(unclampedPopulation).OverrideFailureMessage(
            "The live fixture must sustain this design to exercise the population clamp").IsGreater(0L);
        AssertThat(unclampedPopulation).OverrideFailureMessage(
                "The live prediction already equals the simulation population; choose another population")
            .IsNotEqual(simulationPopulation);

        // Zero player Auto-Evo strength preserves the world simulation population exactly.
        var clamped = Complete(new EditorAutoEvoRun(world, world.AutoEvoGlobalCache, original, modified, source));
        AssertThat(clamped.GetPopulationInPatch(modified, source)).IsEqual(simulationPopulation);
        AssertThat(source.GetSpeciesSimulationPopulation(original)).IsEqual(simulationPopulation);
        AssertThat(source.GetSpeciesGameplayPopulation(original)).IsEqual(456L);
        AssertThat(source.SpeciesInPatch.ContainsKey(modified)).IsFalse();
    }

    /// <summary>
    ///   Verifies that predicting a modified player design preserves an NPC's chemoreceptor target and the
    ///   world map's species and populations. Prediction must not apply replacements to the live world.
    /// </summary>
    [TestCase]
    public void PredictionPreservesSharedNpcChemoreceptorTarget()
    {
        var cellType = SimulationCacheTestFixtures.CreateCellType("Player", "cellulose",
            SimulationCacheTestFixtures.CreateOrganelle("cytoplasm", new Hex(0, 0)));
        var player = SimulationCacheTestFixtures.CreateMulticellular(1, "PlayerPrey", (cellType, new Hex(0, 0)));
        var settings = new WorldGenerationSettings
        {
            Seed = 3171,
            WorldSize = WorldGenerationSettings.WorldSizeEnum.Small,
        };
        var world = GameProperties.StartNewMulticellularGame(settings, startingSpecies: player).GameWorld;
        var patch = world.Map.CurrentPatch!;
        var predator = SimulationCacheTestFixtures.CreateMicrobe(1000, "Hunter", "cellulose",
            "cytoplasm", "metabolosome", "metabolosome");
        predator.Organelles.Add(SimulationCacheTestFixtures.CreateOrganelle("oxytoxy", new Hex(0, -2)));
        var receptor = SimulationCacheTestFixtures.CreateOrganelle("chemoreceptor", new Hex(0, 2));
        receptor.ModifiableUpgrades = new OrganelleUpgrades
        {
            CustomUpgradeData = new ChemoreceptorUpgrades(Compound.Invalid, player, 100, 1, default),
        };
        predator.Organelles.Add(receptor);
        predator.ModifiableBehaviour.Activity = Constants.MAX_SPECIES_ACTIVITY;
        predator.ModifiableBehaviour.Aggression = Constants.MAX_SPECIES_AGGRESSION;
        predator.OnEdited();
        GameWorld.SetSpeciesInitialTolerances(predator, world.Map, null);
        AssertThat(patch.AddSpecies(predator, 100)).IsTrue();
        var playerPopulation = patch.GetSpeciesSimulationPopulation(player);
        var predatorPopulation = patch.GetSpeciesSimulationPopulation(predator);
        var speciesCount = patch.SpeciesInPatch.Count;
        var modified = player.Clone(true, true);
        modified.BecomePlayerSpecies();

        var predicted = Complete(new EditorAutoEvoRun(world, world.AutoEvoGlobalCache, player, modified, patch));

        AssertThat(predicted.InspectPatchMicheData().ContainsKey(patch)).IsTrue();
        AssertThat(predicted.GetPopulationInPatchIfExists(modified, patch, out _)).IsTrue();
        AssertThat(predicted.GetPopulationInPatchIfExists(predator, patch, out _)).IsTrue();
        AssertThat(((ChemoreceptorUpgrades)receptor.Upgrades!.CustomUpgradeData!).TargetSpecies).IsSame(player);
        AssertThat(patch.SpeciesInPatch.Count).IsEqual(speciesCount);
        AssertThat(patch.SpeciesInPatch.ContainsKey(player)).IsTrue();
        AssertThat(patch.SpeciesInPatch.ContainsKey(predator)).IsTrue();
        AssertThat(patch.SpeciesInPatch.ContainsKey(modified)).IsFalse();
        AssertThat(patch.GetSpeciesSimulationPopulation(player)).IsEqual(playerPopulation);
        AssertThat(patch.GetSpeciesSimulationPopulation(predator)).IsEqual(predatorPopulation);
    }

    private static GameWorld CreateWorld()
    {
        return new GameWorld(new WorldGenerationSettings
        {
            Seed = 3171,
            WorldSize = WorldGenerationSettings.WorldSizeEnum.Small,
        });
    }

    private static RunResults Complete(EditorAutoEvoRun run)
    {
        var timer = Stopwatch.StartNew();
        try
        {
            while (!run.Finished && timer.Elapsed < TimeSpan.FromSeconds(30))
                run.OneStep();

            AssertThat(run.Finished).IsTrue();
            AssertThat(run.WasSuccessful).IsTrue();
            return run.Results!;
        }
        finally
        {
            // OneStep executes synchronously, so no worker can retain test inputs after this cleanup.
            if (!run.Finished)
            {
                run.Abort();
                run.OneStep();
            }
        }
    }
}
