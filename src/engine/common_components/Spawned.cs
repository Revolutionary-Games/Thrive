namespace Components
{
    /// <summary>
    ///   Entity that has been spawned by a spawn system and can be automatically despawned
    /// </summary>
    public struct Spawned
    {
        /// <summary>
        ///   If the squared distance to the player of this object is
        ///   greater than this, it is despawned.
        /// </summary>
        public int DespawnRadiusSquared;

        /// <summary>
        ///   How much this entity contributes to the entity limit relative to a single node
        /// </summary>
        public float EntityWeight;

        /// <summary>
        ///   Set to true when despawning is disallowed temporarily. For permanently disallowing despawning remove this
        ///   component.
        /// </summary>
        public bool DisallowDespawning;
    }
}
