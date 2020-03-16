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
    public static MicrobeSpawner MakeMicrobeSpawner(Species species)
    {
        return new MicrobeSpawner(species);
    }

    public static ChunkSpawner MakeChunkSpawner(Biome.ChunkConfiguration chunkType)
    {
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
        Node worldRoot, PackedScene microbeScene, bool aiControlled)
    {
        var microbe = (Microbe)microbeScene.Instance();

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
}

/// <summary>
///   Spawns microbes of a specific species
/// </summary>
public class MicrobeSpawner : ISpawner
{
    private readonly PackedScene microbeScene;
    private readonly Species species;

    public MicrobeSpawner(Species species)
    {
        this.species = species ?? throw new ArgumentException("species is null");

        microbeScene = SpawnHelpers.LoadMicrobeScene();
    }

    public override List<ISpawned> Spawn(Node worldNode, Vector3 location)
    {
        var entities = new List<ISpawned>();

        var microbe = SpawnHelpers.SpawnMicrobe(species, location, worldNode, microbeScene, true);

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

    public ChunkSpawner(Biome.ChunkConfiguration chunkType)
    {
        this.chunkType = chunkType;
        chunkScene = GD.Load<PackedScene>("res://src/microbe_stage/FloatingChunk.tscn");
    }

    public override List<ISpawned> Spawn(Node worldNode, Vector3 location)
    {
        var entities = new List<ISpawned>();

        var chunk = (FloatingChunk)chunkScene.Instance();

        // Settings need to be applied before adding it to the scene
        chunk.GraphicsScene = chunkType.Meshes[random.Next(chunkType.Meshes.Count)].
            LoadedScene;

        worldNode.AddChild(chunk);
        chunk.Translation = location;
        entities.Add(chunk);

        return entities;
    }
}
