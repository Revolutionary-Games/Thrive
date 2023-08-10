namespace Components
{
    using System.Collections.Generic;
    using DefaultEcs;

    /// <summary>
    ///   Entity that can engulf <see cref="Engulfable"/>s
    /// </summary>
    public struct Engulfer
    {
        /// <summary>
        ///   Tracks entities this already engulfed.
        /// </summary>
        public List<Entity>? EngulfedObjects;

        /// <summary>
        ///   Tracks entities this has previously engulfed. This is used to not constantly attempt to re-engulf
        ///   something this cannot fully engulf
        /// </summary>
        public List<Entity>? ExpelledObjects;

        /// <summary>
        ///   The attacking capability of this engulfer. Used to determine what this can eat
        /// </summary>
        public float EngulfingSize;

        /// <summary>
        ///   The amount of space all of the currently engulfed objects occupy in the cytoplasm. This is used to
        ///   determine whether a cell can ingest any more objects or not due to being full.
        /// </summary>
        /// <remarks>
        ///   <para>
        ///     In a more technical sense, this is the accumulated <see cref="Engulfable.AdjustedEngulfSize"/> from all
        ///     the ingested objects. Maximum should be this cell's own <see cref="Engulfable.BaseEngulfSize"/>.
        ///   </para>
        /// </remarks>
        public float UsedIngestionCapacity;

        /// <summary>
        ///   Total size that all engulfed objects need to fit in
        /// </summary>
        public float EngulfStorageSize;
    }
}
