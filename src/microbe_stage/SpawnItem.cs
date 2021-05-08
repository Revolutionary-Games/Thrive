using System.Collections.Generic;
using Godot;

/// <summary>
/// SpawnItems contains the data for one spawn.
/// These items are added to and drawn from the spawnItemsBag
/// </summary>
public abstract class SpawnItem
{
    public Vector3 Position { get; set; }

    public void SetSpawnPosition(Vector3 position)
    {
        Position = position;
    }

    public abstract IEnumerable<ISpawned> Spawn();
}

/// <summary>
/// Cloud to be spawned in a Spawn Event
/// </summary>
public class CloudItem : SpawnItem
{
    private Compound compound;
    private float amount;

    public CloudItem(Compound compound, float amount, CompoundCloudSpawner cloudSpawner)
    {
        this.compound = compound;
        this.amount = amount;
        CloudSpawner = cloudSpawner;
    }

    public CompoundCloudSpawner CloudSpawner { get; set; }

    public override IEnumerable<ISpawned> Spawn()
    {
        CloudSpawner.Spawn(Position, compound, amount);
        return null;
    }
}

/// <summary>
/// Chunk to be Spawned in a Spawn Event
/// </summary>
public class ChunkItem : SpawnItem
{
    private ChunkConfiguration chunkType;

    public ChunkItem(ChunkConfiguration chunkType, ChunkSpawner chunkSpawner)
    {
        this.chunkType = chunkType;
        ChunkSpawner = chunkSpawner;
    }

    public ChunkSpawner ChunkSpawner { get; set; }

    public Node WorldNode { get; set; }

    public override IEnumerable<ISpawned> Spawn()
    {
       yield return ChunkSpawner.Spawn(Position, chunkType, WorldNode);
    }
}

/// <summary>
/// Microbe to be spawned in a Spawn Event or as a wandering microbe.
/// </summary>
public class MicrobeItem : SpawnItem
{
    public bool IsWanderer;
    private MicrobeSpecies species;

    public MicrobeItem(MicrobeSpecies species, MicrobeSpawner microbeSpawner)
    {
        this.species = species;
        MicrobeSpawner = microbeSpawner;
    }

    public MicrobeSpawner MicrobeSpawner { get; set; }
    public Node WorldNode { get; set; }

    public override IEnumerable<ISpawned> Spawn()
    {
        return MicrobeSpawner.Spawn(WorldNode, Position, species, IsWanderer);
    }
}
