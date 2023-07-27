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
    private readonly Node worldRoot;

    private readonly CompoundCloudSystem clouds;

    private Vector3 latestPlayerPosition = Vector3.Zero;

    public FloatingChunkSystem(Node worldRoot, CompoundCloudSystem cloudSystem)
    {
        this.worldRoot = worldRoot;
        clouds = cloudSystem;
    }

    public void Process(float delta, Vector3? playerPosition)
    {
        if (playerPosition != null)
            latestPlayerPosition = playerPosition.Value;

        var chunks = worldRoot.GetChildrenToProcess<FloatingChunk>(Constants.AI_TAG_CHUNK).ToList();

        foreach (var chunk in chunks)
        {
            chunk.ProcessChunk(delta, clouds);
        }

        if (!NetworkManager.Instance.IsClient)
        {
            var findTooManyChunksTask = new Task<IEnumerable<FloatingChunk>>(() =>
            {
                int tooManyChunks =
                    Math.Min(Constants.MAX_DESPAWNS_PER_FRAME, chunks.Count - Constants.FLOATING_CHUNK_MAX_COUNT);

                if (tooManyChunks < 1)
                    return Array.Empty<FloatingChunk>();

                var comparePosition = latestPlayerPosition;

                return chunks.OrderByDescending(c => c.Translation.DistanceSquaredTo(comparePosition))
                    .Take(tooManyChunks);
            });

            TaskExecutor.Instance.AddTask(findTooManyChunksTask);

            findTooManyChunksTask.Wait();
            foreach (var toDespawn in findTooManyChunksTask.Result)
            {
                toDespawn.PopImmediately(clouds);
            }
        }
    }
}
