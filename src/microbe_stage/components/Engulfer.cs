namespace Components
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using DefaultEcs;
    using Godot;
    using Newtonsoft.Json;
    using Systems;

    /// <summary>
    ///   Entity that can engulf <see cref="Engulfable"/>s
    /// </summary>
    [JSONDynamicTypeAllowed]
    public struct Engulfer
    {
        /// <summary>
        ///   Tracks entities this already engulfed. Or is in the process of currently pulling in or expelling.
        /// </summary>
        public List<Entity>? EngulfedObjects;

        /// <summary>
        ///   Tracks entities this has previously engulfed. This is used to not constantly attempt to re-engulf
        ///   something this cannot fully engulf. The value is how long since the object was expelled. Values are
        ///   automatically removed once the time reaches <see cref="Constants.ENGULF_EJECTED_COOLDOWN"/>
        /// </summary>
        [JsonConverter(typeof(DictionaryWithJSONKeysConverter<Entity, float>))]
        public Dictionary<Entity, float>? ExpelledObjects;

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
        public static EngulfCheckResult CanEngulfObject(this ref Engulfer engulfer, uint engulferSpeciesID,
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
            if (engulfer.ExpelledObjects != null && engulfer.ExpelledObjects.ContainsKey(target))
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
                if (engulfer.UsedIngestionCapacity + engulfable.AdjustedEngulfSize >= engulfer.EngulfStorageSize)
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

        /// <summary>
        ///   Tries to find an engulfable entity as close to this engulfer as possible. Note that this is *slow* and
        ///   not meant for normal gameplay code (just using this for the player infrequently is fine as there's only
        ///   ever one player at once)
        /// </summary>
        /// <param name="engulfer">The engulfer that wants to engulf something</param>
        /// <param name="cellProperties">
        ///   Cell properties to determine if this engulfer can even engulf things in the first place
        /// </param>
        /// <param name="organelles">Organelles the engulfer has, used to determine what it can eat or digest</param>
        /// <param name="position">Location of the engulfer to search nearby positions for</param>
        /// <param name="usefulCompoundSource">
        ///   Used to filter engulfables to only ones this bag considers useful
        /// </param>
        /// <param name="engulferEntity">Entity of the engulfer, used to skip self engulfment check</param>
        /// <param name="engulferSpeciesID">Engulfer species ID to use in engulfability checks</param>
        /// <param name="world">Where to fetch potential entities</param>
        /// <param name="searchRadius">How wide to search around the position</param>
        /// <returns>The nearest found point for the engulfable entity or null</returns>
        public static Vector3? FindNearestEngulfableSlow(this ref Engulfer engulfer,
            ref CellProperties cellProperties, ref OrganelleContainer organelles, ref WorldPosition position,
            CompoundBag usefulCompoundSource, in Entity engulferEntity, uint engulferSpeciesID, IWorldSimulation world,
            float searchRadius = 200)
        {
            if (searchRadius < 1)
                throw new ArgumentException("searchRadius must be >= 1");

            // If the microbe cannot engulf, no need for this
            if (!cellProperties.MembraneType.CanEngulf)
                return null;

            Vector3? nearestPoint = null;
            float nearestDistanceSquared = float.MaxValue;
            var searchRadiusSquared = searchRadius * searchRadius;

            // Retrieve nearest potential entities
            foreach (var entity in world.EntitySystem)
            {
                if (!entity.Has<Engulfable>() || !entity.Has<CompoundStorage>())
                    continue;

                ref var engulfable = ref entity.Get<Engulfable>();
                var compounds = entity.Get<CompoundStorage>().Compounds;

                if (compounds.Compounds.Count <= 0 || engulfable.PhagocytosisStep != PhagocytosisPhase.None)
                    continue;

                if (!entity.Has<WorldPosition>())
                    continue;

                ref var entityPosition = ref entity.Get<WorldPosition>();

                // Skip entities that are out of range
                var distance = (entityPosition.Position - position.Position).LengthSquared();
                if (distance > searchRadiusSquared)
                    continue;

                // Skip non-engulfable or digestible entities
                if (organelles.CanDigestObject(ref engulfable) != DigestCheckResult.Ok ||
                    engulfer.CanEngulfObject(engulferSpeciesID, entity) != EngulfCheckResult.Ok)
                {
                    continue;
                }

                // Skip entities that have no useful compounds
                if (!compounds.Compounds.Any(x => usefulCompoundSource.IsUseful(x.Key)))
                    continue;

                if (nearestPoint == null || distance < nearestDistanceSquared)
                {
                    nearestPoint = entityPosition.Position;
                    nearestDistanceSquared = distance;
                }
            }

            return nearestPoint;
        }

        public static bool EjectEngulfable(this ref Engulfer engulfer, ref Engulfable engulfable)
        {
            // Cannot start ejecting a thing that is not in a valid state for that
            switch (engulfable.PhagocytosisStep)
            {
                case PhagocytosisPhase.Ingestion:
                case PhagocytosisPhase.Ingested:
                    break;

                case PhagocytosisPhase.RequestExocytosis:
                    // Already requested
                    return true;

                default:
                    return false;
            }

            engulfable.PhagocytosisStep = PhagocytosisPhase.RequestExocytosis;
            return true;
        }

        /// <summary>
        ///   Immediately deletes all engulfed objects. Should only be used in special cases.
        /// </summary>
        public static void DeleteEngulfedObjects(this ref Engulfer engulfer, IWorldSimulation worldSimulation)
        {
            if (engulfer.EngulfedObjects != null)
            {
                foreach (var engulfedObject in engulfer.EngulfedObjects)
                {
                    worldSimulation.DestroyEntity(engulfedObject);
                }

                engulfer.UsedIngestionCapacity = 0;
            }

            engulfer.ExpelledObjects?.Clear();
        }

        /// <summary>
        ///   Moves all engulfables from <see cref="engulfer"/> to <see cref="targetEngulfer"/>
        /// </summary>
        public static void TransferEngulferObjectsToAnotherEngulfer(this ref Engulfer engulfer,
            ref Engulfer targetEngulfer, in Entity targetEngulferEntity)
        {
            lock (AttachedToEntityHelpers.EntityAttachRelationshipModifyLock)
            {
                if (engulfer.EngulfedObjects is not { Count: > 0 })
                    return;

                // Can't move to a dead engulfer
                if (targetEngulferEntity.Get<Health>().Dead)
                    return;

                foreach (var ourEngulfedEntity in engulfer.EngulfedObjects.ToList())
                {
                    if (!engulfer.EngulfedObjects.Remove(ourEngulfedEntity) || !ourEngulfedEntity.IsAlive ||
                        !ourEngulfedEntity.Has<Engulfable>())
                    {
                        continue;
                    }

                    ref var engulfed = ref ourEngulfedEntity.Get<Engulfable>();

                    if (!targetEngulfer.TakeOwnershipOfEngulfed(targetEngulferEntity, ref engulfed, ourEngulfedEntity))
                    {
                        // Add back to original list as it can't be moved. The engulfing system will eject it
                        // properly out of the dead entity
                        GD.Print("Adding failed to be transferred engulfed back to us for ejecting when " +
                            "death is processed");
                        engulfer.EngulfedObjects.Add(ourEngulfedEntity);
                    }
                }
            }
        }

        /// <summary>
        ///   Moves an already engulfed object to be engulfed by this object's engulfer
        /// </summary>
        /// <remarks>
        ///   <para>
        ///     This has to be called with <see cref="AttachedToEntityHelpers.EntityAttachRelationshipModifyLock"/>
        ///     already locked
        ///   </para>
        /// </remarks>
        private static bool TakeOwnershipOfEngulfed(this ref Engulfer engulfer, in Entity engulferEntity,
            ref Engulfable engulfable, in Entity engulfableEntity)
        {
            return EngulfingSystem.AddAlreadyEngulfedObject(ref engulfer, in engulferEntity, ref engulfable,
                in engulfableEntity);
        }
    }
}
