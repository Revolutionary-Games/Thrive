using Godot;
using System;

public class DelayedEffect : Node, ITimedLife
{
    public float TimeToLiveRemaining { get; set; } = 0;

    public Action Effect = () => {};

    public void OnTimeOver() => DoEffect();

    private void DoEffect()
    {
        Effect();
        QueueFree();
    }
}
