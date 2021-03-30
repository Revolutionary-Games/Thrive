using Godot;
using System;

// SpawnItems contains the data for one spawn.
// These items are added to and drawn from the spawnItemsBag

abstract class SpawnItem
{
    abstract public void Spawn(Vector3 location);
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

    public override void Spawn(Vector3 location)
    {
        cloudSpawner.SpawnCloud(location, compound, amount);
    }

    public Compound GetCompound()
    {
        return compound;
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

    public override void Spawn(Vector3 location)
    {
        chunkSpawner.SpawnChunk(location, chunkType, worldNode);
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

    public override void Spawn(Vector3 location)
    {
        microbeSpawner.Spawn(worldNode, location, species);
    }
}