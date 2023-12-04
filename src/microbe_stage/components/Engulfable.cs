namespace Components
{
    using System;
    using System.Collections.Generic;
    using DefaultEcs;
    using Godot;
    using Newtonsoft.Json;
    using Systems;

    /// <summary>
    ///   Something that can be engulfed by a microbe
    /// </summary>
    [JSONDynamicTypeAllowed]
    public struct Engulfable
    {
        /// <summary>
        ///   Stores the original scale when this becomes engulfed so that it can be always restored (in some
        ///   situations storing this in the transport animation will cause problems with not being able to reliably
        ///   restore this, so this extra variable is used to get this to be problem free)
        /// </summary>
        public Vector3 OriginalScale;

        /// <summary>
        ///   If this is being engulfed then this is not default and is a reference to the entity (trying to) eating us
        /// </summary>
        public Entity HostileEngulfer;

        /// <summary>
        ///   If not null then the engulfer must have the specified enzyme to be able to eat this
        /// </summary>
        public Enzyme? RequisiteEnzymeToDigest;

        /// <summary>
        ///   Set when an object is engulfed
        ///   (by <see cref="EngulfableHelpers.CalculateAdditionalDigestibleCompounds"/>) to the additional resources
        ///   on top of what the entity's <see cref="CompoundStorage"/> contains that are gained by digestion
        /// </summary>
        public Dictionary<Compound, float>? AdditionalEngulfableCompounds;

        public BulkTransportAnimation? BulkTransport;

        /// <summary>
        ///   Base, unadjusted engulfable size of this. That is the number an engulfer compares their ability to engulf
        ///   against to see if something is too big.
        /// </summary>
        /// <remarks>
        ///   <para>
        ///     Note that the AI assumes this is the same as the same entity's engulfing size (in
        ///     <see cref="Engulfer"/>) is the same as this to save a bit of memory when storing things.
        ///   </para>
        /// </remarks>
        public float BaseEngulfSize;

        public float DigestedAmount;

        /// <summary>
        ///   When this is engulfed this gets the total amount of compounds that exist here for digestion progress.
        /// </summary>
        /// <remarks>
        ///   <para>
        ///     TODO: investigate if this is correct as the process system keeps running for engulfed cells
        ///   </para>
        /// </remarks>
        public float InitialTotalEngulfableCompounds;

        public int OriginalRenderPriority;

        /// <summary>
        ///   The current step of phagocytosis process this engulfable is currently in. If not phagocytized,
        ///   state is None.
        /// </summary>
        public PhagocytosisPhase PhagocytosisStep;

        // This might not need a reference to the hostile engulfer as this should have AttachedToEntity to mark what
        // this is attached to

        /// <summary>
        ///   If this is partially digested when ejected from an engulfer, this is destroyed (with a dissolve animation
        ///   if detected to be possible)
        /// </summary>
        public bool DestroyIfPartiallyDigested;

        [JsonIgnore]
        public float AdjustedEngulfSize => BaseEngulfSize * (1 - DigestedAmount);

        public class BulkTransportAnimation
        {
            /// <summary>
            ///   If false the animation is complete and doesn't require actions
            /// </summary>
            public bool Interpolate;

            /// <summary>
            ///   Used to only trigger the digestion eject once
            /// </summary>
            public bool DigestionEjectionStarted;

            public float LerpDuration;
            public float AnimationTimeElapsed;

            // TODO: refactor this to not use nullable values as that will save a bunch of boxing and memory allocation
            public (Vector3? Translation, Vector3? Scale, Vector3? EndosomeScale) TargetValuesToLerp;
            public (Vector3 Translation, Vector3 Scale, Vector3 EndosomeScale) InitialValuesToLerp;
        }
    }

    public static class EngulfableHelpers
    {
        /// <summary>
        ///   Effective size of the engulfable for engulfability calculations
        /// </summary>
        public static float EffectiveEngulfSize(this ref Engulfable engulfable)
        {
            return engulfable.BaseEngulfSize * (1 - engulfable.DigestedAmount);
        }

        /// <summary>
        ///   Calculates additional digestible compounds to be made available when entity is engulfed. Note that only
        ///   <see cref="Compound.Digestible"/> compounds may be returned as the result.
        /// </summary>
        /// <returns>
        ///   The extra compounds to add (this also shouldn't have any 0 values in it for clarity). Or null if there
        ///   aren't any extra digestible compounds.
        /// </returns>
        public static Dictionary<Compound, float>? CalculateAdditionalDigestibleCompounds(
            this ref Engulfable engulfable, in Entity entity)
        {
            // Extra digestible compounds for microbes
            if (entity.Has<OrganelleContainer>() && entity.Has<CompoundStorage>())
            {
                return CalculateMicrobeAdditionalDigestibleCompounds(ref entity.Get<OrganelleContainer>(),
                    ref entity.Get<CompoundStorage>());
            }

            // This entity type doesn't have extra digestible compounds
            return null;
        }

        /// <summary>
        ///   Called when this becomes engulfed and starts to be pulled in (this may get immediately thrown out if this
        ///   is not digestible by the attacker)
        /// </summary>
        public static void OnBecomeEngulfed(this ref Engulfable engulfable, in Entity entity,
            int engulferRenderPriority)
        {
            if (entity.Has<CellProperties>())
            {
                ref var cellProperties = ref entity.Get<CellProperties>();

                if (cellProperties.CreatedMembrane != null)
                {
                    // Make membrane not wiggle to make it look better
                    cellProperties.CreatedMembrane.WigglyNess = 0;
                }
            }

            // Stop being in ready to reproduce state while engulfed
            if (entity.Has<OrganelleContainer>())
            {
                ref var organelleContainer = ref entity.Get<OrganelleContainer>();
                organelleContainer.AllOrganellesDivided = false;
            }

            if (entity.Has<MicrobeEventCallbacks>())
            {
                ref var callbacks = ref entity.Get<MicrobeEventCallbacks>();

                callbacks.OnReproductionStatus?.Invoke(entity, false);
            }

            // Disable absorbing compounds
            if (entity.Has<CompoundAbsorber>())
            {
                entity.Get<CompoundAbsorber>().AbsorbSpeed = -1;
            }

            // Force mode to normal
            if (entity.Has<MicrobeControl>())
            {
                // Cells are yanked from colonies so this doesn't need to use colony aware set
                entity.Get<MicrobeControl>().State = MicrobeState.Normal;
            }

            // Disable compound venting
            if (entity.Has<UnneededCompoundVenter>())
            {
                entity.Get<UnneededCompoundVenter>().VentThreshold = float.MaxValue;
            }

            // Save the original scale for re-applying when ejecting
            ref var spatial = ref entity.Get<SpatialInstance>();
            engulfable.OriginalScale = spatial.ApplyVisualScale ? spatial.VisualScale : Vector3.One;

            if (entity.Has<RenderPriorityOverride>())
            {
                ref var renderPriority = ref entity.Get<RenderPriorityOverride>();

                engulfable.OriginalRenderPriority = renderPriority.RenderPriority;

                // Make the render priority of our organelles be on top of the highest possible render priority
                // of the hostile engulfer's organelles
                // +2 is used here as the membrane also takes one render priority slot
                renderPriority.RenderPriority = engulferRenderPriority + Constants.HEX_MAX_RENDER_PRIORITY + 2;
                renderPriority.RenderPriorityApplied = false;

                // TODO: the above doesn't take recursive engulfing into account but that's probably fine enough for now
                // If the above is done, IngestEngulfableFromOtherEntity might also need changes
            }
        }

        /// <summary>
        ///   Called when it is confirmed that an engulfable will be digested (i.e. will not be thrown out immediately
        ///   due to being inedible)
        /// </summary>
        public static void OnReportBecomeIngestedIfCallbackRegistered(this ref Engulfable engulfable, in Entity entity)
        {
            if (!entity.Has<MicrobeEventCallbacks>())
                return;

            ref var callbacks = ref entity.Get<MicrobeEventCallbacks>();

            callbacks.OnIngestedByHostile?.Invoke(entity, engulfable.HostileEngulfer);
        }

        /// <summary>
        ///   Called when an entity is thrown out from the engulfer, for example due to being indigestible or if the
        ///   attacker dies
        /// </summary>
        /// <remarks>
        ///   <para>
        ///     This needs to take in the <see cref="IWorldSimulation"/> and spawn system to be able to spawn death
        ///     chunks as a special case for a microbe that basically died during engulfment.
        ///   </para>
        /// </remarks>
        public static void OnExpelledFromEngulfment(this ref Engulfable engulfable, in Entity entity,
            ISpawnSystem spawnSystem, IWorldSimulation worldSimulation)
        {
            bool alreadyDeathProcessed = false;

            if (entity.Has<Health>())
                alreadyDeathProcessed = entity.Get<Health>().DeathProcessed;

            if (engulfable.DigestedAmount >= Constants.PARTIALLY_DIGESTED_THRESHOLD && !alreadyDeathProcessed)
            {
                if (entity.Has<Health>() && entity.Has<OrganelleContainer>())
                {
                    // Cell is too damaged from digestion, can't live in open environment and is considered dead
                    ref var health = ref entity.Get<Health>();
                    health.Kill();

                    // Organelles must be initialized to drop chunks
                    ref var organelleContainer = ref entity.Get<OrganelleContainer>();

                    if (organelleContainer.Organelles != null)
                    {
                        ref var position = ref entity.Get<WorldPosition>();

                        // Most of the normal microbe death gets skipped on engulfed things, instead we do some stuff
                        // here

                        MicrobeDeathSystem.CustomizeSpawnedChunk? customizeCallback = null;

                        if (engulfable.HostileEngulfer.Has<WorldPosition>())
                        {
                            var hostilePosition = engulfable.HostileEngulfer.Get<WorldPosition>().Position;

                            customizeCallback = (ref Vector3 position) =>
                            {
                                var direction = hostilePosition.DirectionTo(position);
                                position += direction *
                                    Constants.EJECTED_PARTIALLY_DIGESTED_CELL_CORPSE_CHUNKS_SPAWN_OFFSET;

                                // Apply outwards ejection velocity
                                // TODO: this used to also add the linear velocity of the ejected entity (which was
                                // probably not doing much, but now we could take the velocity from the engulfer
                                // and add it here)
                                return direction * Constants.ENGULF_EJECTION_VELOCITY;
                            };
                        }

                        var recorder = worldSimulation.StartRecordingEntityCommands();

                        MicrobeDeathSystem.SpawnCorpseChunks(ref organelleContainer,
                            entity.Get<CompoundStorage>().Compounds, spawnSystem, worldSimulation, recorder,
                            position.Position, new Random(), customizeCallback, null);

                        SpawnHelpers.FinalizeEntitySpawn(recorder, worldSimulation);

                        // Don't need to do the normal entity state restore as the entity was killed and will be
                        // shortly destroyed
                        return;
                    }
                }
            }

            // There used to be an else branch here that set the escaped flag for the microbe for use in population
            // bonus. That is now gone as this feature didn't really do anything anymore due to the new engulf
            // mechanics which are extremely hard to escape.

            if (entity.Has<CellProperties>())
            {
                if (alreadyDeathProcessed)
                {
                    if (!entity.Has<TimedLife>())
                    {
                        GD.PrintErr("Microbe was ejected from engulfment without setting lifetime remaining");
                    }
                }

                ref var cellProperties = ref entity.Get<CellProperties>();

                // Reset wigglyness (which was cleared when this was engulfed)
                if (cellProperties.CreatedMembrane != null)
                    cellProperties.ApplyMembraneWigglyness(cellProperties.CreatedMembrane);
            }

            // Restore unlimited absorption speed
            if (entity.Has<CompoundAbsorber>())
            {
                entity.Get<CompoundAbsorber>().AbsorbSpeed = 0;
            }

            // Re-enable compound venting
            if (entity.Has<UnneededCompoundVenter>())
            {
                entity.Get<UnneededCompoundVenter>().VentThreshold = Constants.DEFAULT_MICROBE_VENT_THRESHOLD;
            }

            // Reset render priority
            if (entity.Has<RenderPriorityOverride>())
            {
                ref var renderPriority = ref entity.Get<RenderPriorityOverride>();

                renderPriority.RenderPriority = engulfable.OriginalRenderPriority;
                renderPriority.RenderPriorityApplied = false;

                // If recursive engulfing render priority is supported in the future, there might be a need to write
                // some code here related to that
            }

            // Restore scale
            ref var spatial = ref entity.Get<SpatialInstance>();

#if DEBUG
            if (engulfable.OriginalScale.Length() < MathUtils.EPSILON)
            {
                throw new Exception("Ejected engulfable with zero original scale");
            }
#endif

            spatial.VisualScale = engulfable.OriginalScale;

            if (entity.Has<MicrobeEventCallbacks>())
            {
                ref var callbacks = ref entity.Get<MicrobeEventCallbacks>();
                callbacks.OnEjectedFromHostileEngulfer?.Invoke(entity);
            }
        }

        public static void CalculateBonusDigestibleGlucose(Dictionary<Compound, float> result,
            CompoundBag compoundCapacityInfo, Compound? glucose = null)
        {
            glucose ??= SimulationParameters.Instance.GetCompound("glucose");

            result.TryGetValue(glucose, out float existingGlucose);
            result[glucose] = existingGlucose + compoundCapacityInfo.GetCapacityForCompound(glucose) *
                Constants.ADDITIONAL_DIGESTIBLE_GLUCOSE_AMOUNT_MULTIPLIER;
        }

        private static Dictionary<Compound, float> CalculateMicrobeAdditionalDigestibleCompounds(
            ref OrganelleContainer organelleContainer, ref CompoundStorage heldCompounds)
        {
            if (organelleContainer.Organelles == null)
                throw new ArgumentException("Organelle container has to be initialized");

            var result = new Dictionary<Compound, float>();

            // Add some part of the build cost of all the organelles
            foreach (var organelle in organelleContainer.Organelles)
            {
                foreach (var entry in organelle.Definition.InitialComposition)
                {
                    if (!entry.Key.Digestible)
                        continue;

                    result.TryGetValue(entry.Key, out float existing);
                    result[entry.Key] = existing + entry.Value;
                }
            }

            CalculateBonusDigestibleGlucose(result, heldCompounds.Compounds);
            return result;
        }
    }
}
