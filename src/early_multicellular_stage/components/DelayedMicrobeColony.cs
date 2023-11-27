namespace Components
{
    using DefaultEcs;

    /// <summary>
    ///   Holds operation info for delayed microbe colony operations
    /// </summary>
    public struct DelayedMicrobeColony
    {
        /// <summary>
        ///   If not default then this entity wants to attach to a colony after initialization. Note that this entity
        ///   must already have a <see cref="AttachedToEntity"/> component added.
        /// </summary>
        public Entity FinishAttachingToColony;

        public int GrowAdditionalMembers;

        // This doesn't have an applied field as this component is always removed after operating on it because this
        // component only is used once on each entity

        /// <summary>
        ///   Delayed growth of colony members
        /// </summary>
        /// <param name="growAdditionalMembers">
        ///   How many members to add (should be one less than the early multicellular body plan count for fully
        /// grown colony)
        /// </param>
        public DelayedMicrobeColony(int growAdditionalMembers)
        {
            GrowAdditionalMembers = growAdditionalMembers;

            FinishAttachingToColony = default(Entity);
        }

        /// <summary>
        ///   Attach to a colony in a delayed way (must have attached position already set)
        /// </summary>
        /// <param name="delayAttachToColony">Entity to attach to</param>
        public DelayedMicrobeColony(Entity delayAttachToColony)
        {
            FinishAttachingToColony = delayAttachToColony;
            GrowAdditionalMembers = 0;
        }
    }
}
