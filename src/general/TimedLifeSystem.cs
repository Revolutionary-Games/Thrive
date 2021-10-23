using Godot;

/// <summary>
///   System that deletes nodes that are in the timed group after their lifespan expires.
/// </summary>
public class TimedLifeSystem
{
    private readonly Node worldRoot;

    public TimedLifeSystem(Node worldRoot)
    {
        this.worldRoot = worldRoot;
    }

    public void Process(float delta)
    {
        var nodes = worldRoot.GetTree().GetNodesInGroup(Constants.TIMED_GROUP);

        foreach (Node entity in nodes)
        {
            var timed = entity as ITimedLife;

            if (timed == null)
            {
                GD.PrintErr("A node has been put in the timed group " +
                    "but it isn't derived from ITimedLife");
                continue;
            }

            timed.TimeToLiveRemaining -= delta;

            if (timed.TimeToLiveRemaining <= 0.0f)
            {
                timed.OnTimeOver();
            }
        }
    }

    /// <summary>
    ///   Despawns all timed entities
    /// </summary>
    public void DespawnAll()
    {
        var spawnedEntities = worldRoot.GetTree().GetNodesInGroup(Constants.TIMED_GROUP);

        foreach (Node entity in spawnedEntities)
        {
            if (!entity.IsQueuedForDeletion())
                entity.DetachAndQueueFree();
        }
    }
}
