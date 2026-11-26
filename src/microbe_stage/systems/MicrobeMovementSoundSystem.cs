namespace Systems;

using System;
using System.Runtime.CompilerServices;
using Arch.System;
using Components;
using Godot;
using World = Arch.Core.World;

/// <summary>
///   Handles microbe swimming and turning sounds
/// </summary>
[ReadsComponent(typeof(MicrobeControl))]
[ReadsComponent(typeof(Engulfable))]
[ReadsComponent(typeof(Health))]
[ReadsComponent(typeof(Physics))]
[RunsAfter(typeof(PhysicsUpdateAndPositionSystem))]
[RunsAfter(typeof(MicrobeMovementSystem))]
[RunsBefore(typeof(SoundEffectSystem))]
[RuntimeCost(3)]
public partial class MicrobeMovementSoundSystem : BaseSystem<World, float>
{
    public MicrobeMovementSoundSystem(World world) : base(world)
    {
    }

    [Query]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void Update([Data] in float delta, ref MicrobeStatus status, ref MicrobeControl control,
        ref Physics physics, ref SoundEffectPlayer soundEffectPlayer, ref Engulfable engulfable, in Health health)
    {
        bool isEngulfed = engulfable.PhagocytosisStep != PhagocytosisPhase.None;

        if (control.MovementDirection != Vector3.Zero && !isEngulfed)
        {
            var acceleration = physics.Velocity - status.LastLinearVelocity;
            var deltaAcceleration = (acceleration - status.LastLinearAcceleration).LengthSquared();

            var volumeScaler = Math.Min(physics.Velocity.Length() / Constants.MICROBE_MOVEMENT_SOUND_SPEED_SCALER,
                Constants.MICROBE_MOVEMENT_SOUND_MAX_VOLUME_SCALER);

            if (status.MovementSoundCooldownTimer > 0)
                status.MovementSoundCooldownTimer -= delta;

            // The cell starts swimming from a relatively idle velocity, so play the begin movement sound
            if (status.MovementSoundCooldownTimer <= 0 &&
                deltaAcceleration > status.LastLinearAcceleration.LengthSquared() &&
                status.LastLinearVelocity.LengthSquared() <= 1)
            {
                status.MovementSoundCooldownTimer = Constants.MICROBE_MOVEMENT_SOUND_EMIT_COOLDOWN;
                soundEffectPlayer.PlaySoundEffect("res://assets/sounds/soundeffects/microbe-movement-1.ogg");
            }

            soundEffectPlayer.PlayGraduallyTurningUpLoopingSound(Constants.MICROBE_MOVEMENT_SOUND,
                Constants.MICROBE_MOVEMENT_SOUND_MAX_BASE_VOLUME * MathF.Sqrt(volumeScaler),
                Constants.MICROBE_MOVEMENT_SOUND_START_VOLUME, delta);

            status.LastLinearVelocity = physics.Velocity;
            status.LastLinearAcceleration = acceleration;
        }
        else
        {
            // If not moving or this is engulfed, then start turning down the movement sound

            soundEffectPlayer.PlayGraduallyTurningDownSound(Constants.MICROBE_MOVEMENT_SOUND, delta);
        }

        // Play the turning sound separately

        var turnRate = MathF.Abs(physics.AngularVelocity.Y);

        // Ignore small steering corrections, which are particularly common for AI microbes.
        if (turnRate > Constants.MICROBE_TURN_SOUND_MIN_RATE && !isEngulfed && !health.Dead &&
            physics.IsBodyEffectivelyEnabled())
        {
            var volumeFraction = Math.Min((turnRate - Constants.MICROBE_TURN_SOUND_MIN_RATE) /
                (Constants.MICROBE_TURN_SOUND_FULL_VOLUME_RATE - Constants.MICROBE_TURN_SOUND_MIN_RATE), 1.0f);

            soundEffectPlayer.PlaySoundEffectIfNotPlayingAlready(Constants.MICROBE_TURN_SOUND,
                Constants.MICROBE_TURN_SOUND_MAX_VOLUME * volumeFraction);
        }
        else
        {
            soundEffectPlayer.StopSound(Constants.MICROBE_TURN_SOUND);
        }
    }
}
