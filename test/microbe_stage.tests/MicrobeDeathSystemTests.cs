using System;
using System.Collections.Generic;
using Arch.Core;
using Arch.Core.Extensions;
using Components;
using GdUnit4;
using Godot;
using Systems;
using Test.Utils;
using static GdUnit4.Assertions;

[TestSuite]
[RequireGodotRuntime]
public class MicrobeDeathSystemTests
{
    [TestCase]
    public void CorpseChunkCountScalesWithCellSize()
    {
        AssertThat(MicrobeDeathSystem.CalculateCorpseChunkCount(1))
            .IsEqual(Constants.CORPSE_CHUNK_MINIMUM);
        AssertThat(MicrobeDeathSystem.CalculateCorpseChunkCount(10)).IsEqual(2);
        AssertThat(MicrobeDeathSystem.CalculateCorpseChunkCount(15)).IsEqual(3);
        AssertThat(MicrobeDeathSystem.CalculateCorpseChunkCount(25))
            .IsEqual(Constants.CORPSE_CHUNK_AMOUNT_CAP);
        AssertThat(MicrobeDeathSystem.CalculateCorpseChunkCount(100))
            .IsEqual(Constants.CORPSE_CHUNK_AMOUNT_CAP);
    }

    [TestCase]
    public void CorpseChunksContainExpectedAmountOfCompounds()
    {
        using var world = new TestWorldSimulation();
        var spawnEnvironment = new DummyMicrobeSpawnEnvironment();
        var simulationParameters = SimulationParameters.Instance;
        var species = new MicrobeSpecies(1, "Test", "corpse")
        {
            IsBacteria = true,
            MembraneType = simulationParameters.GetMembrane("single"),
            Colour = Colors.White,
        };
        species.Organelles.Add(new OrganelleTemplate(simulationParameters.GetOrganelleType("cytoplasm"),
            new Hex(0, 0), 0));
        species.OnEdited();

        SpawnHelpers.SpawnMicrobe(world, spawnEnvironment, species, Vector3.Zero, false, GameteType.All);
        world.ProcessAll(0.1f);

        var microbe = new FirstEntityGrabber(new QueryDescription().WithAll<MicrobeSpeciesMember>(),
            world.EntitySystem).Found;
        ref var storage = ref microbe.Get<CompoundStorage>();

        // Use all cloud compounds so the test covers both storage contents and organelle composition.
        const float storedAmountPerCompound = 100.0f;
        foreach (var compound in simulationParameters.GetCloudCompounds())
            storage.Compounds.Compounds[compound.ID] = storedAmountPerCompound;

        // Hack the hex count to make sure we get a lot of chunks.
        ref var organelles = ref microbe.Get<OrganelleContainer>();
        organelles.HexCount = 30;

        var spawnSystem = new DummySpawnSystem();

        world.EntitySystem.Query(new QueryDescription().WithAll<CompoundVenter, CompoundStorage>(), _ =>
            throw new Exception("Chunks shouldn't exist yet)"));

        var recorder = world.StartRecordingEntityCommands();
        MicrobeDeathSystem.SpawnCorpseChunks(ref organelles, storage.Compounds, spawnSystem, world, recorder,
            Vector3.Zero, new Random(1), null, true);
        world.FinishRecordingEntityCommands(recorder);
        world.ProcessAll(0.1f);

        // Chunks only exists now, so grab them
        var spawnedChunks = new List<Entity>();
        world.EntitySystem.Query(new QueryDescription().WithAll<CompoundVenter, CompoundStorage>(),
            spawnedChunks.Add);

        if (spawnedChunks.Count < 0)
            throw new InvalidOperationException("No chunks spawned / found");

        // Calculate how much stuff will be released.
        var expectedChunkCount = MicrobeDeathSystem.CalculateCorpseChunkCount(organelles.HexCount);
        var expectedAmounts = new Dictionary<Compound, float>();
        foreach (var compound in simulationParameters.GetCloudCompounds())
        {
            var released = storedAmountPerCompound * Constants.COMPOUND_RELEASE_FRACTION;
            foreach (var organelle in organelles.Organelles!)
            {
                foreach (var composition in organelle.Definition.InitialComposition)
                {
                    if (composition.Key == compound.ID)
                        released += composition.Value * Constants.COMPOUND_MAKEUP_RELEASE_FRACTION;
                }
            }

            expectedAmounts[compound.ID] = released / expectedChunkCount;
        }

        // And verify that the chunks have the expected amounts.
        AssertThat(spawnedChunks.Count).IsEqual(expectedChunkCount);
        foreach (var chunk in spawnedChunks)
        {
            if (!chunk.IsAliveAndHas<CompoundStorage>())
                throw new InvalidOperationException("Expected entity doesn't have compound storage");

            var chunkCompounds = chunk.Get<CompoundStorage>().Compounds;
            foreach (var expected in expectedAmounts)
            {
                AssertThat(chunkCompounds.GetCompoundAmount(expected.Key))
                    .IsEqualApprox(expected.Value, 0.0001f);
            }
        }
    }
}
