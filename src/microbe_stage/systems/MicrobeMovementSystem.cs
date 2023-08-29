namespace Systems
{
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

            var reverseLookVector = position.Position - control.LookAtPoint;
            reverseLookVector.y = 0;
            var up = Vector3.Up;

            var length = reverseLookVector.Length();

            if (length > MathUtils.EPSILON)
            {
                // Normalize vector when it has a length
                reverseLookVector /= length;
            }
            else
            {
                // Without any difference with the look at point compared to the current position, default to looking
                // forward
                reverseLookVector = -Vector3.Forward;
            }

            // Math loaned from Godot.Transform.SetLookAt adapted to fit here and removed one extra
            var column0 = up.Cross(reverseLookVector);
            var column1 = reverseLookVector.Cross(column0);
            var wantedRotation = new Basis(column0.Normalized(), column1.Normalized(), reverseLookVector).Quat();

            float rotationSpeed = 1;

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
                // TODO: make this force make sense
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
