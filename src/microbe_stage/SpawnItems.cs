using Godot;
using System;
using System.Collections.Generic;

// SpawnItems contains the data for one spawn.
// These items are added to and drawn from the spawnItemsBag

abstract class SpawnItem
{
    abstract public List<ISpawned> Spawn(Vector3 location);
    abstract public int GetSpawnRadius();
    abstract public float GetMinSpawnRadius();
}

// Cloud to be spawned
class CloudItem : SpawnItem
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

    public override int GetSpawnRadius(){
        return cloudSpawner.SpawnRadius;
    }

    public override float GetMinSpawnRadius()
    {
        return cloudSpawner.MinSpawnRadius;
    }
}

// Chunk to be spawned
class ChunkItem : SpawnItem
{
    ChunkSpawner chunkSpawner;
    ChunkConfiguration chunkType;
    Node worldNode;

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
}

// Microbe to be spawned
class MicrobeItem : SpawnItem
{
    MicrobeSpawner microbeSpawner;
    MicrobeSpecies species;
    Node worldNode;
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
}