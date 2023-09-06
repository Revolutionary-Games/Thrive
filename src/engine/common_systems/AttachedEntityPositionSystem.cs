namespace Systems
{
    using Components;
    using DefaultEcs;
    using DefaultEcs.System;
    using DefaultEcs.Threading;
    using World = DefaultEcs.World;

    /// <summary>
    ///   Handles positioning of entities attached to each other
    /// </summary>
    [With(typeof(AttachedToEntity))]
    [With(typeof(WorldPosition))]
    [RunsAfter(typeof(PhysicsUpdateAndPositionSystem))]
    [RunsBefore(typeof(SpatialPositionSystem))]
    public sealed class AttachedEntityPositionSystem : AEntitySetSystem<float>
    {
        public AttachedEntityPositionSystem(World world, IParallelRunner runner) : base(world, runner)
        {
        }

        protected override void Update(float state, in Entity entity)
        {
            ref var attachInfo = ref entity.Get<AttachedToEntity>();

            if (!attachInfo.AttachedTo.Has<WorldPosition>())
            {
                // This can happen if the entity is dead now
                // TODO: should this queue a clear of the data (it's not safe to remove during an update without using
                // the recorder interface)
                return;
            }

            ref var parentPosition = ref attachInfo.AttachedTo.Get<WorldPosition>();

            ref var position = ref entity.Get<WorldPosition>();

            position.Position = parentPosition.Position + attachInfo.RelativePosition;
            position.Rotation = parentPosition.Rotation * attachInfo.RelativeRotation;
        }
    }
}
