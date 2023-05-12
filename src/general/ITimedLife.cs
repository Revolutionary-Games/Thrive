/// <summary>
///   All nodes that despawn after some time need to implement this.
/// </summary>
public interface ITimedLife
{
    public float TimeToLiveRemaining { get; set; }

    public void OnTimeOver();
}
