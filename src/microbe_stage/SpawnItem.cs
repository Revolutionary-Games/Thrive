using System.Collections.Generic;
using Godot;

// SpawnItems contains the data for one spawn.
// These items are added to and drawn from the spawnItemsBag
public abstract class SpawnItem
{
    public Vector3 Position { get; set; }

    public void SetSpawnPosition(Vector3 position)
    {
        Position = position;
    }

    public abstract List<ISpawned> Spawn();
}

// Cloud to be spawned
public class CloudItem : SpawnItem
{
    private Compound compound;
    private float amount;

    public CloudItem(Compound compound, float amount)
    {
        this.compound = compound;
        this.amount = amount;
    }

    public CompoundCloudSpawner CloudSpawner { get; set; }

    public override List<ISpawned> Spawn()
    {
        CloudSpawner.Spawn(Position, compound, amount);
        return null;
    }
}

// Chunk to be spawned
public class ChunkItem : SpawnItem
{
    private ChunkConfiguration chunkType;

    public ChunkItem(ChunkConfiguration chunkType)
    {
        this.chunkType = chunkType;
    }

    public ChunkSpawner ChunkSpawner { get; set; }

    public Node WorldNode { get; set; }

    public override List<ISpawned> Spawn()
    {
        List<ISpawned> chunks = new List<ISpawned>();
        chunks.Add(ChunkSpawner.Spawn(Position, chunkType, WorldNode));
        return chunks;
    }
}

// Microbe to be spawned
public class MicrobeItem : SpawnItem
{
    public bool IsWanderer;
    private MicrobeSpecies species;

    public MicrobeItem(MicrobeSpecies species)
    {
        this.species = species;
    }

    public MicrobeSpawner MicrobeSpawner { get; set; }
    public Node WorldNode { get; set; }

    public override List<ISpawned> Spawn()
    {
        return MicrobeSpawner.Spawn(WorldNode, Position, species, IsWanderer);
    }
}
