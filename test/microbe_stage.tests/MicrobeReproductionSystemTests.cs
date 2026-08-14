using System;
using System.Collections.Generic;
using System.Linq;
using Arch.Core;
using Arch.Core.Extensions;
using Components;
using GdUnit4;
using Godot;
using Systems;
using Test.Utils;

[TestSuite]
[RequireGodotRuntime]
public class MicrobeReproductionSystemTests
{
    private const float SimulationStep = 0.1f;
    private const float GrowthCompoundAmountPerStep = 10;
    private const int MaximumReproductionSteps = 1000;

    [TestCase]
    public void TestReproductionRefreshesRuntimeStateAfterInPlaceSpeciesMutation()
    {
        using var worldSimulation = new TestWorldSimulation();
        var gameWorld = new GameWorld(new WorldGenerationSettings());
        var spawnEnvironment = new DummyMicrobeSpawnEnvironment();
        bool offspringSpawnReported = false;

        void ReportOffspringSpawned(in Entity entity)
        {
            offspringSpawnReported = true;
        }

        var spawnSystem = new DummySpawnSystem(ReportOffspringSpawned)
        {
            AllowReproduction = true,
        };

        var reproductionSystem = new MicrobeReproductionSystem(worldSimulation, spawnEnvironment, spawnSystem,
            worldSimulation.EntitySystem);
        reproductionSystem.SetWorld(gameWorld);

        var simulationParameters = SimulationParameters.Instance;
        var bacterialSpecies = CreateBacterialSpecies(simulationParameters);
        var parentMicrobe = SpawnAiMicrobe(worldSimulation, spawnEnvironment, bacterialSpecies);

        AssertInitialBacterialRuntimeState(parentMicrobe);

        var eukaryoticMutation = CreateEukaryoticMutation(simulationParameters, bacterialSpecies);
        bacterialSpecies.ApplyMutation(eukaryoticMutation);

        AssertParentStillHasStaleBacterialState(parentMicrobe);

        RunReproductionUntilSpawnReported(parentMicrobe, reproductionSystem, worldSimulation,
            () => offspringSpawnReported);

        Assertions.AssertThat(offspringSpawnReported).IsTrue();

        var offspringMicrobe = FindOffspring(worldSimulation, parentMicrobe);

        Assertions.AssertThat(offspringMicrobe).IsNotEqual(Entity.Null);
        AssertEukaryoticRuntimeState(parentMicrobe);
        AssertEukaryoticRuntimeState(offspringMicrobe);
    }

    private static MicrobeSpecies CreateBacterialSpecies(SimulationParameters simulationParameters)
    {
        var bacterialSpecies = new MicrobeSpecies(1, "Test", "bacteria")
        {
            IsBacteria = true,
            MembraneType = simulationParameters.GetMembrane("single"),
            Colour = new Color(1, 1, 1),
        };

        bacterialSpecies.Organelles.Add(new OrganelleTemplate(
            simulationParameters.GetOrganelleType("cytoplasm"), new Hex(0, 0), 0));
        bacterialSpecies.OnEdited();

        return bacterialSpecies;
    }

    private static MicrobeSpecies CreateEukaryoticMutation(SimulationParameters simulationParameters,
        MicrobeSpecies bacterialSpecies)
    {
        var eukaryoticMutation = new MicrobeSpecies(2, "Test", "eukaryote")
        {
            IsBacteria = false,
            MembraneType = bacterialSpecies.MembraneType,
            Colour = bacterialSpecies.Colour,
        };

        var organellePlacementWorkMemory1 = new List<Hex>();
        var organellePlacementWorkMemory2 = new List<Hex>();
        eukaryoticMutation.Organelles.AddFast(
            new OrganelleTemplate(simulationParameters.GetOrganelleType("nucleus"), new Hex(0, 0), 0),
            organellePlacementWorkMemory1, organellePlacementWorkMemory2);
        eukaryoticMutation.Organelles.AddFast(
            new OrganelleTemplate(simulationParameters.GetOrganelleType("cytoplasm"), new Hex(3, 0), 0),
            organellePlacementWorkMemory1, organellePlacementWorkMemory2);
        eukaryoticMutation.OnEdited();

        return eukaryoticMutation;
    }

    private static Entity SpawnAiMicrobe(TestWorldSimulation worldSimulation,
        DummyMicrobeSpawnEnvironment spawnEnvironment, MicrobeSpecies species)
    {
        SpawnHelpers.SpawnMicrobe(worldSimulation, spawnEnvironment, species, Vector3.Zero, true, GameteType.All);
        worldSimulation.ProcessAll(SimulationStep);

        return new FirstEntityGrabber(new QueryDescription().WithAll<MicrobeAI>(),
            worldSimulation.EntitySystem).Found;
    }

    private static void RunReproductionUntilSpawnReported(Entity parentMicrobe,
        MicrobeReproductionSystem reproductionSystem, TestWorldSimulation worldSimulation,
        Func<bool> spawnWasReported)
    {
        ref var storage = ref parentMicrobe.Get<CompoundStorage>();

        for (int i = 0; i < MaximumReproductionSteps && !spawnWasReported(); ++i)
        {
            storage.Compounds.AddCompound(Compound.Ammonia, GrowthCompoundAmountPerStep);
            storage.Compounds.AddCompound(Compound.Phosphates, GrowthCompoundAmountPerStep);

            reproductionSystem.BeforeUpdate(SimulationStep);
            reproductionSystem.Update(SimulationStep);
            reproductionSystem.AfterUpdate(SimulationStep);
            worldSimulation.ProcessAll(SimulationStep);
        }
    }

    private static Entity FindOffspring(TestWorldSimulation worldSimulation, Entity parentMicrobe)
    {
        var offspringMicrobe = Entity.Null;
        worldSimulation.EntitySystem.Query(new QueryDescription().WithAll<MicrobeAI>(), entity =>
        {
            if (entity != parentMicrobe)
                offspringMicrobe = entity;
        });

        return offspringMicrobe;
    }

    private static void AssertInitialBacterialRuntimeState(Entity microbe)
    {
        Assertions.AssertThat(microbe.Get<CellProperties>().IsBacteria).IsTrue();
        Assertions.AssertThat(microbe.Get<SpatialInstance>().VisualScale)
            .IsEqual(Vector3.One * Constants.BACTERIA_CELL_SCALE);
        Assertions.AssertThat(microbe.Get<SpatialInstance>().ApplyVisualScale).IsTrue();
    }

    private static void AssertParentStillHasStaleBacterialState(Entity parentMicrobe)
    {
        // Applying auto-evo results mutates the species object in place, leaving existing entities with stale state.
        Assertions.AssertThat(parentMicrobe.Get<CellProperties>().IsBacteria).IsTrue();
        Assertions.AssertThat(parentMicrobe.Get<SpatialInstance>().VisualScale)
            .IsEqual(Vector3.One * Constants.BACTERIA_CELL_SCALE);
    }

    private static void AssertEukaryoticRuntimeState(Entity microbe)
    {
        Assertions.AssertThat(microbe.Get<CellProperties>().IsBacteria).IsFalse();
        Assertions.AssertThat(microbe.Get<SpatialInstance>().VisualScale).IsEqual(Vector3.One);
        Assertions.AssertThat(microbe.Get<SpatialInstance>().ApplyVisualScale).IsTrue();
        Assertions.AssertThat(HasOrganelle(microbe, "nucleus")).IsTrue();
    }

    private static bool HasOrganelle(Entity microbe, string internalName)
    {
        return microbe.Get<OrganelleContainer>().Organelles!.Any(organelle => organelle.Definition.InternalName == internalName);
    }
}
