using System;
using Godot;

/// <summary>
///   This is a shot agent projectile, does damage on hitting a cell of different species
/// </summary>
public class AgentProjectile : RigidBody, ITimedLife
{
    public float TimeToLiveRemaining { get; set; }
    public float Amount { get; set; }
    public AgentProperties Properties { get; set; }

    public override void _Ready()
    {
        // TODO: physics callbacks
    }

    public override void _Process(float delta)
    {
    }
}
