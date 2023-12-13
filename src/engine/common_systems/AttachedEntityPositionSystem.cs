namespace Systems
{
    using Components;
    using DefaultEcs;
    using DefaultEcs.System;
    using DefaultEcs.Threading;

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

                if (!attachInfo.AttachedTo.IsAlive)
                {
                    // TODO: should this queue a clear of the data (using entity command recorder), or entity delete?
                }

                return;
            }

            // TODO: optimize for attached entities where the position / parent position doesn't change each frame?

            ref var parentPosition = ref attachInfo.AttachedTo.Get<WorldPosition>();

            ref var position = ref entity.Get<WorldPosition>();

            position.Position = parentPosition.Position + parentPosition.Rotation.Xform(attachInfo.RelativePosition);
            position.Rotation = parentPosition.Rotation * attachInfo.RelativeRotation;
        }
    }
}
