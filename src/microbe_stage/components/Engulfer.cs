namespace Components
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using DefaultEcs;
    using Godot;

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
        ///   Entities that are currently being attempted to be engulfed. Once succeeded these will be moved to
        ///   <see cref="EngulfedObjects"/>
        /// </summary>
        public List<Entity>? AttemptingToEngulf;

        /// <summary>
        ///   Tracks entities this has previously engulfed. This is used to not constantly attempt to re-engulf
        ///   something this cannot fully engulf
        /// </summary>
        public List<Entity>? ExpelledObjects;

        /// <summary>
        ///   Total size of engulfable objects in <see cref="AttemptingToEngulf"/>. This is a separate variable to
        ///   avoid having to constantly read the entity components of <see cref="AttemptingToEngulf"/> when checking
        ///   if more things can be engulfed.
        /// </summary>
        public float SumOfAttemptingToEngulfSizes;

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

    public static class EngulferHelpers
    {
        /// <summary>
        ///   Direct engulfing check. Microbe should use <see cref="CellPropertiesHelpers.CanEngulfObject"/>
        /// </summary>
        public static EngulfCheckResult CanEngulfObject(ref this Engulfer engulfer, uint engulferSpeciesID,
            in Entity target)
        {
            if (!target.IsAlive)
                return EngulfCheckResult.TargetDead;

            bool invulnerable = false;

            // Can't engulf dead microbes (unlikely to happen but this is a fail-safe)
            if (target.Has<Health>())
            {
                ref var health = ref target.Get<Health>();

                if (health.Dead)
                    return EngulfCheckResult.TargetDead;

                invulnerable = health.Invulnerable;
            }

            // Can't engulf recently ejected objects, this act as a cooldown
            if (engulfer.ExpelledObjects != null && engulfer.ExpelledObjects.Contains(target))
                return EngulfCheckResult.RecentlyExpelled;

            try
            {
                ref var engulfable = ref target.Get<Engulfable>();

                if (engulfable.PhagocytosisStep != PhagocytosisPhase.None)
                    return EngulfCheckResult.NotInEngulfMode;

                // The following checks are in a specific order to make sure the fail reporting logic gives
                // sensible results (this means that a few things that shouldn't be necessary to be inside this try
                // block are in here)

                // Disallow cannibalism
                if (target.Has<SpeciesMember>() && target.Get<SpeciesMember>().ID == engulferSpeciesID)
                    return EngulfCheckResult.CannotCannibalize;

                // Needs to be big enough to engulf
                if (engulfer.EngulfingSize < engulfable.AdjustedEngulfSize * Constants.ENGULF_SIZE_RATIO_REQ)
                    return EngulfCheckResult.TargetTooBig;

                // Limit amount of things that can be engulfed at once
                if (engulfer.UsedIngestionCapacity >= engulfer.EngulfStorageSize ||
                    engulfer.UsedIngestionCapacity + engulfable.AdjustedEngulfSize >= engulfer.EngulfStorageSize)
                {
                    return EngulfCheckResult.IngestedMatterFull;
                }

                // Too many things attempted to be pulled in at once
                if (engulfer.UsedIngestionCapacity + engulfer.SumOfAttemptingToEngulfSizes +
                    engulfable.AdjustedEngulfSize >= engulfer.EngulfStorageSize)
                {
                    return EngulfCheckResult.IngestedMatterFull;
                }
            }
            catch (Exception e)
            {
                GD.PrintErr("Cannot check engulfing an object that is missing Engulfable component: " + e);
                return EngulfCheckResult.InvalidEntity;
            }

            // Godmode grants player complete engulfment invulnerability
            if (invulnerable)
                return EngulfCheckResult.TargetInvulnerable;

            return EngulfCheckResult.Ok;
        }
    }
}
