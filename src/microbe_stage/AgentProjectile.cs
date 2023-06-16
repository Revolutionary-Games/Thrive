using System;
using Godot;
using Newtonsoft.Json;

/// <summary>
///   This is a shot agent projectile, does damage on hitting a cell of different species
/// </summary>
[JSONAlwaysDynamicType]

// TODO: reimplement inspectable
public class
    AgentProjectile : SimulatedPhysicsEntity, ISimulatedEntityWithDirectVisuals, ITimedLife /*, IInspectableEntity*/
{
    private static readonly Lazy<PackedScene> VisualsScene =
        new(() => GD.Load<PackedScene>("res://src/microbe_stage/AgentProjectile.tscn"));

#pragma warning disable CA2213
    private Particles particles = null!;
#pragma warning restore CA2213

    public float Amount { get; set; }
    public AgentProperties? Properties { get; set; }

    /// <summary>
    ///   Entity that emitted this, is used to ignore collisions with it. Note that this must be set before adding this
    ///   to a simulation.
    /// </summary>
    [JsonProperty]
    public EntityReference<SimulatedPhysicsEntity> Emitter { get; set; } = new();

    [JsonProperty]
    public float TimeToLiveRemaining { get; set; }

    [JsonProperty]
    public float? FadeTimeRemaining { get; set; }

    [JsonIgnore]
    public string ReadableName => Properties?.ToString() ?? TranslationServer.Translate("N_A");

    [JsonIgnore]
    public Spatial VisualNode => particles;

    [JsonProperty]
    public float VisualScale { get; set; } = 1;

    public override void OnAddedToSimulation(IWorldSimulation simulation)
    {
        if (simulation is not IWorldSimulationWithPhysics simulationWithPhysics)
            throw new ArgumentException("This can only be used in a world with physics");

        if (Properties == null)
            throw new InvalidOperationException($"{nameof(Properties)} is required");

        base.OnAddedToSimulation(simulation);

        var emitterBody = Emitter.Value?.Body;

        if (emitterBody != null)
        {
            DisableCollisionsWith(emitterBody);
        }

        RegisterCollisionCallback(OnContactBegin);

        particles = VisualsScene.Value.Instance<Particles>();

        particles.Scale = new Vector3(VisualScale, VisualScale, VisualScale);
    }

    public void OnTimeOver()
    {
        if (FadeTimeRemaining == null)
            BeginDestroy();
    }

    private void OnContactBegin(PhysicsBody physicsBody, int collidedSubShapeDataOurs, int bodyShape)
    {
        throw new NotImplementedException();
        /*_ = bodyID;
        _ = localShape;

        if (body is not Microbe microbe)
            return;

        if (microbe.Species == Properties!.Species)
            return;

        // If more stuff needs to be damaged we could make an IAgentDamageable interface.
        var target = microbe.GetMicrobeFromShape(bodyShape);

        if (target == null)
            return;

        Invoke.Instance.Perform(
            () => target.Damage(Constants.OXYTOXY_DAMAGE * Amount, Properties.AgentType));

        if (FadeTimeRemaining == null)
        {
            // We should probably get some *POP* effect here.
            BeginDestroy();
        }*/
    }

    /// <summary>
    ///   Stops particle emission and destroys the object after 5 seconds.
    /// </summary>
    private void BeginDestroy()
    {
        particles.Emitting = false;

        // Disable collisions and stop this entity
        DisableAllCollisions();
        SetVelocityToZero();

        // Timer that delays despawn of projectiles
        FadeTimeRemaining = Constants.PROJECTILE_DESPAWN_DELAY;
    }
}
