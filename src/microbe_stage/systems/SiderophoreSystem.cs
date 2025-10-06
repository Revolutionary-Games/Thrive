﻿namespace Systems;

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Arch.Core;
using Arch.Core.Extensions;
using Arch.System;
using Components;
using World = Arch.Core.World;

/// <summary>
///   Handles siderophore projectile collisions
/// </summary>
[WritesToComponent(typeof(CompoundStorage))]
[RunsAfter(typeof(PhysicsCollisionManagementSystem))]
[RuntimeCost(0.5f, false)]
public partial class SiderophoreSystem : BaseSystem<World, float>
{
    private readonly ChunkConfiguration smallIronChunkCache = SimulationParameters.Instance.GetBiome("default")
        .Conditions.Chunks["ironSmallChunk"];

    private readonly IWorldSimulation worldSimulation;

    public SiderophoreSystem(World world, IWorldSimulation worldSimulation) : base(world)
    {
        this.worldSimulation = worldSimulation;

        // Create an unshared dictionary for the compounds data that we modify
        smallIronChunkCache.Compounds = new Dictionary<Compound, ChunkConfiguration.ChunkCompound>();
    }

    [Query]
    [All<WorldPosition>]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void Update(ref SiderophoreProjectile projectile, ref CollisionManagement collisions, in Entity entity)
    {
        if (projectile.IsUsed)
            return;

        if (!projectile.ProjectileInitialized)
        {
            projectile.ProjectileInitialized = true;

            collisions.StartCollisionRecording(Constants.MAX_SIMULTANEOUS_COLLISIONS_TINY);
        }

        // Check for active collisions that count as a hit and use up this projectile
        var count = collisions.GetActiveCollisions(out var activeCollisions);
        for (int i = 0; i < count; ++i)
        {
            ref var collision = ref activeCollisions![i];

            if (!HandleSiderophoreCollision(ref collision, ref projectile))
            {
                continue;
            }

            // Expire right now
            ref var timedLife = ref entity.Get<TimedLife>();
            timedLife.TimeToLiveRemaining = -1;

            ref var physics = ref entity.Get<Physics>();

            physics.DisableCollisionState = Physics.CollisionState.DisableCollisions;

            projectile.IsUsed = true;

            break;
        }
    }

    private bool HandleSiderophoreCollision(ref PhysicsCollision collision, ref SiderophoreProjectile projectile)
    {
        var target = collision.SecondEntity;

        if (target == Entity.Null)
            return false;

        // Skip if hit something that isn't a valid target
        if (!target.Has<SiderophoreTarget>() || !target.Has<CompoundStorage>())
            return false;

        ref var compounds = ref target.Get<CompoundStorage>();

        if (!compounds.Compounds.Compounds.TryGetValue(Compound.Iron, out var existingIron))
            return false;

        var efficiency = projectile.Amount;

        var size = Math.Max(Math.Min(efficiency / 3, 20), 1);

        var remainingIron = existingIron - size;

        var firstEntityPosition = collision.FirstEntity.Get<WorldPosition>().Position;

        // This makes a shallow copy of the struct to modify here
        var smallIronChunk = smallIronChunkCache;

        smallIronChunk.ChunkScale = (float)Math.Sqrt(size);
        smallIronChunk.Size = Math.Min(size, remainingIron);

        // Chunk spawn reads the data and copies it, so we can use one dictionary here, but in case this system is
        // multithreaded in the future, there's a lock here
        lock (smallIronChunk.Compounds!)
        {
            // This update is probably safe as long as 2 threads don't exactly at the same time process a chunk
            // collision with siderophore, but this lock should make this much less likely problem
            compounds.Compounds.Compounds[Compound.Iron] = remainingIron;

            smallIronChunk.Compounds = new Dictionary<Compound, ChunkConfiguration.ChunkCompound>
            {
                {
                    Compound.Iron, new ChunkConfiguration.ChunkCompound
                    {
                        Amount = smallIronChunk.Size,
                    }
                },
            };

            SpawnHelpers.SpawnChunk(worldSimulation, smallIronChunk, firstEntityPosition, new Random(), false);
        }

        // TODO: New effect for siderophore collision with iron chunk
        // The world simulation is just coincidentally used inside the lock, this doesn't have to be inside the lock
        // ReSharper disable once InconsistentlySynchronizedField
        SpawnHelpers.SpawnCellBurstEffect(worldSimulation, firstEntityPosition, efficiency - 2);

        return true;
    }
}
