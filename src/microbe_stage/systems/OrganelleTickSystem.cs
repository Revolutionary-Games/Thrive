﻿namespace Systems;

using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using Arch.Core;
using Arch.System;
using Components;
using Godot;
using World = Arch.Core.World;

/// <summary>
///   Handles calling <see cref="IOrganelleComponent.UpdateAsync"/> and other tick methods on organelles each game
///   update
/// </summary>
/// <remarks>
///   <para>
///     This runs after <see cref="MicrobeMovementSystem"/> as this mostly deals with animating movement
///     organelles. Other operations are less time-sensitive, so they are fine to be detected next frame.
///   </para>
/// </remarks>
/// <remarks>
///   <para>
///     This is marked as needing a few common components that the organelle component types use, but this doesn't
///     reference directly
///   </para>
/// </remarks>
[ReadsComponent(typeof(Engulfable))]
[ReadsComponent(typeof(MicrobeControl))]
[ReadsComponent(typeof(Physics))]
[ReadsComponent(typeof(WorldPosition))]
[WritesToComponent(typeof(ManualPhysicsControl))]
[WritesToComponent(typeof(EntityLight))]
[RunsAfter(typeof(MicrobeMovementSystem))]
[RunsAfter(typeof(OrganelleComponentFetchSystem))]
[RunsBefore(typeof(PhysicsSensorSystem))]
[RunsBefore(typeof(EntityLightSystem))]
[RuntimeCost(10)]
[RunsOnMainThread]
public partial class OrganelleTickSystem : BaseSystem<World, float>
{
    private readonly IWorldSimulation worldSimulation;
    private readonly ConcurrentStack<(IOrganelleComponent Component, Entity Entity)> queuedSyncRuns = new();

    public OrganelleTickSystem(IWorldSimulation worldSimulation, World world) : base(world)
    {
        this.worldSimulation = worldSimulation;
    }

    public override void AfterUpdate(in float delta)
    {
        while (queuedSyncRuns.TryPop(out var entry))
        {
            // TODO: determine if it is a good idea to always fetch the container like for UpdateAsync here
            // ref entry.Entity.Get<OrganelleContainer>()
            entry.Component.UpdateSync(entry.Entity, delta);
        }

        if (!queuedSyncRuns.IsEmpty)
            GD.PrintErr("Queued sync runs for organelle updates is not empty after processing");
    }

    [Query(Parallel = true)]
    [All<CompoundStorage, WorldPosition>]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void Update([Data] in float delta, ref OrganelleContainer organelleContainer, in Entity entity)
    {
        if (organelleContainer.Organelles == null)
            return;

        // Clear state that needs to be rebuilt each frame
        organelleContainer.ActiveCompoundDetections?.Clear();
        organelleContainer.ActiveSpeciesDetections?.Clear();

        var organelles = organelleContainer.Organelles.Organelles;
        int organelleCount = organelles.Count;

        // Manual loop used here to avoid memory allocations in this very often running code
        for (int i = 0; i < organelleCount; ++i)
        {
            var components = organelles[i].Components;
            int componentCount = components.Count;

            for (int j = 0; j < componentCount; ++j)
            {
                var component = components[j];

                // Organelles can do various things which is why we have the above "All" attribute
                component.UpdateAsync(ref organelleContainer, entity, worldSimulation, delta);

                if (component.UsesSyncProcess)
                    queuedSyncRuns.Push((component, entity));
            }
        }
    }
}
