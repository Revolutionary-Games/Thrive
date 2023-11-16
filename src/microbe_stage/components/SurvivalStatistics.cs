namespace Components
{
    /// <summary>
    ///   Collects information to give population bonuses and penalties to species based on how well they do in the
    ///   stage interacting with each other and the player for real
    /// </summary>
    [JSONDynamicTypeAllowed]
    public struct SurvivalStatistics
    {
        public float EscapeInterval;

        /// <summary>
        ///   Used to prevent population bonus from escaping a predator triggering too much
        /// </summary>
        public bool HasEscaped;
    }
}
