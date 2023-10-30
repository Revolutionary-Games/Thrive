namespace Systems
{
    using Components;
    using DefaultEcs;
    using DefaultEcs.System;
    using DefaultEcs.Threading;
    using Godot;
    using World = DefaultEcs.World;

    /// <summary>
    ///   Applies external (impulse, direct velocity) control to physics bodies
    /// </summary>
    [With(typeof(Physics))]
    [With(typeof(ManualPhysicsControl))]
    public sealed class PhysicsBodyControlSystem : AEntitySetSystem<float>
    {
        private readonly PhysicalWorld physicalWorld;

        public PhysicsBodyControlSystem(PhysicalWorld physicalWorld, World world, IParallelRunner runner) :
            base(world, runner)
        {
            this.physicalWorld = physicalWorld;
        }

        protected override void Update(float delta, in Entity entity)
        {
            ref var physics = ref entity.Get<Physics>();

            var body = physics.Body;
            if (physics.BodyDisabled || body == null)
                return;

            ref var control = ref entity.Get<ManualPhysicsControl>();

            if (control.PhysicsApplied && physics.VelocitiesApplied)
                return;

            if (!physics.VelocitiesApplied)
            {
                physicalWorld.SetBodyVelocity(body, physics.Velocity, physics.AngularVelocity);
                physics.VelocitiesApplied = true;
            }

            if (!control.PhysicsApplied)
            {
                if (control.RemoveVelocity && control.RemoveAngularVelocity)
                {
                    control.RemoveVelocity = false;
                    control.RemoveAngularVelocity = false;
                    physicalWorld.SetBodyVelocity(body, Vector3.Zero, Vector3.Zero);
                }
                else if (control.RemoveVelocity)
                {
                    control.RemoveVelocity = false;
                    physicalWorld.SetOnlyBodyVelocity(body, Vector3.Zero);
                }
                else if (control.RemoveAngularVelocity)
                {
                    control.RemoveAngularVelocity = false;
                    physicalWorld.SetOnlyBodyAngularVelocity(body, Vector3.Zero);
                }

                if (control.ImpulseToGive != Vector3.Zero)
                {
                    control.ImpulseToGive = Vector3.Zero;
                    physicalWorld.GiveImpulse(body, control.ImpulseToGive);
                }

                if (control.AngularImpulseToGive != Vector3.Zero)
                {
                    control.AngularImpulseToGive = Vector3.Zero;
                    physicalWorld.GiveAngularImpulse(body, control.AngularImpulseToGive);
                }

                control.PhysicsApplied = true;
            }
        }
    }
}
