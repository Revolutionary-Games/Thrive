namespace Systems;

using System;
using Arch.Core;
using Arch.Core.Extensions;
using Arch.System;
using Components;
using Godot;
using World = Arch.Core.World;

/// <summary>
///   Performs simple spatial animations, such as moving or rescaling objects
/// </summary>
[WritesToComponent(typeof(SpatialAnimation))]
[WritesToComponent(typeof(SpatialInstance))]
[RuntimeCost(0.25f)]
public partial class SpatialAnimationSystem : BaseSystem<World, float>
{
    public SpatialAnimationSystem(World world) : base(world)
    {
    }

    [Query]
    [All<SpatialAnimation, SpatialInstance>]
    private void Update([Data] in float delta, ref SpatialAnimation spatialAnimation, ref SpatialInstance spatialInstance,
        in Entity entity)
    {
        spatialAnimation.TimeSpent += delta;

        float progress = spatialAnimation.TimeSpent / spatialAnimation.AnimationTime;

        if (progress > 1.0f)
        {
            //entity.Remove<SpatialAnimation>();
            return;
        }

        spatialInstance.VisualScale = spatialAnimation.InitialScale * (1.0f - progress)
            + spatialAnimation.FinalScale * progress;
        spatialInstance.ApplyVisualScale = true;
    }
}
