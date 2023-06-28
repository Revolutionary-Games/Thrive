namespace Components
{
    /// <summary>
    ///   Special actions to perform on time to live expiring and fading out
    /// </summary>
    public struct FadeOutActions
    {
        public float FadeTime;

        public bool DisableCollisions;
        public bool RemoveVelocity;

        /// <summary>
        ///   Disables a particles emitter if there is one on the entity spatial root
        /// </summary>
        public bool DisableParticles;

        /// <summary>
        ///   Internal variable for use by the managing system
        /// </summary>
        public bool CallbackRegistered;
    }
}
