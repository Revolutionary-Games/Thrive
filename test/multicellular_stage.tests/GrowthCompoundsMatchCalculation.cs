using System.Collections.Generic;
using Arch.Core;
using Arch.Core.Extensions;
using Components;
using GdUnit4;
using Godot;
using Systems;
using Test.Utils;

[TestSuite]
[RequireGodotRuntime]
public class GrowthCompoundsMatchCalculation
{
    [TestCase]
    public void TestSingleSporeCellMatchesExpected()
    {
        using var worldSimulation = new TestWorldSimulation();
        var gameWorld = new GameWorld(new WorldGenerationSettings());
        var spawnEnvironment = new DummyMicrobeSpawnEnvironment();
        var growthSystem = new MulticellularGrowthSystem(worldSimulation, spawnEnvironment,
            new DummySpawnSystem(), worldSimulation.EntitySystem);
        growthSystem.SetWorld(gameWorld);

        var simulationParameters = SimulationParameters.Instance;

        // Set up a test species
        var species = new MulticellularSpecies(1, "Test", "species");

        var sporeType =
            new CellType(new OrganelleLayout<OrganelleTemplate>([
                    new OrganelleTemplate(simulationParameters.GetOrganelleType("nucleus"), new Hex(0, 0), 0),
                    new OrganelleTemplate(simulationParameters.GetOrganelleType("cytoplasm"), new Hex(5, 0), 0),
                    new OrganelleTemplate(simulationParameters.GetOrganelleType("cytoplasm"), new Hex(6, 0), 0),
                    new OrganelleTemplate(simulationParameters.GetOrganelleType("cytoplasm"), new Hex(7, 0), 0),
                    new OrganelleTemplate(simulationParameters.GetOrganelleType("cytoplasm"), new Hex(8, 0), 0),
                ], null, null),
                simulationParameters.GetMembrane("single"));
        species.ModifiableCellTypes.Add(sporeType);

        // Fewer organelles to have a smaller cost (this is the root of the bug this test is against)
        var mainType =
            new CellType(new OrganelleLayout<OrganelleTemplate>([
                    new OrganelleTemplate(simulationParameters.GetOrganelleType("nucleus"), new Hex(0, 0), 0),
                    new OrganelleTemplate(simulationParameters.GetOrganelleType("cytoplasm"), new Hex(5, 0), 0),
                ], null, null),
                simulationParameters.GetMembrane("single"));
        species.ModifiableCellTypes.Add(mainType);

        species.ModifiableGameplayCells.AddFast(new CellTemplate(mainType, new Hex(0, 0), 0),
            new List<Hex>(), new List<Hex>());
        species.ReproductionMethod = MulticellularReproductionMethod.Sporulation;
        species.ModifiableSporeCellType = sporeType;
        species.OnEdited();

        // Spawn a cell of the type for the "player"
        SpawnHelpers.SpawnMicrobe(worldSimulation, spawnEnvironment, species, new Vector3(0, 0, 0), false,
            MulticellularSpawnState.Offspring);
        worldSimulation.ProcessAll(0.1f);

        // Get the cell
        var microbe =
            new FirstEntityGrabber(new QueryDescription().WithAll<PlayerMarker>(), worldSimulation.EntitySystem).Found;

        Assertions.AssertThat(microbe.Get<MulticellularGrowth>().IsFullyGrownMulticellular).IsFalse();

        var germinated = microbe.Get<MulticellularGrowth>()
            .GerminateSpore(microbe, worldSimulation, spawnEnvironment, new List<Hex>(), new List<Hex>());

        Assertions.AssertThat(germinated).IsTrue();

        Assertions.AssertThat(microbe.Get<MulticellularGrowth>().IsFullyGrownMulticellular).IsFalse();
        Assertions.AssertThat(microbe.Get<MulticellularGrowth>().EnoughResourcesForBudding).IsFalse();

        ref var storage = ref microbe.Get<CompoundStorage>();

        // Then simulate it growing
        for (int i = 0; i < 1000; ++i)
        {
            // Give some growth resources
            storage.Compounds.AddCompound(Compound.Ammonia, 10);
            storage.Compounds.AddCompound(Compound.Phosphates, 10);

            growthSystem.Update(0.1f);
            worldSimulation.ProcessAll(0.1f);
        }

        // And check that the actual compounds usage is correct to the estimated one
        var growth = microbe.Get<MulticellularGrowth>();

        Assertions.AssertThat(growth.IsFullyGrownMulticellular).IsTrue();
        Assertions.AssertThat(growth.EnoughResourcesForBudding).IsTrue();
    }
}
