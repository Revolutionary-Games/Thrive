namespace Systems;

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using Arch.Core;
using Arch.Core.Extensions;
using Arch.System;
using Components;
using Godot;
using World = Arch.Core.World;

/// <summary>
///   Handles fading out animations on entities. When an entity's <see cref="TimedLife"/> expires, the registered
///   callback enqueues the entity, and this system applies the fade-out effects (physics, particles, dissolve,
///   compound venting) during its own update on the main thread.
/// </summary>
/// <remarks>
///   <para>
///     This is marked as reading the physics info as this does just some few simple physics actions on single
///     physics bodies. Same related to the spatial instance as this just can disable particles.
///   </para>
/// </remarks>
[ReadsComponent(typeof(WorldPosition))]
[ReadsComponent(typeof(SpatialInstance))]
[WritesToComponent(typeof(CompoundStorage))]
[WritesToComponent(typeof(Physics))]
[WritesToComponent(typeof(ManualPhysicsControl))]
[RunsAfter(typeof(TimedLifeSystem))]
[RuntimeCost(0.25f)]
[RunsOnMainThread]
public partial class FadeOutActionSystem : BaseSystem<World, float>
{
    private readonly IWorldSimulation worldSimulation;
    private readonly CompoundCloudSystem? compoundCloudSystem;

    private readonly TimedLife.OnTimeOver cachedDelegate;

    private readonly Lock queueLock = new();
    private Queue<Entity> pendingFadeOutEntities = new();
    private Queue<Entity> processingEntities = new();

    // TODO: Constants.SYSTEM_EXTREME_ENTITIES_PER_THREAD
    public FadeOutActionSystem(IWorldSimulation worldSimulation, CompoundCloudSystem? compoundCloudSystem,
        World world) : base(world)
    {
        this.worldSimulation = worldSimulation;
        this.compoundCloudSystem = compoundCloudSystem;

        cachedDelegate = PerformTimeOverActions;
    }

    public override void BeforeUpdate(in float delta)
    {
        base.BeforeUpdate(in delta);

        lock (queueLock)
        {
            (pendingFadeOutEntities, processingEntities) = (processingEntities, pendingFadeOutEntities);
        }

        while (processingEntities.Count > 0)
        {
            ApplyFadeOutActions(processingEntities.Dequeue());
        }
    }

    [Query]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void Update(ref FadeOutActions actions, ref TimedLife timed)
    {
        if (actions.CallbackRegistered)
            return;

        actions.CallbackRegistered = true;

        // Set up the callback which ensures the main processing happens in our context
        timed.CustomTimeOverUserData1 = actions.FadeTime;
        timed.CustomTimeOverCallback = cachedDelegate;
    }

    private bool PerformTimeOverActions(Entity entity, ref TimedLife timedLife)
    {
        if (timedLife.CustomTimeOverUserData1 <= 0)
        {
            GD.PrintErr("Custom fade out actions fade time is zero, can't use deferred custom callback");
            return true;
        }

        timedLife.FadeTimeRemaining = timedLife.CustomTimeOverUserData1;
        timedLife.FadeTimeRemainingSet = true;

        lock (queueLock)
        {
            pendingFadeOutEntities.Enqueue(entity);
        }

        return false;
    }

    private void ApplyFadeOutActions(Entity entity)
    {
        if (!entity.IsAliveAndHas<FadeOutActions>())
        {
            GD.PrintErr("Queued entity has no fade out actions: ", entity);
            return;
        }

        ref var actions = ref entity.Get<FadeOutActions>();

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
