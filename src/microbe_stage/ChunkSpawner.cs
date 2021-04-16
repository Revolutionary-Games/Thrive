using System;
using System.Collections.Generic;
using Godot;

/// <summary>
///   Spawns chunks of a specific type
/// </summary>
public class ChunkSpawner
{
    private readonly PackedScene chunkScene;
    private readonly Random random = new Random();
    private readonly CompoundCloudSystem cloudSystem;

    public ChunkSpawner(CompoundCloudSystem cloudSystem)
    {
        this.cloudSystem = cloudSystem;
        chunkScene = LoadChunkScene();
    }

    public static PackedScene LoadChunkScene()
    {
        return GD.Load<PackedScene>("res://src/microbe_stage/FloatingChunk.tscn");
    }

    public static ISpawned SpawnChunk(ChunkConfiguration chunkType, Vector3 location, Node worldNode,
        PackedScene chunkScene, CompoundCloudSystem cloudSystem, Random random)
    {
        var chunk = (FloatingChunk)chunkScene.Instance();

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
        return chunk;
    }

    public ISpawned Spawn(Vector3 location, ChunkConfiguration chunkType, Node worldNode)
    {
        return SpawnChunk(chunkType, location, worldNode, chunkScene, cloudSystem, random);
    }
}
