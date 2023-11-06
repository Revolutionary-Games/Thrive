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
        ///   How big the radius for absorption is
        /// </summary>
        public float AbsorbRadius;

        /// <summary>
        ///   How fast this can absorb things. If 0 then the absorption speed is not limited.
        /// </summary>
        public float AbsorbSpeed;

        /// <summary>
        ///   The effectiveness (ratio of gained vs compounds taken from the clouds) of absorption
        /// </summary>
        public float AbsorptionRatio;

        /// <summary>
        ///   When true, then the <see cref="CompoundBag"/> that we put things in must have useful compounds set and
        ///   only those will be absorbed
        /// </summary>
        public bool OnlyAbsorbUseful;
    }
}
