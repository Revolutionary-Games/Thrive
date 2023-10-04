namespace Components
{
    using System;
    using System.Collections.Generic;
    using DefaultEcs;
    using Newtonsoft.Json;

    /// <summary>
    ///   Something that can be engulfed by a microbe
    /// </summary>
    public struct Engulfable
    {
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
        ///   When this is engulfed this gets the total amount of compounds that exist here for digestion progress.
        /// </summary>
        /// <remarks>
        ///   <para>
        ///     TODO: investigate if this is correct as the process system keeps running for engulfed cells
        ///   </para>
        /// </remarks>
        public float InitialTotalEngulfableCompounds;

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

    public static class EngulfableHelpers
    {
        /// <summary>
        ///   Effective size of the engulfable for engulfability calculations
        /// </summary>
        public static float EffectiveEngulfSize(this ref Engulfable engulfable)
        {
            return engulfable.BaseEngulfSize * (1 - engulfable.DigestedAmount);
        }

        // TODO: hook this up to the start of engulfment process
        public static Dictionary<Compound, float> CalculateAdditionalDigestibleCompounds(this ref Engulfable engulfable,
            in Entity entity)
        {
            // Extra digestible compounds for microbes
            if (entity.Has<OrganelleContainer>() && entity.Has<CompoundStorage>())
            {
                return CalculateMicrobeAdditionalDigestibleCompounds(ref entity.Get<OrganelleContainer>(),
                    ref entity.Get<CompoundStorage>());
            }

            // This entity type doesn't have extra digestible compounds
            return new Dictionary<Compound, float>();
        }

        /// <summary>
        ///   Called when this becomes engulfed and starts to be pulled in (this may get immediately thrown out if this
        ///   is not digestible by the attacker)
        /// </summary>
        public static void OnBecomeEngulfed(this ref Engulfable engulfable, in Entity entity)
        {
            Membrane.WigglyNess = 0;

            UnreadyToReproduce();

            // Make the render priority of our organelles be on top of the highest possible render priority
            // of the hostile engulfer's organelles
            var hostile = HostileEngulfer.Value;
            if (hostile != null)
            {
                foreach (var organelle in organelles!)
                {
                    var newPriority = Mathf.Clamp(Hex.GetRenderPriority(organelle.Position) +
                        hostile.OrganelleMaxRenderPriority, 0, Material.RenderPriorityMax);
                    organelle.UpdateRenderPriority(newPriority);
                }
            }

            Colony?.RemoveFromColony(this);

            playerEngulfedDeathTimer = 0;
        }

        /// <summary>
        ///   Called when it is confirmed that an engulfable will be digested (i.e. will not be thrown out immediately
        ///   due to being inedible)
        /// </summary>
        public static void OnReportBecomeIngestedIfCallbackRegistered(this ref Engulfable engulfable, in Entity entity)
        {
            OnIngestedByHostile?.Invoke(this, HostileEngulfer.Value!);
        }

        /// <summary>
        ///   Called when an entity is thrown out from the engulfer, for example due to being indigestible or if the
        ///   attacker dies
        /// </summary>
        public static void OnExpelledFromEngulfment(this ref Engulfable engulfable, in Entity entity)
        {
            var hostile = HostileEngulfer.Value;

            // Reset wigglyness
            ApplyMembraneWigglyness();

            // Reset our organelles' render priority back to their original values
            foreach (var organelle in organelles!)
            {
                organelle.UpdateRenderPriority(Hex.GetRenderPriority(organelle.Position));
            }

            if (DigestedAmount >= Constants.PARTIALLY_DIGESTED_THRESHOLD)
            {
                // Cell is too damaged from digestion, can't live in open environment and is considered dead
                // Kill() is not called here because it's already called during partial digestion
                OnDestroyed();
                var droppedChunks = OnKilled().ToList();

                if (hostile == null)
                    return;

                foreach (var chunk in droppedChunks)
                {
                    throw new NotImplementedException();

                    // var direction = hostile.Transform.origin.DirectionTo(chunk.Transform.origin);
                    // chunk.Translation += direction *
                    //     Constants.EJECTED_PARTIALLY_DIGESTED_CELL_CORPSE_CHUNKS_SPAWN_OFFSET;
                    //
                    // var impulse = direction * chunk.Mass * Constants.ENGULF_EJECTION_FORCE;
                    //
                    // // Apply outwards ejection force
                    // chunk.ApplyCentralImpulse(impulse + LinearVelocity);
                }
            }
            else
            {
                hasEscaped = true;
                escapeInterval = 0;
                playerEngulfedDeathTimer = 0;
            }
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
                    result.TryGetValue(entry.Key, out float existing);
                    result[entry.Key] = existing + entry.Value;
                }
            }

            CalculateBonusDigestibleGlucose(result, heldCompounds.Compounds);
            return result;
        }

        private static void CalculateBonusDigestibleGlucose(Dictionary<Compound, float> result,
            CompoundBag compoundCapacityInfo, Compound? glucose = null)
        {
            glucose ??= SimulationParameters.Instance.GetCompound("glucose");

            result.TryGetValue(glucose, out float existingGlucose);
            result[glucose] = existingGlucose + compoundCapacityInfo.GetCapacityForCompound(glucose) *
                Constants.ADDITIONAL_DIGESTIBLE_GLUCOSE_AMOUNT_MULTIPLIER;
        }
    }
}
