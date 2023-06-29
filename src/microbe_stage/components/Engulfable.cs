namespace Components
{
    using DefaultEcs;

    /// <summary>
    ///   Something that can be engulfed by a microbe
    /// </summary>
    public struct Engulfable
    {
        /// <summary>
        ///   If this is being engulfed then this is not null and is a reference to the entity (trying to) eating us
        /// </summary>
        public Entity? HostileEngulfer;

        /// <summary>
        ///   If not null then the engulfer must have the specified enzyme to be able to eat this
        /// </summary>
        public Enzyme? RequisiteEnzymeToDigest;

        /// <summary>
        ///   Base, unadjusted engulfable size of this. That is the number an engulfer compares their ability to engulf
        ///   against to see if something is too big.
        /// </summary>
        public float BaseEngulfSize;

        public float DigestedAmount;

        public PhagocytosisPhase PhagocytosisStep;

        // TODO: implement this for when ejected
        /// <summary>
        ///   If this is partially digested when ejected from an engulfer, this is destroyed (with a dissolve animation
        ///   if detected to be possible)
        /// </summary>
        public bool DestroyIfPartiallyDigested;
    }

    public static class EngulfableExtensions
    {
        /// <summary>
        ///   Effective size of the engulfable for engulfability calculations
        /// </summary>
        public static float EffectiveEngulfSize(this Engulfable engulfable)
        {
            return engulfable.BaseEngulfSize * (1 - engulfable.DigestedAmount);
        }
    }
}
