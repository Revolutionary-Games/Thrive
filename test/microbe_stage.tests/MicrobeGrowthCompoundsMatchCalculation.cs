using System;
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
public class MicrobeGrowthCompoundsMatchCalculation
{
    private bool readyReported;

    [TestCase]
    public void TestMicrobeMatchesExpected()
    {
        using var worldSimulation = new TestWorldSimulation();
        var gameWorld = new GameWorld(new WorldGenerationSettings());
        var spawnEnvironment = new DummyMicrobeSpawnEnvironment();
        var reproductionSystem = new MicrobeReproductionSystem(worldSimulation, spawnEnvironment,
            new DummySpawnSystem(), worldSimulation.EntitySystem);
        reproductionSystem.SetWorld(gameWorld);

        var simulationParameters = SimulationParameters.Instance;

        // Set up a test species
        var species = new MicrobeSpecies(1, "Test", "microbe")
        {
            MembraneType = simulationParameters.GetMembrane("single"),
            Colour = new Color(1, 1, 1),
        };

        var work1 = new List<Hex>();
        var work2 = new List<Hex>();
        species.Organelles.AddFast(
            new OrganelleTemplate(simulationParameters.GetOrganelleType("nucleus"), new Hex(0, 0), 0), work1, work2);
        species.Organelles.AddFast(
            new OrganelleTemplate(simulationParameters.GetOrganelleType("cytoplasm"), new Hex(3, 0), 0), work1, work2);
        species.OnEdited();

        // Spawn a cell of the type for the "player"
        SpawnHelpers.SpawnMicrobe(worldSimulation, spawnEnvironment, species, new Vector3(0, 0, 0), false);
        worldSimulation.ProcessAll(0.1f);

        // Get the cell
        var microbe =
            new FirstEntityGrabber(new QueryDescription().WithAll<PlayerMarker>(), worldSimulation.EntitySystem).Found;

        Assertions.AssertThat(microbe.Has<CellProperties>()).IsTrue();
        Assertions.AssertThat(microbe.Has<MicrobeSpeciesMember>()).IsTrue();
        Assertions.AssertThat(microbe.Has<BioProcesses>()).IsTrue();
        Assertions.AssertThat(microbe.Has<WorldPosition>()).IsTrue();
        Assertions.AssertThat(microbe.Has<MicrobeEnvironmentalEffects>()).IsTrue();
        Assertions.AssertThat(microbe.Has<Engulfable>()).IsTrue();
        Assertions.AssertThat(microbe.Has<Engulfer>()).IsTrue();
        Assertions.AssertThat(microbe.Has<OrganelleContainer>()).IsTrue();
        Assertions.AssertThat(microbe.Has<MicrobeControl>()).IsTrue();
        Assertions.AssertThat(microbe.Has<Health>()).IsTrue();
        Assertions.AssertThat(microbe.Has<CompoundStorage>()).IsTrue();
        Assertions.AssertThat(microbe.Has<MicrobeStatus>()).IsTrue();
        Assertions.AssertThat(microbe.Has<ReproductionStatus>()).IsTrue();

        // Add callbacks to not split automatically on being ready
        microbe.Add(new MicrobeEventCallbacks
        {
            OnReproductionStatus = (entity, ready) =>
            {
                if (entity == microbe && ready)
                    readyReported = true;

                if (entity != microbe)
                    throw new Exception("Wrong entity reported for callback");
            },
        });

        readyReported = false;

        var calculatedGrowthNeeds = new Dictionary<Compound, float>();
        microbe.Get<OrganelleContainer>().CalculateTotalReproductionCompounds(microbe, species, calculatedGrowthNeeds);

        ref var storage = ref microbe.Get<CompoundStorage>();

        Assertions.AssertThat(readyReported).IsFalse();
        var neededOriginal = new Dictionary<Compound, float>();
        microbe.Get<OrganelleContainer>().CalculateTotalReproductionCompounds(microbe, species, neededOriginal);

        // Then simulate it growing
        for (int i = 0; i < 1000; ++i)
        {
            // Give some growth resources
            storage.Compounds.AddCompound(Compound.Ammonia, 10);
            storage.Compounds.AddCompound(Compound.Phosphates, 10);

            reproductionSystem.BeforeUpdate(0.1f);
            reproductionSystem.Update(0.1f);
            reproductionSystem.AfterUpdate(0.1f);
            worldSimulation.ProcessAll(0.1f);

            // Stop immediately once ready
            if (readyReported)
                break;
        }

        Assertions.AssertThat(readyReported).IsTrue();

        // And check that the actual compounds usage is correct to the estimated one
        ref var reproductionStatus = ref microbe.Get<ReproductionStatus>();
        ref var organelles = ref microbe.Get<OrganelleContainer>();

        Assertions.AssertThat(organelles.AllOrganellesDivided).IsTrue();

        var totalNeeded = new Dictionary<Compound, float>();
        organelles.CalculateTotalReproductionCompounds(microbe, species, totalNeeded);

        // Make sure total needed didn't change
        Assertions.AssertThat(neededOriginal.DictionaryEquals(totalNeeded)).IsTrue();

        var usedCompounds = new Dictionary<Compound, float>();
        organelles.CalculateAlreadyAbsorbedCompounds(ref reproductionStatus, microbe, species, usedCompounds);

        var usedSameAmount =
            usedCompounds.DictionaryEqualsApprox(totalNeeded, 0.0001f);
        Assertions.AssertThat(usedSameAmount).IsTrue();

        // Finally, check that the actual resource usage matches the calculated one
        var pass = usedCompounds.DictionaryEqualsApprox(calculatedGrowthNeeds, 0.0001f);

        Assertions.AssertThat(pass).IsTrue();
    }
}
