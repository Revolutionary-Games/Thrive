using System;
using System.Collections.Generic;
using Godot;
using Newtonsoft.Json;

// SpawnItems contains the data for one spawn.
// These items are added to and drawn from the spawnItemsBag
public abstract class SpawnItem
{
    public abstract List<ISpawned> Spawn(Vector3 location);
    public abstract int GetSpawnRadius();
    public abstract float GetMinSpawnRadius();
}

// Cloud to be spawned
public class CloudItem : SpawnItem
{
    private CompoundCloudSpawner cloudSpawner;
    private Compound compound;
    private float amount;

    public CloudItem(CompoundCloudSpawner cloudSpawner, Compound compound, float amount)
    {
        this.cloudSpawner = cloudSpawner;
        this.compound = compound;
        this.amount = amount;
    }

    public override List<ISpawned> Spawn(Vector3 location)
    {
        cloudSpawner.SpawnCloud(location, compound, amount);
        return null;
    }

    public override int GetSpawnRadius()
    {
        return cloudSpawner.SpawnRadius;
    }

    public override float GetMinSpawnRadius()
    {
        return cloudSpawner.MinSpawnRadius;
    }

    public void SetCloudSpawner(CompoundCloudSpawner cloudSpawner)
    {
        this.cloudSpawner = cloudSpawner;
    }
}

// Chunk to be spawned
public class ChunkItem : SpawnItem
{
    private ChunkSpawner chunkSpawner;
    private ChunkConfiguration chunkType;
    private Node worldNode;

    public ChunkItem(ChunkSpawner chunkSpawner, ChunkConfiguration chunkType, Node worldNode)
    {
        this.chunkSpawner = chunkSpawner;
        this.chunkType = chunkType;
        this.worldNode = worldNode;
    }

    public override List<ISpawned> Spawn(Vector3 location)
    {
        List<ISpawned> chunks = new List<ISpawned>();
        chunks.Add(chunkSpawner.Spawn(location, chunkType, worldNode));
        return chunks;
    }

    public override int GetSpawnRadius()
    {
        return chunkSpawner.SpawnRadius;
    }

    public override float GetMinSpawnRadius()
    {
        return chunkSpawner.MinSpawnRadius;
    }

    public void SetChunkSpawner(ChunkSpawner chunkSpawner)
    {
        this.chunkSpawner = chunkSpawner;
    }
}

// Microbe to be spawned
public class MicrobeItem : SpawnItem
{
    private MicrobeSpawner microbeSpawner;
    private MicrobeSpecies species;
    private Node worldNode;

    public MicrobeItem(MicrobeSpawner microbeSpawner, MicrobeSpecies species, Node worldNode)
    {
        this.microbeSpawner = microbeSpawner;
        this.species = species;
        this.worldNode = worldNode;
    }

    public override List<ISpawned> Spawn(Vector3 location)
    {
        return microbeSpawner.Spawn(worldNode, location, species);
    }

    public override int GetSpawnRadius()
    {
        return microbeSpawner.SpawnRadius;
    }

    public override float GetMinSpawnRadius()
    {
        return microbeSpawner.MinSpawnRadius;
    }

    public void SetMicrobeSpawner(MicrobeSpawner microbeSpawner)
    {
        this.microbeSpawner = microbeSpawner;
    }
}
