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
        foreach (var entity in worldRoot.GetChildrenToProcess<Node>(Constants.TIMED_GROUP))
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
        foreach (var entity in worldRoot.GetChildrenToProcess<Node>(Constants.TIMED_GROUP))
        {
            if (!entity.IsQueuedForDeletion())
                entity.DetachAndQueueFree();
        }
    }
}
