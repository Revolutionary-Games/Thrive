namespace Components
{
    /// <summary>
    ///   Marks entity as the player's controlled character
    /// </summary>
    [JSONDynamicTypeAllowed]
    public struct PlayerMarker
    {
        /// <summary>
        ///   Used for a few player specific dying conditions that take different amount of time than for AI creatures
        /// </summary>
        public float PlayerDeathTimer;
    }
}
