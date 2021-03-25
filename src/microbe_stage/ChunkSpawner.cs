using System;
using System.Collections.Generic;
using Godot;

/// <summary>
///   Spawns chunks of a specific type
/// </summary>
public class ChunkSpawner : Spawner
{
    private readonly PackedScene chunkScene;
    private readonly Random random = new Random();
    private readonly CompoundCloudSystem cloudSystem;

    private Dictionary<ChunkConfiguration, int> ChunkCounts = new Dictionary<ChunkConfiguration, int>();

    public ChunkSpawner(CompoundCloudSystem cloudSystem, int spawnRadius)
    {
        this.cloudSystem = cloudSystem;
        this.SetSpawnRadius(spawnRadius);
        chunkScene = ChunkSpawner.LoadChunkScene();
    }

    public void AddChunk(ChunkConfiguration chunk, int numOfItems)
    {
         foreach (var mesh in chunk.Meshes)
        {
            if (mesh.LoadedScene == null)
                throw new ArgumentException("configured chunk spawner has a mesh that has no scene loaded");
        }
        ChunkCounts.Add(chunk,numOfItems);
    }

    public void ClearChunks()
    {
        ChunkCounts.Clear();
    }

    public ChunkConfiguration[] GetChunks()
    {
        ChunkConfiguration[] chunks = new ChunkConfiguration[ChunkCounts.Keys.Count];
        ChunkCounts.Keys.CopyTo(chunks, 0);
        return chunks;
    }

    public int getChunkCount(ChunkConfiguration chunk)
    {
        return ChunkCounts[chunk];
    }

    public void SpawnChunk(Vector3 location, ChunkConfiguration chunkType, Node worldNode)
    {
        var chunk = (FloatingChunk)chunkScene.Instance();

        GD.Print("Spawning a Chunk");

        // Settings need to be applied before adding it to the scene
        var selectedMesh = chunkType.Meshes.Random(random);
        chunk.GraphicsScene = selectedMesh.LoadedScene;
        chunk.ConvexPhysicsMesh = selectedMesh.LoadedConvexShape;

        if (chunk.GraphicsScene == null)
            throw new ArgumentException("couldn't find a graphics scene for a chunk");

        // Pass on the chunk data
        chunk.Init(chunkType, cloudSystem, selectedMesh.SceneModelPath);
        chunk.UsesDespawnTimer = !chunkType.Dissolves;

        worldNode.AddChild(chunk);

        // Chunk is spawned with random rotation
        chunk.Transform = new Transform(new Quat(
                new Vector3(0, 1, 1).Normalized(), 2 * Mathf.Pi * (float)random.NextDouble()),
            location);

        chunk.GetNode<Spatial>("NodeToScale").Scale = new Vector3(chunkType.ChunkScale, chunkType.ChunkScale,
            chunkType.ChunkScale);

        chunk.AddToGroup(Constants.FLUID_EFFECT_GROUP);
        chunk.AddToGroup(Constants.AI_TAG_CHUNK);
    }

    //Temp untill I can work on microbe spawning.
    public static void SpawnChunk(ChunkConfiguration chunkType, Vector3 location, Node worldNode,
                PackedScene chunkScene, CompoundCloudSystem cloudSystem, Random random)
    {
        GD.Print("--SPAWNING CHUNK--");
        //find a better place for this error check
         foreach (var mesh in chunkType.Meshes)
        {
            if (mesh.LoadedScene == null)
                throw new ArgumentException("configured chunk spawner has a mesh that has no scene loaded");
        }

        var chunk = (FloatingChunk)chunkScene.Instance();

        GD.Print("Spawning a Chunk");

        // Settings need to be applied before adding it to the scene
        var selectedMesh = chunkType.Meshes.Random(random);
        chunk.GraphicsScene = selectedMesh.LoadedScene;
        chunk.ConvexPhysicsMesh = selectedMesh.LoadedConvexShape;

        if (chunk.GraphicsScene == null)
            throw new ArgumentException("couldn't find a graphics scene for a chunk");

        // Pass on the chunk data
        chunk.Init(chunkType, cloudSystem, selectedMesh.SceneModelPath);
        chunk.UsesDespawnTimer = !chunkType.Dissolves;

        worldNode.AddChild(chunk);

        // Chunk is spawned with random rotation
        chunk.Transform = new Transform(new Quat(
                new Vector3(0, 1, 1).Normalized(), 2 * Mathf.Pi * (float)random.NextDouble()),
            location);

        chunk.GetNode<Spatial>("NodeToScale").Scale = new Vector3(chunkType.ChunkScale, chunkType.ChunkScale,
            chunkType.ChunkScale);

        chunk.AddToGroup(Constants.FLUID_EFFECT_GROUP);
        chunk.AddToGroup(Constants.AI_TAG_CHUNK);
    }
    public static PackedScene LoadChunkScene()
    {
        return GD.Load<PackedScene>("res://src/microbe_stage/FloatingChunk.tscn");
    }
}
