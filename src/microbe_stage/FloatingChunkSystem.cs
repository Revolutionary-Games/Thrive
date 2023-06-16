using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Godot;

/// <summary>
///   Handles floating chunks emitting compounds and dissolving. This is centralized to be able to apply the max chunks
///   cap.
/// </summary>
public class FloatingChunkSystem
{
    private readonly IWorldSimulation worldSimulation;

    private readonly CompoundCloudSystem clouds;

    private Vector3 latestPlayerPosition = Vector3.Zero;

    public FloatingChunkSystem(IWorldSimulation worldSimulation, CompoundCloudSystem cloudSystem)
    {
        this.worldSimulation = worldSimulation;
        clouds = cloudSystem;
    }

    public void Process(float delta, Vector3? playerPosition)
    {
        if (playerPosition != null)
            latestPlayerPosition = playerPosition.Value;

        var comparePosition = latestPlayerPosition;

        var chunks = worldSimulation.Entities.OfType<FloatingChunk>().ToList();

        var findTooManyChunksTask = new Task<IEnumerable<FloatingChunk>>(() =>
        {
            int tooManyChunks =
                Math.Min(Constants.MAX_DESPAWNS_PER_FRAME, chunks.Count - Constants.FLOATING_CHUNK_MAX_COUNT);

            if (tooManyChunks < 1)
                return Array.Empty<FloatingChunk>();

            return chunks.OrderByDescending(c => c.Position.DistanceSquaredTo(comparePosition))
                .Take(tooManyChunks);
        });

        TaskExecutor.Instance.AddTask(findTooManyChunksTask);

        foreach (var chunk in chunks)
        {
            if (chunk.ProcessChunk(delta, clouds))
                worldSimulation.DestroyEntity(chunk);
        }

        findTooManyChunksTask.Wait();
        foreach (var toDespawn in findTooManyChunksTask.Result)
        {
            toDespawn.PopImmediately(clouds);
            worldSimulation.DestroyEntity(toDespawn);
        }
    }
}
