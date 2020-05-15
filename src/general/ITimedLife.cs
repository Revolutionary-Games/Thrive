﻿/// <summary>
///   All nodes that despawn after some time need to implement this.
/// </summary>
public interface ITimedLife
{
    float TimeToLiveRemaining { get; set; }

    void OnTimeOver();
}
