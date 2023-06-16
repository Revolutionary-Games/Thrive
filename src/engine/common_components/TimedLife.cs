namespace Components
{
    using DefaultEcs;

    /// <summary>
    ///   Entities that despawn after a certain amount of time
    /// </summary>
    public struct TimedLife
    {
        /// <summary>
        ///   Custom callback to be triggered when the timed life is over. If this returns false then the entity won't
        ///   be automatically destroyed. If this callback sets <see cref="FadeTimeRemaining"/> then this also won't
        ///   be automatically destroyed.
        /// </summary>
        public OnTimeOver? CustomTimeOverCallback;

        public float TimeToLiveRemaining;

        /// <summary>
        ///   If not null then this entity is fading out and the timed despawn system will wait until this time is up
        ///   as well
        /// </summary>
        public float? FadeTimeRemaining;

        public bool OnTimeOverTriggered;

        public delegate bool OnTimeOver(Entity entity, TimedLife timedLife);
    }
}
