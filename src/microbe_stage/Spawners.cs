// This file contains all the different microbe stage spawner types
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

    public static ChunkSpawner MakeChunkSpawner(Biome.ChunkConfiguration chunkType,
        CompoundCloudSystem cloudSystem)
    {
        return new ChunkSpawner(chunkType, cloudSystem);
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
        CompoundCloudSystem cloudSystem, GameProperties currentGame)
    {
        var microbe = (Microbe)microbeScene.Instance();

        // The second parameter is (isPlayer), and we assume that if the
        // cell is not AI controlled it is the player's cell
        microbe.Init(cloudSystem, currentGame, !aiControlled);

        worldRoot.AddChild(microbe);
        microbe.Translation = location;

        microbe.AddToGroup(Constants.PROCESS_GROUP);

        if (aiControlled)
            microbe.AddToGroup(Constants.AI_GROUP);

        microbe.ApplySpecies(species);
        microbe.SetInitialCompounds();
        return microbe;
    }

    public static PackedScene LoadMicrobeScene()
    {
        return GD.Load<PackedScene>("res://src/microbe_stage/Microbe.tscn");
    }

    public static FloatingChunk SpawnChunk(Biome.ChunkConfiguration chunkType,
        Vector3 location, Node worldNode, PackedScene chunkScene,
        CompoundCloudSystem cloudSystem, Random random)
    {
        var chunk = (FloatingChunk)chunkScene.Instance();

        // Settings need to be applied before adding it to the scene
        chunk.GraphicsScene = chunkType.Meshes[random.Next(chunkType.Meshes.Count)].
            LoadedScene;

        // Pass on the chunk data
        chunk.Init(chunkType, cloudSystem);

        worldNode.AddChild(chunk);

        // Chunk is spawned with random rotation
        chunk.Transform = new Transform(new Quat(
                new Vector3(0, 1, 1).Normalized(), 2 * Mathf.Pi * (float)random.NextDouble()),
                location);

        chunk.Scale = new Vector3(chunkType.ChunkScale, chunkType.ChunkScale,
            chunkType.ChunkScale);

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
    public static void SpawnAgent(AgentProperties properties, float amount,
        float lifetime, Vector3 location, Vector3 direction,
        Node worldRoot, PackedScene agentScene, Node emitter)
    {
        var normalizedDirection = direction.Normalized();

        var agent = (AgentProjectile)agentScene.Instance();
        agent.Properties = properties;
        agent.Amount = amount;
        agent.TimeToLiveRemaining = lifetime;
        agent.Emitter = emitter;

        worldRoot.AddChild(agent);
        agent.Translation = location + (direction * 1.5f);

        // TODO: pass in this random from somewhere
        agent.Rotate(new Vector3(0, 1, 0), 2 * Mathf.Pi * (float)new Random().NextDouble());

        agent.ApplyCentralImpulse(normalizedDirection *
            Constants.AGENT_EMISSION_IMPULSE_STRENGTH);

        agent.AddToGroup(Constants.TIMED_GROUP);
    }

    public static PackedScene LoadAgentScene()
    {
        return GD.Load<PackedScene>("res://src/microbe_stage/AgentProjectile.tscn");
    }
}

/// <summary>
///   Spawns microbes of a specific species
/// </summary>
public class MicrobeSpawner : ISpawner
{
    private readonly PackedScene microbeScene;
    private readonly Species species;
    private readonly CompoundCloudSystem cloudSystem;
    private readonly GameProperties currentGame;

    public MicrobeSpawner(Species species, CompoundCloudSystem cloudSystem, GameProperties currentGame)
    {
        this.species = species ?? throw new ArgumentException("species is null");

        microbeScene = SpawnHelpers.LoadMicrobeScene();
        this.cloudSystem = cloudSystem;
        this.currentGame = currentGame;
    }

    public override List<ISpawned> Spawn(Node worldNode, Vector3 location)
    {
        var entities = new List<ISpawned>();

        // The true here is that this is AI controlled
        var microbe = SpawnHelpers.SpawnMicrobe(species, location, worldNode, microbeScene,
            true, cloudSystem, currentGame);

        entities.Add(microbe);
        return entities;
    }
}

/// <summary>
///   Spawns compound clouds of a certain type
/// </summary>
public class CompoundCloudSpawner : ISpawner
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

    public override List<ISpawned> Spawn(Node worldNode, Vector3 location)
    {
        SpawnHelpers.SpawnCloud(clouds, location, compound, amount);

        // We don't spawn entities
        return null;
    }
}

/// <summary>
///   Spawns chunks of a specific type
/// </summary>
public class ChunkSpawner : ISpawner
{
    private readonly PackedScene chunkScene;
    private readonly Biome.ChunkConfiguration chunkType;
    private readonly Random random = new Random();
    private readonly CompoundCloudSystem cloudSystem;

    public ChunkSpawner(Biome.ChunkConfiguration chunkType, CompoundCloudSystem cloudSystem)
    {
        this.chunkType = chunkType;
        this.cloudSystem = cloudSystem;
        chunkScene = SpawnHelpers.LoadChunkScene();
    }

    public override List<ISpawned> Spawn(Node worldNode, Vector3 location)
    {
        var entities = new List<ISpawned>();

        var chunk = SpawnHelpers.SpawnChunk(chunkType, location, worldNode, chunkScene,
            cloudSystem, random);

        entities.Add(chunk);
        return entities;
    }
}
