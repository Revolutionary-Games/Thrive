namespace Components
{
    using Godot;

    /// <summary>
    ///   Control variables for specifying how a microbe wants to move / behave
    /// </summary>
    public struct MicrobeControl
    {
        /// <summary>
        ///   The point towards which the microbe will move to point to
        /// </summary>
        public Vector3 LookAtPoint;

        /// <summary>
        ///   The direction the microbe wants to move. Doesn't need to be normalized
        /// </summary>
        public Vector3 MovementDirection;

        /// <summary>
        ///   If not null this microbe will fire the specified toxin on next update. This is done to allow
        ///   multithreaded AI to decide to fire a toxin.
        /// </summary>
        public Compound? QueuedToxinToEmit;

        /// <summary>
        ///   This is here as this is very closely related to
        /// </summary>
        public float SlimeSecretionCooldown;

        /// <summary>
        ///   How long this microbe wants to emit slime (this is done so that AI which doesn't run each frame can still
        ///   sufficiently control the emission of slime)
        /// </summary>
        public float QueuedSlimeSecretionTime;

        /// <summary>
        ///   Time until this microbe can fire agents (toxin) again
        /// </summary>
        public float AgentEmissionCooldown;

        /// <summary>
        ///   This is an overall state of the Microbe
        /// </summary>
        public MicrobeState State;

        /// <summary>
        ///   Whether this microbe is currently being slowed by environmental slime
        /// </summary>
        public bool SlowedBySlime;

        /// <summary>
        ///   Constructs an instance with a sensible <see cref="LookAtPoint"/> set
        /// </summary>
        /// <param name="startingPosition">World position this entity is starting at</param>
        public MicrobeControl(Vector3 startingPosition)
        {
            LookAtPoint = startingPosition + new Vector3(0, 0, -1);
            MovementDirection = new Vector3(0, 0, 0);
            QueuedToxinToEmit = null;
            SlimeSecretionCooldown = 0;
            QueuedSlimeSecretionTime = 0;
            AgentEmissionCooldown = 0;
            State = MicrobeState.Normal;
            SlowedBySlime = false;
        }
    }
}
