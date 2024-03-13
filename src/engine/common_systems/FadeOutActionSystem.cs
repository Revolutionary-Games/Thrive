namespace Systems;

using System;
using Components;
using DefaultEcs;
using DefaultEcs.System;
using DefaultEcs.Threading;
using Godot;
using World = DefaultEcs.World;

/// <summary>
///   Handles fading out animations on entities
/// </summary>
/// <remarks>
///   <para>
///     This is marked as reading the physics info as this does just some few simple physics actions on single
///     physics bodies. Same related to the spatial instance as this just can disable particles.
///   </para>
/// </remarks>
/// <remarks>
///   <para>
///     This is really marked as needing to run on the main thread because this modifies particle emitter emitting
///     setting of Godot.
///   </para>
/// </remarks>
/// <remarks>
///   <para>
///     TODO: due to the way this accesses things through a callback, all the attributes here might need to be
///     copied to any of the systems that may trigger the callback. Fortunately this doesn't do very extensive
///     writes so for now this is likely fine.
///   </para>
/// </remarks>
[With(typeof(FadeOutActions))]
[With(typeof(TimedLife))]
[ReadsComponent(typeof(WorldPosition))]
[ReadsComponent(typeof(SpatialInstance))]
[WritesToComponent(typeof(CompoundStorage))]
[WritesToComponent(typeof(Physics))]
[WritesToComponent(typeof(ManualPhysicsControl))]
[RuntimeCost(0.25f)]
[RunsOnMainThread]
public sealed class FadeOutActionSystem : AEntitySetSystem<float>
{
    private readonly IWorldSimulation worldSimulation;
    private readonly CompoundCloudSystem? compoundCloudSystem;

    public FadeOutActionSystem(IWorldSimulation worldSimulation, CompoundCloudSystem? compoundCloudSystem,
        World world, IParallelRunner runner) : base(world, runner, Constants.SYSTEM_EXTREME_ENTITIES_PER_THREAD)
    {
        this.worldSimulation = worldSimulation;
        this.compoundCloudSystem = compoundCloudSystem;
    }

    protected override void Update(float delta, in Entity entity)
    {
        ref var actions = ref entity.Get<FadeOutActions>();

        if (actions.CallbackRegistered)
            return;

        actions.CallbackRegistered = true;

        ref var timed = ref entity.Get<TimedLife>();

        // Register the callback which we use to trigger the actions for
        timed.CustomTimeOverCallback = PerformTimeOverActions;
    }

    private bool PerformTimeOverActions(Entity entity, ref TimedLife timedLife)
    {
        // This callback is triggered after this system has run already so when debugging component access, this
        // needs slight modification
        if (GenerateThreadedSystems.UseCheckedComponentAccess)
        {
            // To actually make this work for safety checking the following list *must* match the attributes on
            // this class
            // TODO: the following should actually be put as component accesses on any systems that may trigger
            // the callback. See the TODO comment on this class.
            ComponentAccessChecks.ReportAllowedAccessType<FadeOutActions>();
            ComponentAccessChecks.ReportAllowedAccessType<CompoundStorage>();
            ComponentAccessChecks.ReportAllowedAccessType<WorldPosition>();
            ComponentAccessChecks.ReportAllowedAccessType<SpatialInstance>();
            ComponentAccessChecks.ReportAllowedAccessType<Physics>();
            ComponentAccessChecks.ReportAllowedAccessType<ManualPhysicsControl>();
        }

        try
        {
            ref var actions = ref entity.Get<FadeOutActions>();

            if (actions.FadeTime <= 0)
            {
                // For now this indicates bad data but in the future we might get some more custom "time over"
                // actions
                GD.PrintErr("Custom fade out actions fade time is zero");
                return true;
            }

            timedLife.FadeTimeRemaining = actions.FadeTime;

            if (actions.DisableCollisions)
                PerformPhysicsOperations(entity, actions.DisableCollisions);

            if (actions.RemoveVelocity || actions.RemoveAngularVelocity)
            {
                PerformManualPhysicsControlOperations(entity, actions.RemoveVelocity,
                    actions.RemoveAngularVelocity);
            }

            if (actions.DisableParticles)
                DisableParticleEmission(entity);

            if (actions.UsesMicrobialDissolveEffect)
            {
                entity.StartDissolveAnimation(worldSimulation, true, false);
            }

            // Fade actions can be used without a cloud simulation
            if (actions.VentCompounds && compoundCloudSystem != null)
            {
                if (entity.Has<CompoundStorage>() && entity.Has<WorldPosition>())
                {
                    ref var position = ref entity.Get<WorldPosition>();
                    ref var storage = ref entity.Get<CompoundStorage>();

                    storage.VentAllCompounds(position.Position, compoundCloudSystem);
                }
                else
                {
                    GD.PrintErr("Cannot vent compounds on fade as entity has no compound storage " +
                        "(or world position is missing)");
                }
            }

            // Fade started, don't destroy yet
            return false;
        }
        catch (Exception e)
        {
            GD.PrintErr("Failed to perform custom fade out actions on timer over: ", e);
            return true;
        }
    }

    private void PerformPhysicsOperations(Entity entity, bool disableCollisions)
    {
        try
        {
            ref var physics = ref entity.Get<Physics>();

            physics.SetCollisionDisableState(disableCollisions);
        }
        catch (Exception e)
        {
            GD.PrintErr($"Cannot apply all fade out actions due to missing {nameof(Physics)} " +
                "component on entity: ", e);
        }
    }

    private void PerformManualPhysicsControlOperations(Entity entity, bool removeVelocity,
        bool removeAngularVelocity)
    {
        try
        {
            ref var physicsControl = ref entity.Get<ManualPhysicsControl>();

            physicsControl.RemoveVelocity = removeVelocity;
            physicsControl.RemoveAngularVelocity = removeAngularVelocity;

            physicsControl.PhysicsApplied = false;
        }
        catch (Exception e)
        {
            GD.PrintErr($"Cannot apply all fade out actions due to missing {nameof(ManualPhysicsControl)} " +
                "component on entity: ", e);
        }
    }

    private void DisableParticleEmission(Entity entity)
    {
        try
        {
            ref var spatial = ref entity.Get<SpatialInstance>();

            // TODO: also check for CPU particles?
            var particles = spatial.GraphicalInstance as GpuParticles3D;

            if (particles == null)
            {
                // Allow one level of indirection node
                if (spatial.GraphicalInstance?.GetChildCount() > 0)
                {
                    particles = spatial.GraphicalInstance.GetChild(0) as GpuParticles3D;
                }

                if (particles == null)
                {
                    throw new NullReferenceException(
                        $"Graphical instance ({spatial.GraphicalInstance}) casted as particles is null");
                }
            }

            particles.Emitting = false;

            // TODO: do we need a feature to automatically read the particle lifetime here and then adjust the
            // fade out time accordingly?
            // particleFadeTimer = particles.Lifetime;
        }
        catch (Exception e)
        {
            GD.PrintErr($"Cannot apply all fade out actions due to missing {nameof(SpatialInstance)} " +
                "or the visuals not being particles: ", e);
        }
    }
}
