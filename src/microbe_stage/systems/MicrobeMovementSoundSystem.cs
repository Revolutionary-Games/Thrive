﻿namespace Systems;

using System.Runtime.CompilerServices;
using Arch.System;
using Components;
using Godot;
using World = Arch.Core.World;

/// <summary>
///   Handles playing (and stopping) of microbe movement sound
/// </summary>
[ReadsComponent(typeof(MicrobeControl))]
[ReadsComponent(typeof(Engulfable))]
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
        ref Physics physics, ref SoundEffectPlayer soundEffectPlayer, ref Engulfable engulfable)
    {
        if (control.MovementDirection != Vector3.Zero &&
            engulfable.PhagocytosisStep == PhagocytosisPhase.None)
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
