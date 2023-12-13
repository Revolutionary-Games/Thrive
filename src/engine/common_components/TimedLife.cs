namespace Components
{
    using DefaultEcs;
    using Newtonsoft.Json;

    /// <summary>
    ///   Entities that despawn after a certain amount of time
    /// </summary>
    [JSONDynamicTypeAllowed]
    public struct TimedLife
    {
        /// <summary>
        ///   Custom callback to be triggered when the timed life is over. If this returns false then the entity won't
        ///   be automatically destroyed. If this callback sets <see cref="FadeTimeRemaining"/> then this also won't
        ///   be automatically destroyed.
        /// </summary>
        /// <remarks>
        ///   <para>
        ///     This is save ignored with the intention that any systems that will use the time over callback will
        ///     re-apply it after the save is loaded.
        ///   </para>
        /// </remarks>
        [JsonIgnore]
        public OnTimeOver? CustomTimeOverCallback;

        public float TimeToLiveRemaining;

        /// <summary>
        ///   If not null then this entity is fading out and the timed despawn system will wait until this time is up
        ///   as well
        /// </summary>
        public float? FadeTimeRemaining;

        public bool OnTimeOverTriggered;

        public delegate bool OnTimeOver(Entity entity, ref TimedLife timedLife);
    }
}
