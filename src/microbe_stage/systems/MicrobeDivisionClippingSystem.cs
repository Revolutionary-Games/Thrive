namespace Systems;

using System;
using System.Buffers;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Arch.Core;
using Arch.Core.Extensions;
using Arch.System;
using Components;
using Godot;
using World = Arch.Core.World;

/// <summary>
///   Generates the visuals needed for microbes. Handles the membrane and organelle graphics. Attaching to the
///   Godot scene tree is handled by <see cref="SpatialAttachSystem"/>
/// </summary>
[RunsBefore(typeof(SpatialAttachSystem))]
[RunsBefore(typeof(EntityMaterialFetchSystem))]
[RunsBefore(typeof(SpatialPositionSystem))]
[RuntimeCost(5)]
[RunsOnMainThread]
public partial class MicrobeDivisionClippingSystem : BaseSystem<World, float>
{
    private readonly PhysicalWorld physicalWorld;

    public MicrobeDivisionClippingSystem(PhysicalWorld physicalWorld, World world) : base(world)
    {
        this.physicalWorld = physicalWorld;
    }

    [Query]
    [All<CellProperties, SpatialInstance, EntityMaterial, RenderPriorityOverride>]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void Update(ref CellDivisionCollisionDisablerComponent collisionDisabler, ref CollisionManagement collisionManagement, in Entity entity)
    {
        if (collisionDisabler.IgnoredCollisionWith != null)
        {
            var otherEntity = collisionDisabler.IgnoredCollisionWith.Value;

            if (otherEntity.Has<WorldPosition>() && entity.Has<WorldPosition>())
            {
                if (entity.Has<CellProperties>())
                {
                    // 2.4 = 2 (radiuses) * 1.1
                    var clipOutDistanceSquared = entity.Get<CellProperties>().Radius * 2.2f;

                    // Square
                    clipOutDistanceSquared *= clipOutDistanceSquared;

                    if (otherEntity.Get<WorldPosition>().Position.DistanceSquaredTo(
                        entity.Get<WorldPosition>().Position) >= clipOutDistanceSquared)
                    {
                        if (collisionManagement.RemoveIgnoredCollisions == null)
                            collisionManagement.RemoveIgnoredCollisions = new();

                        collisionManagement.RemoveIgnoredCollisions.Add(otherEntity);
                    }
                }
            }
        }
    }
}
