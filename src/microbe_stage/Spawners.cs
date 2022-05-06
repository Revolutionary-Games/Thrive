﻿// This file contains all the different microbe stage spawner types
// just so that they are in one place.

using System;
using System.Collections.Generic;
using Godot;

/// <summary>
///   Helpers for making different types of spawners
/// </summary>
public static class Spawners
{
    public static MicrobeSpawner MakeMicrobeSpawner(Species species,
        CompoundCloudSystem cloudSystem, GameProperties currentGame)
    {
        return new MicrobeSpawner(species, cloudSystem, currentGame);
    }

    public static ChunkSpawner MakeChunkSpawner(ChunkConfiguration chunkType)
    {
        foreach (var mesh in chunkType.Meshes)
        {
            if (mesh.LoadedScene == null)
                throw new ArgumentException("configured chunk spawner has a mesh that has no scene loaded");
        }

        return new ChunkSpawner(chunkType);
    }

    public static CompoundCloudSpawner MakeCompoundSpawner(Compound compound,
        CompoundCloudSystem clouds, float amount)
    {
        return new CompoundCloudSpawner(compound, clouds, amount);
    }
}

/// <summary>
///   Helper functions for spawning various things
/// </summary>
public static class SpawnHelpers
{
    public static Microbe SpawnMicrobe(Species species, Vector3 location,
        Node worldRoot, PackedScene microbeScene, bool aiControlled,
        CompoundCloudSystem cloudSystem, GameProperties currentGame, CellType? multicellularCellType = null)
    {
        var microbe = (Microbe)microbeScene.Instance();

        // The second parameter is (isPlayer), and we assume that if the
        // cell is not AI controlled it is the player's cell
        microbe.Init(cloudSystem, currentGame, !aiControlled);

        worldRoot.AddChild(microbe);
        microbe.Translation = location;

        microbe.AddToGroup(Constants.AI_TAG_MICROBE);
        microbe.AddToGroup(Constants.PROCESS_GROUP);

        if (aiControlled)
            microbe.AddToGroup(Constants.AI_GROUP);

        if (multicellularCellType != null)
        {
            microbe.ApplyMulticellularNonFirstCellSpecies((EarlyMulticellularSpecies)species, multicellularCellType);
        }
        else
        {
            microbe.ApplySpecies(species);
        }

        microbe.SetInitialCompounds();
        return microbe;
    }

    /// <summary>
    ///   Gives a random chance for a multicellular cell colony to spawn partially or fully grown
    /// </summary>
    /// <param name="microbe">The multicellular microbe</param>
    /// <param name="random">Random to use for the randomness</param>
    /// <exception cref="ArgumentException">If the microbe is not multicellular</exception>
    public static void GiveFullyGrownChanceForMulticellular(Microbe microbe, Random random)
    {
        if (!microbe.IsMulticellular)
            throw new ArgumentException("must be multicellular");

        // Chance to spawn fully grown or partially grown
        if (random.NextDouble() < Constants.CHANCE_MULTICELLULAR_SPAWNS_GROWN)
        {
            microbe.BecomeFullyGrownMulticellularColony();
        }
        else if (random.NextDouble() < Constants.CHANCE_MULTICELLULAR_SPAWNS_PARTLY_GROWN)
        {
            while (!microbe.IsFullyGrownMulticellular)
            {
                microbe.AddMulticellularGrowthCell();

                if (random.NextDouble() > Constants.CHANCE_MULTICELLULAR_PARTLY_GROWN_CELL_CHANCE)
                    break;
            }
        }
    }

