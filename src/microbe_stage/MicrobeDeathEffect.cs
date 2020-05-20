using Godot;

public class MicrobeDeathEffect : Spatial, ITimedLife
{
    public float TimeToLiveRemaining { get; set; } = 5.0f;

    public void OnTimeOver() => QueueFree();
    public override void _Ready() => AddToGroup(Constants.TIMED_GROUP);
}
