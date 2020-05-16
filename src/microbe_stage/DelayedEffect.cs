using System;
using Godot;

public class DelayedEffect : Node, ITimedLife
{
    public Action Effect = () => { };

    public float TimeToLiveRemaining { get; set; } = 0;

    public void OnTimeOver() => DoEffect();

    private void DoEffect()
    {
        Effect();
        QueueFree();
    }
}