    // TODO: this is likely a huge cause of lag. Would be nice to be able
    // to spawn these so that only one per tick is spawned.
    public static IEnumerable<Microbe> SpawnBacteriaColony(Species species, Vector3 location,
        Vector3 playerPosition, Node worldRoot, PackedScene microbeScene,
        CompoundCloudSystem cloudSystem, GameProperties currentGame, Random random)
    {
        var locationOffset = new Vector3(random.Next(1, 8), 0, random.Next(1, 8));

        // Three kinds of colonies are supported, line colonies and clump colonies and Networks
        if (random.Next(0, 5) < 2)
        {
            // Clump
            for (int i = 0; i < random.Next(Constants.MIN_BACTERIAL_COLONY_SIZE,
                     Constants.MAX_BACTERIAL_COLONY_SIZE + 1); i++)
            {
                // Skip spawning this microbe if it would spawn outside of the despawn radius
                if (IsInsideDespawnRadius(location + locationOffset, playerPosition))
                {
                    // Dont spawn them on top of each other because it
                    // causes them to bounce around and lag
                    yield return SpawnMicrobe(species, location + locationOffset, worldRoot, microbeScene, true,
                        cloudSystem, currentGame);
                }

                locationOffset = locationOffset + new Vector3(random.Next(-7, 8), 0, random.Next(-7, 8));
            }
        }
        else if (random.Next(0, 31) > 2)
        {
            // Line
            // Allow for many types of line
            // (I combined the lineX and lineZ here because they have the same values)
            var line = random.Next(-5, 6) + random.Next(-5, 6);

            for (int i = 0; i < random.Next(Constants.MIN_BACTERIAL_LINE_SIZE,
                     Constants.MAX_BACTERIAL_LINE_SIZE + 1); i++)
            {
                // Skip spawning this microbe if it would spawn outside of the despawn radius
                if (IsInsideDespawnRadius(location + locationOffset, playerPosition))
                {
                    // Dont spawn them on top of each other because it
                    // Causes them to bounce around and lag
                    yield return SpawnMicrobe(species, location + locationOffset, worldRoot, microbeScene, true,
                        cloudSystem, currentGame);
                }

                locationOffset = locationOffset + new Vector3(line + random.Next(-2, 3), 0, line + random.Next(-2, 3));
            }
        }
        else
        {
            // Network
            // Allows for "jungles of cyanobacteria"
            // Network is extremely rare

            // To prevent bacteria being spawned on top of each other
            var vertical = false;

            var colony = new ColonySpawnInfo(false, random, species, cloudSystem, currentGame, locationOffset,
                microbeScene, worldRoot);

            for (int i = 0; i < random.Next(Constants.MIN_BACTERIAL_COLONY_SIZE,
                     Constants.MAX_BACTERIAL_COLONY_SIZE + 1); i++)
            {
                if (random.Next(0, 5) < 2 && !colony.Horizontal)
                {
                    colony.Horizontal = true;
                    vertical = false;

                    foreach (var microbe in MicrobeColonySpawnHelper(colony, location, playerPosition))
                        yield return microbe;
                }
                else if (random.Next(0, 5) < 2 && !vertical)
                {
                    colony.Horizontal = false;
                    vertical = true;

                    foreach (var microbe in MicrobeColonySpawnHelper(colony, location, playerPosition))
                        yield return microbe;
                }
                else if (random.Next(0, 5) < 2 && !colony.Horizontal)
                {
                    colony.Horizontal = true;
                    vertical = false;

                    foreach (var microbe in MicrobeColonySpawnHelper(colony, location, playerPosition))
                        yield return microbe;
                }
                else if (random.Next(0, 5) < 2 && !vertical)
                {
                    colony.Horizontal = false;
                    vertical = true;

                    foreach (var microbe in MicrobeColonySpawnHelper(colony, location, playerPosition))
                        yield return microbe;
                }
                else
                {
                    // Diagonal
                    colony.Horizontal = false;
                    vertical = false;

                    foreach (var microbe in MicrobeColonySpawnHelper(colony, location, playerPosition))
                        yield return microbe;
                }
            }
        }
    }

    public static PackedScene LoadMicrobeScene()
    {
        return GD.Load<PackedScene>("res://src/microbe_stage/Microbe.tscn");
    }

