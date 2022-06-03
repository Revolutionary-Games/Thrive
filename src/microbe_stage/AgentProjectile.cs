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
    private Particles particles = null!;

    public float TimeToLiveRemaining { get; set; }
    public float Amount { get; set; }
    public AgentProperties? Properties { get; set; }
    public EntityReference<IEntity> Emitter { get; set; } = new();

    [JsonProperty]
    private float? FadeTimeRemaining { get; set; }

    public void OnTimeOver()
    {
        if (FadeTimeRemaining == null)
            BeginDestroy();
    }

    public override void _Ready()
    {
        if (Properties == null)
            throw new InvalidOperationException($"{nameof(Properties)} is required");

        particles = GetNode<Particles>("Particles");

        var emitterNode = Emitter.Value?.EntityNode;

        if (emitterNode != null)
            AddCollisionExceptionWith(emitterNode);

        Connect("body_shape_entered", this, nameof(OnContactBegin));
    }

    public override void _Process(float delta)
    {
        if (FadeTimeRemaining == null)
            return;

        FadeTimeRemaining -= delta;
        if (FadeTimeRemaining <= 0)
            Destroy();
    }

    private void OnContactBegin(int bodyID, Node body, int bodyShape, int localShape)
    {
        _ = bodyID;
        _ = localShape;

        if (body is Microbe microbe)
        {
            if (microbe.Species != Properties!.Species)
            {
                // If more stuff needs to be damaged we could make an IAgentDamageable interface.
                var target = microbe.GetMicrobeFromShape(bodyShape);

                if (target != null)
                {
                    Invoke.Instance.Perform(
                        () => target.Damage(Constants.OXYTOXY_DAMAGE * Amount, Properties.AgentType));
                }
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
