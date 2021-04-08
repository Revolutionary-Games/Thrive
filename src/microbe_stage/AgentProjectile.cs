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

    [JsonProperty]
    private float? FadeTimeRemaining { get; set; }

    public void OnTimeOver()
    {
        if (FadeTimeRemaining != null)
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

        BeginDestroy();
    }

    /// <summary>
    ///   Stops particle emission and destroys the object after 5 seconds.
    /// </summary>
    private void BeginDestroy()
    {
        particles.Emitting = false;

        // Timer that delays despawn of projectiles
        FadeTimeRemaining = Constants.PROJECTILE_DESPAWN_DELAY;
    }

    private void Destroy()
    {
        // We should probably get some *POP* effect here.
        this.DetachAndQueueFree();
    }
}