    public static FloatingChunk SpawnChunk(ChunkConfiguration chunkType,
        Vector3 location, Node worldNode, PackedScene chunkScene, Random random)
    {
        var chunk = (FloatingChunk)chunkScene.Instance();

        // Settings need to be applied before adding it to the scene
        var selectedMesh = chunkType.Meshes.Random(random);
        chunk.GraphicsScene = selectedMesh.LoadedScene ??
            throw new Exception("Chunk scene has not been loaded even though it should be loaded here");
        chunk.ConvexPhysicsMesh = selectedMesh.LoadedConvexShape;

        if (chunk.GraphicsScene == null)
            throw new ArgumentException("couldn't find a graphics scene for a chunk");

        // Pass on the chunk data
        chunk.Init(chunkType, selectedMesh.SceneModelPath);
        chunk.UsesDespawnTimer = !chunkType.Dissolves;

        worldNode.AddChild(chunk);

        // Chunk is spawned with random rotation
        chunk.Transform = new Transform(new Quat(
                new Vector3(0, 1, 1).Normalized(), 2 * Mathf.Pi * (float)random.NextDouble()),
            location);

        chunk.GetNode<Spatial>("NodeToScale").Scale = new Vector3(chunkType.ChunkScale, chunkType.ChunkScale,
            chunkType.ChunkScale);

        chunk.AddToGroup(Constants.FLUID_EFFECT_GROUP);
        chunk.AddToGroup(Constants.AI_TAG_CHUNK);
        return chunk;
    }

    public static PackedScene LoadChunkScene()
    {
        return GD.Load<PackedScene>("res://src/microbe_stage/FloatingChunk.tscn");
    }

    public static void SpawnCloud(CompoundCloudSystem clouds, Vector3 location,
        Compound compound, float amount)
    {
        int resolution = Settings.Instance.CloudResolution;

        // This spreads out the cloud spawn a bit
        clouds.AddCloud(compound, amount, location + new Vector3(0 + resolution, 0, 0));
        clouds.AddCloud(compound, amount, location + new Vector3(0 - resolution, 0, 0));
        clouds.AddCloud(compound, amount, location + new Vector3(0, 0, 0 + resolution));
        clouds.AddCloud(compound, amount, location + new Vector3(0, 0, 0 - resolution));
        clouds.AddCloud(compound, amount, location + new Vector3(0, 0, 0));
    }

    /// <summary>
    ///   Spawns an agent projectile
    /// </summary>
    public static AgentProjectile SpawnAgent(AgentProperties properties, float amount,
        float lifetime, Vector3 location, Vector3 direction,
        Node worldRoot, PackedScene agentScene, IEntity emitter)
    {
        var normalizedDirection = direction.Normalized();

        var agent = (AgentProjectile)agentScene.Instance();
        agent.Properties = properties;
        agent.Amount = amount;
        agent.TimeToLiveRemaining = lifetime;
        agent.Emitter = new EntityReference<IEntity>(emitter);

        worldRoot.AddChild(agent);
        agent.Translation = location + (direction * 1.5f);
        var scaleValue = amount / Constants.MAXIMUM_AGENT_EMISSION_AMOUNT;
        agent.Scale = new Vector3(scaleValue, scaleValue, scaleValue);

        agent.ApplyCentralImpulse(normalizedDirection *
            Constants.AGENT_EMISSION_IMPULSE_STRENGTH);

        agent.AddToGroup(Constants.TIMED_GROUP);
        return agent;
    }

    public static PackedScene LoadAgentScene()
    {
        return GD.Load<PackedScene>("res://src/microbe_stage/AgentProjectile.tscn");
    }

    private static IEnumerable<Microbe> MicrobeColonySpawnHelper(ColonySpawnInfo colony, Vector3 location,
        Vector3 playerPosition)
    {
        for (int c = 0; c < colony.Random.Next(Constants.MIN_BACTERIAL_LINE_SIZE,
                 Constants.MAX_BACTERIAL_LINE_SIZE + 1); c++)
        {
            // Dont spawn them on top of each other because
            // It causes them to bounce around and lag
            // And add a little organicness to the look

            if (colony.Horizontal)
            {
                colony.LocationOffset.x += colony.Random.Next(5, 8);
                colony.LocationOffset.z += colony.Random.Next(-2, 3);
            }
            else
            {
                colony.LocationOffset.z += colony.Random.Next(5, 8);
                colony.LocationOffset.x += colony.Random.Next(-2, 3);
            }

            // Skip spawning this microbe if it would spawn outside of the despawn radius
            if (IsInsideDespawnRadius(location + colony.LocationOffset, playerPosition))
            {
                yield return SpawnMicrobe(colony.Species, location + colony.LocationOffset, colony.WorldRoot,
                    colony.MicrobeScene, true, colony.CloudSystem, colony.CurrentGame);
            }
        }
    }

