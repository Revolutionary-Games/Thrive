namespace Systems
{
    using System;
    using Components;
    using DefaultEcs;
    using DefaultEcs.System;

    /// <summary>
    ///   Makes sure the player's physics body is never allowed to sleep. This makes sure the microbe stage doesn't get
    ///   stuck as microbe movement cannot be applied if the physics world has only sleeping bodies (as the body
    ///   control apply operation will be skipped).
    /// </summary>
    [With(typeof(PlayerMarker))]
    [With(typeof(Physics))]
    [ReadsComponent(typeof(PlayerMarker))]
    [ReadsComponent(typeof(Physics))]
    [RunsAfter(typeof(PhysicsBodyCreationSystem))]
    [RunsAfter(typeof(PhysicsBodyDisablingSystem))]
    public sealed class DisallowPlayerBodySleepSystem : AEntitySetSystem<float>
    {
        private readonly PhysicalWorld physicalWorld;
        private WeakReference<NativePhysicsBody>? appliedSleepDisableTo;

        public DisallowPlayerBodySleepSystem(PhysicalWorld physicalWorld, World world) : base(world, null)
        {
            this.physicalWorld = physicalWorld;
        }

        protected override void Update(float delta, in Entity entity)
        {
            ref var physics = ref entity.Get<Physics>();

            if (!physics.IsBodyEffectivelyEnabled())
                return;

            if (appliedSleepDisableTo != null && appliedSleepDisableTo.TryGetTarget(out var appliedTo) &&
                ReferenceEquals(appliedTo, physics.Body))
            {
                return;
            }

            // Apply no sleep to the new body
            physicalWorld.SetBodyAllowSleep(physics.Body!, false);
            appliedSleepDisableTo = new WeakReference<NativePhysicsBody>(physics.Body!);
        }
    }
}
