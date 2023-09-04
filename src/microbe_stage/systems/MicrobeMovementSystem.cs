namespace Systems
{
    using System;
    using Components;
    using DefaultEcs;
    using DefaultEcs.System;
    using DefaultEcs.Threading;
    using Godot;
    using World = DefaultEcs.World;

    /// <summary>
    ///   Handles applying <see cref="MicrobeControl"/> to a microbe
    /// </summary>
    [With(typeof(MicrobeControl))]
    [With(typeof(Physics))]
    [With(typeof(WorldPosition))]
    public sealed class MicrobeMovementSystem : AEntitySetSystem<float>
    {
        private readonly PhysicalWorld physicalWorld;

        public MicrobeMovementSystem(PhysicalWorld physicalWorld, World world, IParallelRunner runner) : base(world,
            runner)
        {
            this.physicalWorld = physicalWorld;
        }

        protected override void Update(float delta, in Entity entity)
        {
            ref var physics = ref entity.Get<Physics>();

            if (physics.BodyDisabled || physics.Body == null)
                return;

            ref var control = ref entity.Get<MicrobeControl>();

            // Position is used to calculate the look direction
            ref var position = ref entity.Get<WorldPosition>();

            var lookVector = control.LookAtPoint - position.Position;
            lookVector.y = 0;

            var length = lookVector.Length();

            if (length > MathUtils.EPSILON)
            {
                // Normalize vector when it has a length
                lookVector /= length;
            }
            else
            {
                // Without any difference with the look at point compared to the current position, default to looking
                // forward
                lookVector = Vector3.Forward;
            }

#if DEBUG
            if (!lookVector.IsNormalized())
                throw new Exception("Look vector not normalized");
#endif

            var up = Vector3.Up;

            // Math loaned from Godot.Transform.SetLookAt adapted to fit here and removed one extra
            var column0 = up.Cross(lookVector);
            var column1 = lookVector.Cross(column0);
            var wantedRotation = new Basis(column0.Normalized(), column1.Normalized(), lookVector).Quat();

#if DEBUG
            if (!wantedRotation.IsNormalized())
                throw new Exception("Created target microbe rotation is not normalized");
#endif

            // Lower value is faster rotation
            float rotationSpeed = 0.2f;

            // TODO: rotation penalty from size

            // TODO: rotation speed from cilia

            var movementImpulse = Vector3.Zero;

            if (control.MovementDirection != Vector3.Zero)
            {
                // Normalize if length is over 1 to not allow diagonal movement to be very fast
                length = control.MovementDirection.Length();

                if (length > 1)
                {
                    control.MovementDirection /= length;
                }

                // Base movement force
                movementImpulse = control.MovementDirection * Constants.BASE_MOVEMENT_FORCE;

                // TODO: speed from flagella

                // MovementDirection is proportional to the current cell rotation, so we need to rotate the movement
                // vector to work correctly
                movementImpulse = position.Rotation.Xform(movementImpulse);
            }

            // TODO: ATP usage, and reduce speed when out of ATP

            physicalWorld.ApplyBodyMicrobeControl(physics.Body, movementImpulse, wantedRotation, rotationSpeed);
        }
    }
}