    private static bool IsInsideDespawnRadius(Vector3 location, Vector3 playerPosition)
    {
        float distanceSquared = (playerPosition - location).LengthSquared();
        return distanceSquared < Constants.MICROBE_DESPAWN_RADIUS_SQUARED;
    }

    private class ColonySpawnInfo
    {
        public Species Species;
        public Node WorldRoot;
        public PackedScene MicrobeScene;
        public Vector3 LocationOffset;
        public bool Horizontal;
        public Random Random;
        public CompoundCloudSystem CloudSystem;
        public GameProperties CurrentGame;

        public ColonySpawnInfo(bool horizontal, Random random, Species species, CompoundCloudSystem cloudSystem,
            GameProperties currentGame, Vector3 locationOffset, PackedScene microbeScene, Node worldRoot)
        {
            Horizontal = horizontal;
            Random = random;
            Species = species;
            CloudSystem = cloudSystem;
            CurrentGame = currentGame;
            LocationOffset = locationOffset;
            MicrobeScene = microbeScene;
            WorldRoot = worldRoot;
        }
    }
}

/// <summary>
///   Spawns microbes of a specific species
/// </summary>
public class MicrobeSpawner : Spawner
{
    private readonly PackedScene microbeScene;
    private readonly CompoundCloudSystem cloudSystem;
    private readonly GameProperties currentGame;
    private readonly Random random;

    public MicrobeSpawner(Species species, CompoundCloudSystem cloudSystem, GameProperties currentGame)
    {
        Species = species ?? throw new ArgumentException("species is null");

        microbeScene = SpawnHelpers.LoadMicrobeScene();
        this.cloudSystem = cloudSystem;
        this.currentGame = currentGame;

        random = new Random();
    }

    public Species Species { get; }

    public override IEnumerable<ISpawned>? Spawn(Node worldNode, Vector3 location, Vector3 playerPosition)
    {
        // The true here is that this is AI controlled
        var first = SpawnHelpers.SpawnMicrobe(Species, location, worldNode, microbeScene, true, cloudSystem,
            currentGame);

        if (first.IsMulticellular)
        {
            SpawnHelpers.GiveFullyGrownChanceForMulticellular(first, random);
        }

        yield return first;

        ModLoader.ModInterface.TriggerOnMicrobeSpawned(first);

        // Just in case the is bacteria flag is not correct in a multicellular cell type, here's an extra safety check
        if (first.CellTypeProperties.IsBacteria && !first.IsMulticellular)
        {
            foreach (var colonyMember in SpawnHelpers.SpawnBacteriaColony(Species, location, playerPosition, worldNode,
                         microbeScene, cloudSystem, currentGame, random))
            {
                yield return colonyMember;

                ModLoader.ModInterface.TriggerOnMicrobeSpawned(colonyMember);
            }
        }
    }
}

/// <summary>
///   Spawns compound clouds of a certain type
/// </summary>
public class CompoundCloudSpawner : Spawner
{
    private readonly Compound compound;
    private readonly CompoundCloudSystem clouds;
    private readonly float amount;

    public CompoundCloudSpawner(Compound compound, CompoundCloudSystem clouds, float amount)
    {
        this.compound = compound ?? throw new ArgumentException("compound is null");
        this.clouds = clouds ?? throw new ArgumentException("clouds is null");
        this.amount = amount;
    }

    public override IEnumerable<ISpawned>? Spawn(Node worldNode, Vector3 location, Vector3 playerPosition)
    {
        SpawnHelpers.SpawnCloud(clouds, location, compound, amount);

        // We don't spawn entities
        return null;
    }
}

/// <summary>
///   Spawns chunks of a specific type
/// </summary>
public class ChunkSpawner : Spawner
{
    private readonly PackedScene chunkScene;
    private readonly ChunkConfiguration chunkType;
    private readonly Random random = new();

    public ChunkSpawner(ChunkConfiguration chunkType)
    {
        this.chunkType = chunkType;
        chunkScene = SpawnHelpers.LoadChunkScene();
    }

    public override IEnumerable<ISpawned>? Spawn(Node worldNode, Vector3 location, Vector3 playerPosition)
    {
        var chunk = SpawnHelpers.SpawnChunk(chunkType, location, worldNode, chunkScene,
            random);

        yield return chunk;

        ModLoader.ModInterface.TriggerOnChunkSpawned(chunk, true);
    }
}
