namespace Systems
{
    using Components;
    using DefaultEcs;
    using DefaultEcs.System;
    using DefaultEcs.Threading;
    using Godot;
    using World = DefaultEcs.World;

    /// <summary>
    ///   Handles playing (and stopping) of microbe movement sound
    /// </summary>
    [With(typeof(MicrobeStatus))]
    [With(typeof(MicrobeControl))]
    [With(typeof(Engulfable))]
    [With(typeof(Physics))]
    [With(typeof(SoundEffectPlayer))]
    [RunsAfter(typeof(PhysicsUpdateAndPositionSystem))]
    [RunsAfter(typeof(MicrobeMovementSystem))]
    public sealed class MicrobeMovementSoundSystem : AEntitySetSystem<float>
    {
        public MicrobeMovementSoundSystem(World world, IParallelRunner parallelRunner) :
            base(world, parallelRunner)
        {
        }

        protected override void Update(float delta, in Entity entity)
        {
            ref var status = ref entity.Get<MicrobeStatus>();
            ref var control = ref entity.Get<MicrobeControl>();
            ref var physics = ref entity.Get<Physics>();
            ref var soundEffectPlayer = ref entity.Get<SoundEffectPlayer>();

            if (control.MovementDirection != Vector3.Zero &&
                entity.Get<Engulfable>().PhagocytosisStep == PhagocytosisPhase.None)
            {
                var acceleration = physics.Velocity - status.LastLinearVelocity;
                var deltaAcceleration = (acceleration - status.LastLinearAcceleration).LengthSquared();

                if (status.MovementSoundCooldownTimer > 0)
                    status.MovementSoundCooldownTimer -= delta;

                // The cell starts moving from a relatively idle velocity, so play the begin movement sound
                // TODO: Account for cell turning, I can't figure out a reliable way to do that using the current
                // calculation - Kasterisk
                if (status.MovementSoundCooldownTimer <= 0 &&
                    deltaAcceleration > status.LastLinearAcceleration.LengthSquared() &&
                    status.LastLinearVelocity.LengthSquared() <= 1)
                {
                    status.MovementSoundCooldownTimer = Constants.MICROBE_MOVEMENT_SOUND_EMIT_COOLDOWN;
                    soundEffectPlayer.PlaySoundEffect("res://assets/sounds/soundeffects/microbe-movement-1.ogg");
                }

                soundEffectPlayer.PlayGraduallyTurningUpLoopingSound(Constants.MICROBE_MOVEMENT_SOUND,
                    Constants.MICROBE_MOVEMENT_SOUND_MAX_VOLUME, Constants.MICROBE_MOVEMENT_SOUND_START_VOLUME, delta);

                status.LastLinearVelocity = physics.Velocity;
                status.LastLinearAcceleration = acceleration;
            }
            else
            {
                // If not moving or this is engulfed, then start turning down the movement sound

                soundEffectPlayer.PlayGraduallyTurningDownSound(Constants.MICROBE_MOVEMENT_SOUND, delta);
            }
        }
    }
}
