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
    public void GameteAtZeroHealthDissolvesBeforeDespawning()
    {
        using var world = new TestWorldSimulation();
        var spawnSystem = new DummySpawnSystem();
        using var deathSystem = new MicrobeDeathSystem(world, spawnSystem, world.EntitySystem);
        using var shaderSystem = new MicrobeShaderSystem(world.EntitySystem);
        using var timedLifeSystem = new TimedLifeSystem(world, world.EntitySystem);
        var gameWorld = new GameWorld(new WorldGenerationSettings());
        deathSystem.SetWorld(gameWorld);

        var parameters = SimulationParameters.Instance;
        var cellType = new CellType(parameters.GetMembrane("single"));
        cellType.ModifiableOrganelles.Add(new OrganelleTemplate(parameters.GetOrganelleType("cytoplasm"),
            new Hex(0, 0), 0));
        var species = new MulticellularSpecies(2, "Test", "gamete")
        {
            ModifiableGameteTypeA = cellType,
            ReproductionMethod = MulticellularReproductionMethod.SexualIsogamy,
        };
        species.ModifiableCellTypes.Add(cellType);
        var workMemory1 = new List<Hex>();
        var workMemory2 = new List<Hex>();
        species.ModifiableGameplayCells.AddFast(new CellTemplate(cellType, new Hex(0, 0), 0), workMemory1, workMemory2);
        species.ModifiableGameplayCells.AddFast(new CellTemplate(cellType, new Hex(1, 0), 0), workMemory1, workMemory2);
        species.OnEdited();

        const float step = 0.1f;
        SpawnHelpers.SpawnGamete(world, new DummyMicrobeSpawnEnvironment(), spawnSystem, species, Vector3.Zero,
            Vector3.Forward, GameteType.A, true, Entity.Null);
        world.ProcessAll(step);
        var gamete = new FirstEntityGrabber(new QueryDescription().WithAll<GameteCell>(), world.EntitySystem).Found;

        gamete.Get<Health>().CurrentHealth = 0;
        deathSystem.BeforeUpdate(step);
        deathSystem.Update(step);
        world.ProcessAll(step);

        AssertThat(gamete.Get<Health>().Dead).IsTrue();
        AssertThat(gamete.Get<Health>().DeathProcessed).IsTrue();
        AssertThat(gamete.Get<MicrobeShaderParameters>().PlayAnimations).IsTrue();
        AssertThat(gamete.Has<TimedLife>()).IsTrue();

        var animationTime = 1.1f / Constants.MEMBRANE_DISSOLVE_SPEED;
        shaderSystem.Update(animationTime);
        timedLifeSystem.Update(animationTime);
        world.ProcessAll(step);

        AssertThat(gamete.IsAlive()).IsTrue();
        AssertThat(gamete.Get<MicrobeShaderParameters>().DissolveValue).IsEqual(1.0f);

        timedLifeSystem.Update(gamete.Get<TimedLife>().TimeToLiveRemaining);
        world.ProcessAll(step);
        AssertThat(gamete.IsAlive()).IsFalse();
    }

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

                    if (compound.ID == Compound.Glucose)
                    {
                        released += composition.Value * Constants.COMPOUND_MAKEUP_RELEASE_FRACTION *
                            Constants.ADDITIONAL_DIGESTIBLE_GLUCOSE_AMOUNT_MULTIPLIER;
                    }
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

    [TestCase]
    public void CorpseChunksSpawnedFromCavityCellsHaveYPositionZero()
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

        var cavityCellSpawnPosition = new Vector3(10.0f, Constants.CAVITY_CELL_Y_OFFSET, 20.0f);
        SpawnHelpers.SpawnMicrobe(world, spawnEnvironment, species, cavityCellSpawnPosition, false, GameteType.All);
        world.ProcessAll(0.1f);

        var microbe = new FirstEntityGrabber(new QueryDescription().WithAll<MicrobeSpeciesMember>(),
            world.EntitySystem).Found;
        ref var storage = ref microbe.Get<CompoundStorage>();

        const float storedAmountPerCompound = 100.0f;
        foreach (var compound in simulationParameters.GetCloudCompounds())
            storage.Compounds.Compounds[compound.ID] = storedAmountPerCompound;

        ref var organelles = ref microbe.Get<OrganelleContainer>();
        organelles.HexCount = 5;

        var spawnSystem = new DummySpawnSystem();

        var recorder = world.StartRecordingEntityCommands();
        MicrobeDeathSystem.SpawnCorpseChunks(ref organelles, storage.Compounds, spawnSystem, world, recorder,
            cavityCellSpawnPosition, new Random(1), null, true);
        world.FinishRecordingEntityCommands(recorder);
        world.ProcessAll(0.1f);

        var spawnedChunks = new List<Entity>();
        world.EntitySystem.Query(new QueryDescription().WithAll<CompoundVenter, CompoundStorage, WorldPosition>(),
            spawnedChunks.Add);

        AssertThat(spawnedChunks.Count).IsGreater(0);
        foreach (var chunk in spawnedChunks)
        {
            var chunkPosition = chunk.Get<WorldPosition>().Position;
            AssertThat(chunkPosition.Y).IsEqualApprox(0.0f, 0.0001f);
        }
    }

    [TestCase]
    public void CorpseChunksExpelledFromCavityCellEngulferHaveYPositionAndVelocityZero()
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

        var engulferPosition = new Vector3(5.0f, Constants.CAVITY_CELL_Y_OFFSET, 5.0f);
        var engulfedPosition = new Vector3(10.0f, 0.0f, 10.0f);

        var engulfer = world.EntitySystem.Create(new WorldPosition(engulferPosition));

        SpawnHelpers.SpawnMicrobe(world, spawnEnvironment, species, engulfedPosition, false, GameteType.All);
        world.ProcessAll(0.1f);

        var microbe = new FirstEntityGrabber(new QueryDescription().WithAll<MicrobeSpeciesMember>(),
            world.EntitySystem).Found;
        ref var storage = ref microbe.Get<CompoundStorage>();

        const float storedAmountPerCompound = 100.0f;
        foreach (var compound in simulationParameters.GetCloudCompounds())
            storage.Compounds.Compounds[compound.ID] = storedAmountPerCompound;

        ref var organelles = ref microbe.Get<OrganelleContainer>();
        organelles.HexCount = 5;

        ref var engulfable = ref microbe.Get<Engulfable>();
        engulfable.HostileEngulfer = engulfer;
        engulfable.DigestedAmount = Constants.PARTIALLY_DIGESTED_THRESHOLD + 0.1f;
        engulfable.OriginalScale = Vector3.One;

        var spawnSystem = new DummySpawnSystem();
        engulfable.OnExpelledFromEngulfment(microbe, spawnSystem, world);
        world.ProcessAll(0.1f);

        var spawnedChunks = new List<Entity>();
        world.EntitySystem.Query(
            new QueryDescription().WithAll<CompoundVenter, CompoundStorage, WorldPosition, Physics>(),
            spawnedChunks.Add);

        AssertThat(spawnedChunks.Count).IsGreater(0);
        foreach (var chunk in spawnedChunks)
        {
            var chunkPosition = chunk.Get<WorldPosition>().Position;
            var chunkPhysics = chunk.Get<Physics>();
            AssertThat(chunkPosition.Y).IsEqualApprox(0.0f, 0.0001f);
            AssertThat(chunkPhysics.Velocity.Y).IsEqualApprox(0.0f, 0.0001f);
        }
    }
}
