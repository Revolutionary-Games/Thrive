namespace Components
{
    /// <summary>
    ///   Makes entities vent excess (or not-useful) compounds from
    /// </summary>
    [JSONDynamicTypeAllowed]
    public struct UnneededCompoundVenter
    {
        /// <summary>
        ///   Sets how many extra compounds above capacity a thing needs to have before some are vented. For example
        ///   2 means any compounds that are above 2x the capacity will be vented.
        /// </summary>
        public float VentThreshold;
    }
}
