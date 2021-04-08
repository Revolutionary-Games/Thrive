using Godot;

/// <summary>
///   This is a shot agent projectile, does damage on hitting a cell of different species
/// </summary>
[JSONAlwaysDynamicType]
[SceneLoadedClass("res://src/microbe_stage/AgentProjectile.tscn", UsesEarlyResolve = false)]
public class AgentProjectile : RigidBody, ITimedLife
{
    [Export]
    public NodePath ParticlesPath;

    private Timer despawnTimer;
    private Particles particles;

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

        // Timer that delay despawn of projectiles
        despawnTimer = new Timer();
        despawnTimer.OneShot = true;
        despawnTimer.WaitTime = Constants.PROJECTILE_DESPAWN_DELAY;
        despawnTimer.Connect("timeout", this, "OnTimerTimeout");
        AddChild(despawnTimer);
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
        despawnTimer.Start();
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

    public void OnTimerTimeout()
    {
        Destroy();
    }

    private void Destroy()
    {
        // We should probably get some *POP* effect here.
        QueueFree();
    }
}
