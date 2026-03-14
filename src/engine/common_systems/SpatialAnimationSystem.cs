namespace Systems;

using System;
using Arch.Core;
using Arch.Core.Extensions;
using Arch.System;
using Components;
using World = Arch.Core.World;

/// <summary>
///   Performs simple spatial animations, such as moving or rescaling objects
/// </summary>
[WritesToComponent(typeof(SpatialAnimation))]
[WritesToComponent(typeof(SpatialInstance))]
[WritesToComponent(typeof(AttachedToEntity))]
[RuntimeCost(0.25f)]
public partial class SpatialAnimationSystem : BaseSystem<World, float>
{
    private readonly IWorldSimulation worldSimulation;

    public SpatialAnimationSystem(IWorldSimulation worldSimulation, World world) : base(world)
    {
        this.worldSimulation = worldSimulation;
    }

    [Query]
    [All<SpatialAnimation, SpatialInstance>]
    private void Update([Data] in float delta, ref SpatialAnimation spatialAnimation,
        ref SpatialInstance spatialInstance, in Entity entity)
    {
        spatialAnimation.TimeSpent += delta;

        float progress = MathF.Min(spatialAnimation.TimeSpent / spatialAnimation.AnimationTime, 1.0f);

        progress *= progress;

        spatialInstance.VisualScale = spatialAnimation.InitialScale * (1.0f - progress)
            + spatialAnimation.FinalScale * progress;
        spatialInstance.ApplyVisualScale = true;

        if (entity.Has<AttachedToEntity>())
        {
            entity.Get<AttachedToEntity>().RelativePosition = spatialAnimation.InitialPosition * (1.0f - progress)
                + spatialAnimation.FinalPosition * progress;
        }

        if (progress >= 1.0f)
        {
            var recorder = worldSimulation.StartRecordingEntityCommands();
            recorder.Remove<SpatialAnimation>(entity);
            worldSimulation.FinishRecordingEntityCommands(recorder);
        }
    }
}
