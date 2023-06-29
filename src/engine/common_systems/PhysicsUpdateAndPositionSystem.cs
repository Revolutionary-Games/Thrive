namespace Systems
{
    using Components;
    using DefaultEcs;
    using DefaultEcs.System;
    using DefaultEcs.Threading;
    using Godot;
    using World = DefaultEcs.World;

    /// <summary>
    ///   Reads the physics state into position and also applies a few physics component state things
    /// </summary>
    [With(typeof(Physics))]
    [With(typeof(WorldPosition))]
    public sealed class PhysicsUpdateAndPositionSystem : AEntitySetSystem<float>
    {
        private readonly PhysicalWorld physicalWorld;

        public PhysicsUpdateAndPositionSystem(PhysicalWorld physicalWorld, World world, IParallelRunner runner) : base(
            world, runner)
        {
            this.physicalWorld = physicalWorld;
        }

        protected override void Update(float delta, in Entity entity)
        {
            ref var physics = ref entity.Get<Physics>();

            var body = physics.Body;
            if (physics.BodyDisabled || body == null)
                return;

            ref var position = ref entity.Get<WorldPosition>();

            // TODO: implement this operation
            // if (physics.TeleportBodyPosition || physics.TeleportBodyRotationAlso)
            // {
            //     if (physics.TeleportBodyRotationAlso)
            //     {
            //     }
            //     else
            //     {
            //         physics.BodyCreatedInWorld!.SetBodyPosition(body, position.Position);
            //     }
            // }

            (position.Position, position.Rotation) = physicalWorld.ReadBodyPosition(body);

            if (physics.TrackVelocity)
            {
                (physics.Velocity, physics.AngularVelocity) = physicalWorld.ReadBodyVelocity(body);
            }

            if (physics.LockToYAxis)
            {
                // Apply fixing to Y-position if drifted too far
                var driftAmount = Mathf.Abs(position.Position.y);

                if (driftAmount > Constants.PHYSICS_ALLOWED_Y_AXIS_DRIFT)
                {
                    physicalWorld.FixBodyYCoordinateToZero(body);
                }
            }

            if (!physics.DampingApplied)
            {
                physics.DampingApplied = true;

                if (physics.LinearDamping != null)
                {
                    physicalWorld.SetDamping(body, physics.LinearDamping.Value, physics.AngularDamping);
                }
            }
        }
    }
}
