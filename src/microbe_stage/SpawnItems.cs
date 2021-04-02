using Godot;
using System;
using System.Collections.Generic;
using Newtonsoft.Json;

// SpawnItems contains the data for one spawn.
// These items are added to and drawn from the spawnItemsBag
[JsonConverter(typeof(SpawnItemConverter))]
abstract class SpawnItem
{
    public string spawnType = "";
    abstract public List<ISpawned> Spawn(Vector3 location);
    abstract public int GetSpawnRadius();
    abstract public float GetMinSpawnRadius();
}

// Cloud to be spawned
class CloudItem : SpawnItem
{
    private CompoundCloudSpawner cloudSpawner;
    [JsonProperty]
    private Compound compound;
    [JsonProperty]
    private float amount;

    public const string NAME = "CLOUD";

    public CloudItem(CompoundCloudSpawner cloudSpawner, Compound compound, float amount)
    {
        spawnType = NAME;
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

    public void SetCloudSpawner(CompoundCloudSpawner cloudSpawner)
    {
        this.cloudSpawner = cloudSpawner;
    }
}

// Chunk to be spawned
class ChunkItem : SpawnItem
{
    ChunkSpawner chunkSpawner;
    [JsonProperty]
    ChunkConfiguration chunkType;
    Node worldNode;

    public const string NAME = "CHUNK";

    public ChunkItem(ChunkSpawner chunkSpawner, ChunkConfiguration chunkType, Node worldNode)
    {
        spawnType = NAME;
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
class MicrobeItem : SpawnItem
{
    MicrobeSpawner microbeSpawner;
    [JsonProperty]
    MicrobeSpecies species;
    Node worldNode;

    public const string NAME =  "MICROBE";
    public MicrobeItem(MicrobeSpawner microbeSpawner, MicrobeSpecies species, Node worldNode)
    {
        spawnType = NAME;
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