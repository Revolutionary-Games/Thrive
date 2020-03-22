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
        CompoundCloudSystem cloudSystem)
    {
        return new MicrobeSpawner(species, cloudSystem);
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
        CompoundCloudSystem cloudSystem)
    {
        var microbe = (Microbe)microbeScene.Instance();
        microbe.Init(cloudSystem);

        worldRoot.AddChild(microbe);
        microbe.Translation = location;

        microbe.AddToGroup("process");

        if (aiControlled)
            microbe.AddToGroup("ai");

        microbe.ApplySpecies(species);
        return microbe;
    }

    public static PackedScene LoadMicrobeScene()
    {
        return GD.Load<PackedScene>("res://src/microbe_stage/Microbe.tscn");
    }

    public static PackedScene LoadChunkScene()
    {
        return GD.Load<PackedScene>("res://src/microbe_stage/FloatingChunk.tscn");
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
                new Vector3(0, 1, 1), 2 * Mathf.Pi * (float)random.NextDouble()), location);

        chunk.Scale = new Vector3(chunkType.ChunkScale, chunkType.ChunkScale,
            chunkType.ChunkScale);

        return chunk;
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

    public MicrobeSpawner(Species species, CompoundCloudSystem cloudSystem)
    {
        this.species = species ?? throw new ArgumentException("species is null");

        microbeScene = SpawnHelpers.LoadMicrobeScene();
        this.cloudSystem = cloudSystem;
    }

    public override List<ISpawned> Spawn(Node worldNode, Vector3 location)
    {
        var entities = new List<ISpawned>();

        // The true here is that this is AI controlled
        var microbe = SpawnHelpers.SpawnMicrobe(species, location, worldNode, microbeScene,
            true, cloudSystem);

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
        clouds.AddCloud(compound, amount, location);

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
