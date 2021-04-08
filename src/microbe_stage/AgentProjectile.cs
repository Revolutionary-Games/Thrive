using System;
using Godot;
using Newtonsoft.Json;

/// <summary>
///   This is a shot agent projectile, does damage on hitting a cell of different species
/// </summary>
[JSONAlwaysDynamicType]
[SceneLoadedClass("res://src/microbe_stage/AgentProjectile.tscn", UsesEarlyResolve = false)]
public class AgentProjectile : RigidBody, ITimedLife
{
    [Export]
    public NodePath ParticlesPath;

    private Particles particles;

    [JsonProperty]
    public float FadeTimeRemaining { get; set; }
    public float TimeToLiveRemaining { get; set; }
    public float Amount { get; set; }
    public AgentProperties Properties { get; set; }
    public Node Emitter { get; set; }

    public void OnTimeOver()
    {
        Destroy();
    }

    public override void _Ready()
    {
        particles = GetNode<Particles>(ParticlesPath);

        AddCollisionExceptionWith(Emitter);
        Connect("body_entered", this, "OnBodyEntered");
    }

    public override void _Process(float delta)
    {
        if (FadeTimeRemaining < 0.00001)
            return;

        FadeTimeRemaining -= delta;
        if (FadeTimeRemaining < 0.00001)
            Destroy();
    }

    public void OnBodyEntered(Node body)
    {
        if (body is Microbe microbe)
        {
            if (microbe.Species != Properties.Species)
            {
                // If more stuff needs to be damaged we
                // could make an IAgentDamageable interface.
                microbe.Damage(Constants.OXYTOXY_DAMAGE, Properties.AgentType);
            }
        }

        particles.Emitting = false;

        // Timer that delays despawn of projectiles
        FadeTimeRemaining = Constants.PROJECTILE_DESPAWN_DELAY;
    }

    public void ApplyPropertiesFromSave(AgentProjectile projectile)
    {
        NodeGroupSaveHelper.CopyGroups(this, projectile);

        TimeToLiveRemaining = projectile.TimeToLiveRemaining;
        Amount = projectile.Amount;
        Properties = projectile.Properties;
        Transform = projectile.Transform;
        LinearVelocity = projectile.LinearVelocity;
        AngularVelocity = projectile.AngularVelocity;
    }

    private void Destroy()
    {
        // We should probably get some *POP* effect here.
        this.DetachAndQueueFree();
    }
}
