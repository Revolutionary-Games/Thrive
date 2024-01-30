namespace Systems
{
    using Components;
    using DefaultEcs;
    using DefaultEcs.System;
    using DefaultEcs.Threading;

    /// <summary>
    ///   Plays a sound effect when two cells collide hard enough
    /// </summary>
    [With(typeof(CollisionManagement))]
    [With(typeof(SoundEffectPlayer))]
    [With(typeof(SpeciesMember))]
    [RunsAfter(typeof(PhysicsCollisionManagementSystem))]
    [RunsBefore(typeof(SoundEffectSystem))]
    public sealed class MicrobeCollisionSoundSystem : AEntitySetSystem<float>
    {
        public MicrobeCollisionSoundSystem(World world, IParallelRunner parallelRunner) : base(world, parallelRunner)
        {
        }

        protected override void Update(float delta, in Entity entity)
        {
            ref var collisionManagement = ref entity.Get<CollisionManagement>();

            var count = collisionManagement.GetActiveCollisions(out var collisions);
            if (count < 1)
                return;

            ref var soundEffectPlayer = ref entity.Get<SoundEffectPlayer>();

            for (int i = 0; i < count; ++i)
            {
                ref var collision = ref collisions![i];

                // Only process just started collisions to not trigger the sound multiple times
                if (!collision.JustStarted)
                    continue;

                // TODO: should collisions with any physics entities count?
                // For now collisions with just microbes count
                if (!collision.SecondEntity.Has<SpeciesMember>())
                    continue;

                // Play bump sound if the collision is hard enough (there's enough physics bodies overlap)
                if (collision.PenetrationAmount > Constants.CONTACT_PENETRATION_TO_BUMP_SOUND)
                {
                    // TODO: scale volume with the impact penetration
                    soundEffectPlayer.PlaySoundEffect("res://assets/sounds/soundeffects/microbe-collision.ogg");
                }
            }
        }
    }
}
