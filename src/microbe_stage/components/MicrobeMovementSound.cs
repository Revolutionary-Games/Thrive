namespace Components
{
    using Godot;

    /// <summary>
    ///   Data needed to play the microbe movement sounds. Depends on <see cref="WorldPosition"/> and
    ///   <see cref="SoundEffectPlayer"/>
    /// </summary>
    public struct MicrobeMovementSound
    {
        public Vector3 LastLinearVelocity;
        public Vector3 LastLinearAcceleration;
        public Vector3 LinearAcceleration;

        public float MovementSoundCooldownTimer;
    }
}
