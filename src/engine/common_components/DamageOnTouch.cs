namespace Components
{
    using Newtonsoft.Json;

    /// <summary>
    ///   Damages any entities touched by this entity. Requires <see cref="CollisionManagement"/>
    /// </summary>
    public struct DamageOnTouch
    {
        /// <summary>
        ///   The name of the caused damage type this deals
        /// </summary>
        public string DamageType;

        /// <summary>
        ///   The amount of damage this causes. This is allowed to be 0 to implement entities that just get destroyed
        ///   on touch. When <see cref="DestroyOnTouch"/> is true this is the inflicted damage, otherwise this is the
        ///   damage per second.
        /// </summary>
        public float DamageAmount;

        /// <summary>
        ///   If true then this is destroyed when this collides with something this could deal damage to
        /// </summary>
        public bool DestroyOnTouch;

        /// <summary>
        ///   Uses a microbe stage dissolve effect on the visuals when being destroyed
        /// </summary>
        public bool UsesMicrobialDissolveEffect;

        /// <summary>
        ///   Internal variable, don't modify
        /// </summary>
        [JsonIgnore]
        public bool StartedDestroy;

        /// <summary>
        ///   Internal variable, don't modify
        /// </summary>
        [JsonIgnore]
        public bool RegisteredWithCollisions;
    }
}
