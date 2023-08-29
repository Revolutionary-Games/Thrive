namespace Components
{
    using DefaultEcs;
    using Newtonsoft.Json;

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
        /// <remarks>
        ///   <para>
        ///      Note that the AI assumes this is the same as the same entity's engulfing size (in
        ///      <see cref="Engulfer"/>) is the same as this to save a bit of memory when storing things.
        ///   </para>
        /// </remarks>
        public float BaseEngulfSize;

        public float DigestedAmount;

        /// <summary>
        ///   The current step of phagocytosis process this engulfable is currently in. If not phagocytized,
        ///   state is None.
        /// </summary>
        public PhagocytosisPhase PhagocytosisStep;

        // This might not need a reference to the hostile engulfer as this should have AttachedToEntity to mark what
        // this is attached to

        // TODO: implement this for when ejected
        /// <summary>
        ///   If this is partially digested when ejected from an engulfer, this is destroyed (with a dissolve animation
        ///   if detected to be possible)
        /// </summary>
        public bool DestroyIfPartiallyDigested;

        [JsonIgnore]
        public float AdjustedEngulfSize => BaseEngulfSize * (1 - DigestedAmount);
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
