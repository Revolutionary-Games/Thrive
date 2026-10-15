using System;
using System.Diagnostics;
using AutoEvo;
using GdUnit4;
using static GdUnit4.Assertions;

/// <summary>
///   Exercises editor predictions against the world map and isolated prediction populations.
/// </summary>
[TestSuite]
[RequireGodotRuntime]
public class EditorAutoEvoRunTests
{
    [TestCase(false)]
    [TestCase(true)]
    public void PredictionUsesSelectedMapForPopulationEnergyAndMiche(bool usePredictionMap)
    {
        var world = CreateWorld();
        var source = world.Map.CurrentPatch!;
        var original = world.PlayerSpecies;
        var target = new Patch(source.Name, source.ID + 1000, source.BiomeTemplate, source.BiomeType,
            new PatchSnapshot(source.Biome, source.Background), source.DynamicDataSeed);
        world.Map.AddPatch(target);
        AssertThat(target.GetSpeciesSimulationPopulation(original)).IsEqual(0L);

        PatchMap? predictionMap = null;
        if (usePredictionMap)
        {
            original = (Species)original.Clone();
            original.BecomePlayerSpecies();
            predictionMap = CreatePredictionMap(world.Map, original);
            target = predictionMap.Patches[target.ID];
        }

        var modified = (Species)original.Clone();
        modified.BecomePlayerSpecies();
        var run = new EditorAutoEvoRun(world, world.AutoEvoGlobalCache, original, modified, target, predictionMap)
        {
            ApplyPlayerPopulationChangeClamp = false,
        };
        var results = Complete(run);
        var selectedMap = predictionMap ?? world.Map;

        AssertThat(results.InspectPatchMicheData().Count).IsEqual(selectedMap.Patches.Count);
        foreach (var patch in selectedMap.Patches.Values)
        {
            AssertThat(results.InspectPatchMicheData().ContainsKey(patch)).IsTrue();
            AssertThat(results.GetPopulationInPatchIfExists(modified, patch, out _)).IsTrue();
        }

        // The target has the inhabited patch's environment. Check this live fixture can sustain the design.
        AssertThat(results.GetPopulationInPatch(modified, target)).IsGreater(0L);
        AssertThat(results.GetPatchEnergyResults(modified)[target].TotalEnergyGathered).IsGreater(0.0f);
        AssertThat(target.GetSpeciesSimulationPopulation(original)).IsEqual(0L);
        AssertThat(world.Map.Patches[target.ID].GetSpeciesSimulationPopulation(world.PlayerSpecies)).IsEqual(0L);

        if (usePredictionMap)
        {
            foreach (var patch in world.Map.Patches.Values)
            {
                AssertThat(results.InspectPatchMicheData().ContainsKey(patch)).IsFalse();
                AssertThat(results.GetPopulationInPatchIfExists(modified, patch, out _)).IsFalse();
                AssertThat(results.GetPatchEnergyResults(modified).ContainsKey(patch)).IsFalse();
            }
        }
    }

