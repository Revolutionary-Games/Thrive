namespace Components
{
    /// <summary>
    ///   Entity uses a predefined visual that is automatically loaded by
    ///   <see cref="Systems.PredefinedVisualLoaderSystem"/>. This is much better to use for save compatibility than
    ///   directly setting the visuals when creating en entity as that can't be automatically redone when loading a
    ///   save.
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
