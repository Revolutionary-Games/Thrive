using Godot;
using Newtonsoft.Json;

/// <summary>
///   This is a shot agent projectile, does damage on hitting a cell of different species
/// </summary>
[JSONAlwaysDynamicType]
[SceneLoadedClass("res://src/microbe_stage/AgentProjectile.tscn", UsesEarlyResolve = false)]
public class AgentProjectile : RigidBody, ITimedLife
{
    private Particles particles;

    public float TimeToLiveRemaining { get; set; }
    public float Amount { get; set; }
    public AgentProperties Properties { get; set; }
    public Node Emitter { get; set; }

    public static AgentProjectile SpawnAgent(AgentProperties properties, float amount,
        float lifetime, Vector3 location, Vector3 direction,
        Node worldRoot, PackedScene agentScene, Node emitter)
    {
        var normalizedDirection = direction.Normalized();

        var agent = (AgentProjectile)agentScene.Instance();
        agent.Properties = properties;
        agent.Amount = amount;
        agent.TimeToLiveRemaining = lifetime;
        agent.Emitter = emitter;

        worldRoot.AddChild(agent);
        agent.Translation = location + (direction * 1.5f);

        agent.ApplyCentralImpulse(normalizedDirection *
            Constants.AGENT_EMISSION_IMPULSE_STRENGTH);

        agent.AddToGroup(Constants.TIMED_GROUP);
        return agent;
    }

    public static PackedScene LoadAgentScene()
    {
        return GD.Load<PackedScene>("res://src/microbe_stage/AgentProjectile.tscn");
    }

    [JsonProperty]
    private float? FadeTimeRemaining { get; set; }
    public void OnTimeOver()
    {
        if (FadeTimeRemaining == null)
            BeginDestroy();
    }

    public override void _Ready()
    {
        particles = GetNode<Particles>("Particles");

        AddCollisionExceptionWith(Emitter);
        Connect("body_entered", this, "OnBodyEntered");
    }

    public override void _Process(float delta)
    {
        if (FadeTimeRemaining == null)
            return;

        FadeTimeRemaining -= delta;
        if (FadeTimeRemaining <= 0)
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

        if (FadeTimeRemaining == null)
        {
            // We should probably get some *POP* effect here.
            BeginDestroy();
        }
    }

    /// <summary>
    ///   Stops particle emission and destroys the object after 5 seconds.
    /// </summary>
    private void BeginDestroy()
    {
        particles.Emitting = false;

        // Disable collisions and stop this entity
        // This isn't the recommended way (disabling the collision shape), but as we don't have a reference to that here
        // this should also work for disabling the collisions
        CollisionLayer = 0;
        CollisionMask = 0;
        LinearVelocity = Vector3.Zero;

        // Timer that delays despawn of projectiles
        FadeTimeRemaining = Constants.PROJECTILE_DESPAWN_DELAY;
    }

    private void Destroy()
    {
        this.DetachAndQueueFree();
    }
}