    [TestCase]
    public void PopulationClampUsesIsolatedSimulationPopulation()
    {
        var world = CreateWorld();
        world.WorldSettings.Difficulty = new CustomDifficulty
        {
            EnergyCostMultiplier = world.WorldSettings.EnergyCostMultiplier,
            PlayerAutoEvoStrength = 0,
        };
        var source = world.Map.CurrentPatch!;
        AssertThat(source.UpdateSpeciesSimulationPopulation(world.PlayerSpecies, 123)).IsTrue();
        AssertThat(source.UpdateSpeciesGameplayPopulation(world.PlayerSpecies, 456)).IsTrue();
        source.RecordSnapshot(false);
        var original = (Species)world.PlayerSpecies.Clone();
        original.BecomePlayerSpecies();
        var map = CreatePredictionMap(world.Map, original);
        var copied = map.CurrentPatch!;
        AssertThat(source.UpdateSpeciesSimulationPopulation(world.PlayerSpecies, 789)).IsTrue();

        AssertThat(copied.Biome).IsSame(source.Biome);
        AssertThat(copied.SpeciesInPatch.ContainsKey(world.PlayerSpecies)).IsFalse();
        AssertThat(copied.GetSpeciesGameplayPopulation(original)).IsEqual(123L);
        AssertThat(copied.Adjacent.Count).IsEqual(0);
        AssertThat(copied.History.Count).IsEqual(0);
        AssertThat(copied.ID).IsEqual(source.ID);
        AssertThat(copied.VisibleName).IsEqual(source.VisibleName);
        foreach (var entry in source.SpeciesInPatch)
        {
            if (entry.Key.ID != original.ID)
                AssertThat(copied.SpeciesInPatch.ContainsKey(entry.Key)).IsTrue();
        }

        var modified = (Species)original.Clone();
        modified.BecomePlayerSpecies();
        var unclamped = Complete(new EditorAutoEvoRun(world, world.AutoEvoGlobalCache, original, modified, copied, map)
        {
            ApplyPlayerPopulationChangeClamp = false,
        });
        AssertThat(unclamped.GetPopulationInPatch(modified, copied)).IsGreater(0L);

        // Zero player Auto-Evo strength preserves the copied simulation population exactly.
        var clamped = Complete(new EditorAutoEvoRun(world, world.AutoEvoGlobalCache, original, modified, copied, map));
        AssertThat(clamped.GetPopulationInPatch(modified, copied)).IsEqual(123L);
        AssertThat(source.GetSpeciesSimulationPopulation(world.PlayerSpecies)).IsEqual(789L);
        AssertThat(source.GetSpeciesGameplayPopulation(world.PlayerSpecies)).IsEqual(456L);
        AssertThat(copied.GetSpeciesSimulationPopulation(original)).IsEqual(123L);
    }

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
        var historical = player.Clone(true, true);
        historical.BecomePlayerSpecies();
        var map = CreatePredictionMap(world.Map, historical);
        var copiedPatch = map.CurrentPatch!;
        AssertThat(copiedPatch.SpeciesInPatch.ContainsKey(predator)).IsTrue();
        var predicted = Complete(new EditorAutoEvoRun(world, world.AutoEvoGlobalCache,
            historical, historical, copiedPatch, map));

        AssertThat(copiedPatch.SpeciesInPatch.ContainsKey(player)).IsFalse();
        AssertThat(copiedPatch.SpeciesInPatch.ContainsKey(historical)).IsTrue();
        AssertThat(predicted.InspectPatchMicheData().ContainsKey(copiedPatch)).IsTrue();
        AssertThat(predicted.GetPopulationInPatchIfExists(historical, copiedPatch, out _)).IsTrue();
        AssertThat(predicted.GetPopulationInPatchIfExists(predator, copiedPatch, out _)).IsTrue();
        AssertThat(((ChemoreceptorUpgrades)receptor.Upgrades!.CustomUpgradeData!).TargetSpecies).IsSame(player);
        AssertThat(patch.GetSpeciesSimulationPopulation(player)).IsEqual(playerPopulation);
        AssertThat(patch.GetSpeciesSimulationPopulation(predator)).IsEqual(predatorPopulation);
        AssertThat(copiedPatch.GetSpeciesSimulationPopulation(historical)).IsEqual(playerPopulation);
        AssertThat(copiedPatch.GetSpeciesSimulationPopulation(predator)).IsEqual(predatorPopulation);
    }

    private static GameWorld CreateWorld()
    {
        return new GameWorld(new WorldGenerationSettings
        {
            Seed = 3171,
            WorldSize = WorldGenerationSettings.WorldSizeEnum.Small,
        });
    }

    private static PatchMap CreatePredictionMap(PatchMap source, Species original)
    {
        var result = new PatchMap();
        foreach (var patch in source.Patches.Values)
            result.AddPatch(patch.CreatePredictionCopy(original));

        result.CurrentPatch = result.Patches[source.CurrentPatch!.ID];
        return result;
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
