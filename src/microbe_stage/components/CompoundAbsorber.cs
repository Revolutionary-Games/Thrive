namespace Components
{
    using System.Collections.Generic;

    /// <summary>
    ///   Entity that can absorb compounds from <see cref="CompoundCloudSystem"/>. Requires <see cref="WorldPosition"/>
    ///   and <see cref="CompoundStorage"/> components as well.
    /// </summary>
    public struct CompoundAbsorber
    {
        /// <summary>
        ///   If not null then this tracks the total absorbed compounds
        /// </summary>
        public Dictionary<Compound, float>? TotalAbsorbedCompounds;

        /// <summary>
        ///   Compounds this absorber considers useful and will absorb
        /// </summary>
        public HashSet<Compound>? UsefulCompounds;

        /// <summary>
        ///   How big the radius for absorption is
        /// </summary>
        public float AbsorbRadius;

        /// <summary>
        ///   How fast this can absorb things
        /// </summary>
        public float AbsorbSpeed;

        /// <summary>
        ///   The effectiveness (ratio of gained vs compounds taken from the clouds) of absorption
        /// </summary>
        public float AbsorptionRatio;

        /// <summary>
        ///   When true, then <see cref="UsefulCompounds"/> must be set for any absorption to take place
        /// </summary>
        public bool OnlyAbsorbUseful;
    }
}
