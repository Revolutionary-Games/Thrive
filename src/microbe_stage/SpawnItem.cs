using System.Collections.Generic;
using Godot;

// SpawnItems contains the data for one spawn.
// These items are added to and drawn from the spawnItemsBag
public abstract class SpawnItem
{
    protected Vector3 position;

    public void SetSpawnPosition(Vector3 position)
    {
        this.position = position;
    }

    public abstract List<ISpawned> Spawn();
}

// Cloud to be spawned
public class CloudItem : SpawnItem
{
    private Compound compound;
    private float amount;
    private CompoundCloudSpawner cloudSpawner;

    public CloudItem(Compound compound, float amount)
    {
        this.compound = compound;
        this.amount = amount;
    }

    public void SetCloudSpawner(CompoundCloudSpawner cloudSpawner)
    {
        this.cloudSpawner = cloudSpawner;
    }

    public override List<ISpawned> Spawn()
    {
        cloudSpawner.SpawnCloud(position, compound, amount);
        return null;
    }
}

// Chunk to be spawned
public class ChunkItem : SpawnItem
{
    private ChunkConfiguration chunkType;

    private ChunkSpawner chunkSpawner;
    private Node worldNode;

    public ChunkItem(ChunkConfiguration chunkType)
    {
        this.chunkType = chunkType;
    }

    public void SetChunkSpawner(ChunkSpawner chunkSpawner, Node worldNode)
    {
        this.chunkSpawner = chunkSpawner;
        this.worldNode = worldNode;
    }

    public override List<ISpawned> Spawn()
    {
        List<ISpawned> chunks = new List<ISpawned>();
        chunks.Add(chunkSpawner.Spawn(position, chunkType, worldNode));
        return chunks;
    }
}

// Microbe to be spawned
public class MicrobeItem : SpawnItem
{
    private MicrobeSpecies species;
    private MicrobeSpawner microbeSpawner;
    private Node worldNode;
    public bool IsWanderer;

    public MicrobeItem(MicrobeSpecies species)
    {
        this.species = species;
    }

    public void SetMicrobeSpawner(MicrobeSpawner microbeSpawner, Node worldNode)
    {
        this.microbeSpawner = microbeSpawner;
        this.worldNode = worldNode;
    }

    public override List<ISpawned> Spawn()
    {
        return microbeSpawner.Spawn(worldNode, position, species, IsWanderer);
    }
}
