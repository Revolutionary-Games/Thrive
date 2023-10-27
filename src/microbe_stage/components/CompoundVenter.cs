namespace Components
{
    /// <summary>
    ///   An entity that constantly leaks compounds into the environment. Requires <see cref="CompoundStorage"/>.
    /// </summary>
    public struct CompoundVenter
    {
        /// <summary>
        ///   How much of each compound is vented per second
        /// </summary>
        public float VentEachCompoundPerSecond;

        /// <summary>
        ///   When true venting is prevented (used for example when a chunk is engulfed)
        /// </summary>
        public bool VentingPrevented;

        public bool DestroyOnEmpty;

        /// <inheritdoc cref="DamageOnTouch.UsesMicrobialDissolveEffect"/>
        public bool UsesMicrobialDissolveEffect;

        /// <summary>
        ///   Internal flag, don't touch
        /// </summary>
        public bool RanOutOfVentableCompounds;
    }

    public static class CompoundVenterHelpers
    {
        public static void PopImmediately(this ref CompoundVenter venter, ref CompoundStorage compoundStorage,
            ref WorldPosition position, CompoundCloudSystem compoundClouds)
        {
            compoundStorage.VentAllCompounds(position.Position, compoundClouds);

            // For now nothing else except immediately venting everything happens
            _ = venter;
        }
    }
}
