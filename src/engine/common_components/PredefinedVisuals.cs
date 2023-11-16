namespace Components
{
    /// <summary>
    ///   Entity uses a predefined visual
    /// </summary>
    [JSONDynamicTypeAllowed]
    public struct PredefinedVisuals
    {
        /// <summary>
        ///   Specifies what this entity should display as its visuals
        /// </summary>
        public VisualResourceIdentifier VisualIdentifier;

        /// <summary>
        ///   Don't touch this, used by the system for handling this
        /// </summary>
        public VisualResourceIdentifier LoadedInstance;
    }
}
