using Godot;

/// <summary>
///   Handles floating chunks emitting compounds and dissolving. This is centralized to be able to apply the max chunks
///   cap.
/// </summary>
public class FloatingChunkSystem
{
    private readonly Node worldRoot;

    private readonly CompoundCloudSystem clouds;

    public FloatingChunkSystem(Node worldRoot, CompoundCloudSystem cloudSystem)
    {
        this.worldRoot = worldRoot;
        clouds = cloudSystem;
    }

    public void Process(float delta)
    {
        // https://github.com/Revolutionary-Games/Thrive/issues/1976
        if (delta <= 0)
            return;

        foreach (var chunk in worldRoot.GetChildrenToProcess<FloatingChunk>(Constants.AI_TAG_CHUNK))
        {
            chunk.ProcessChunk(delta, clouds);
        }
    }
}
