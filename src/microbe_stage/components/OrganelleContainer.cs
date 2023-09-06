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
    ///   Entity that contains <see cref="PlacedOrganelle"/>
    /// </summary>
    public struct OrganelleContainer
    {
        /// <summary>
        ///   Instances of all the organelles in this entity
        /// </summary>
        public OrganelleLayout<PlacedOrganelle>? Organelles;

        public Dictionary<Enzyme, int>? AvailableEnzymes;

        /// <summary>
        ///   The slime jets attached to this microbe. JsonIgnore as the components add themselves to this list each
        ///   load.
        /// </summary>
        [JsonIgnore]
        public List<SlimeJetComponent>? SlimeJets;

        /// <summary>
        ///   Compound detections set by chemoreceptor organelles.
        /// </summary>
        [JsonIgnore]
        public HashSet<(Compound Compound, float Range, float MinAmount, Color Colour)>? ActiveCompoundDetections;

        /// <summary>
        ///   The number of agent vacuoles. Determines the time between toxin shots.
        /// </summary>
        public int AgentVacuoleCount;

        /// <summary>
        ///   The microbe stores here the sum of capacity of all the current organelles. This is here to prevent anyone
        ///   from messing with this value if we used the Capacity from the CompoundBag for the calculations that use
        ///   this.
        /// </summary>
        public float OrganellesCapacity;

        public int HexCount;

        public float RotationSpeed;

        // TODO: add the following variables only if really needed
        // private bool organelleMaxRenderPriorityDirty = true;
        // private int cachedOrganelleMaxRenderPriority;

        // TODO: could maybe redo these "feature flags" by having separate tagging components?
        public bool HasSignalingAgent;

        public bool HasBindingAgent;

        /// <summary>
        ///   True once all organelles are divided to not continuously run code that is triggered when a cell is ready
        ///   to reproduce.
        /// </summary>
        /// <remarks>
        ///   <para>
        ///     This is not saved so that the player cell can enable the editor when loading a save where the player is
        ///     ready to reproduce. If more code is added to be ran just once based on this flag, it needs to be made
        ///     sure that that code re-running after loading a save is not a problem.
        ///   </para>
        /// </remarks>
        [JsonIgnore]
        public bool AllOrganellesDivided;

        /// <summary>
        ///   Reset this if the organelles are changed to make the <see cref="MicrobeVisualsSystem"/> recreate them
        /// </summary>
        [JsonIgnore]
        public bool OrganelleVisualsCreated;

        /// <summary>
        ///   Internal variable used by the <see cref="MicrobeVisualsSystem"/> to only create visuals for missing /
        ///   removed organelles
        /// </summary>
        [JsonIgnore]
        public Dictionary<PlacedOrganelle, Spatial>? CreatedOrganelleVisuals;

        // TODO: maybe put the process list refresh variable here and a some new system to regenerate the process list?
        // instead of just doing it when changing the organelles?
    }

    public static class OrganelleContainerHelpers
    {
        private static readonly Lazy<Enzyme> Lipase = new(() => SimulationParameters.Instance.GetEnzyme("lipase"));

        /// <summary>
        ///   Returns the check result whether this microbe can digest the target (has the enzyme necessary).
        /// </summary>
        /// <remarks>
        ///   <para>
        ///     This is different from <see cref="CellPropertiesHelpers.CanEngulfObject"/> because ingestibility and
        ///     digestibility are separate, you can engulf a walled cell but not digest it if you're missing the enzyme
        ///     required to do so.
        ///   </para>
        /// </remarks>
        public static DigestCheckResult CanDigestObject(this OrganelleContainer organelleContainer,
            ref Engulfable engulfable)
        {
            var enzyme = engulfable.RequisiteEnzymeToDigest;

            if (enzyme != null && organelleContainer.AvailableEnzymes?.ContainsKey(enzyme) != true)
                return DigestCheckResult.MissingEnzyme;

            return DigestCheckResult.Ok;
        }

        /// <summary>
        ///   Returns true if the given organelles can enter binding mode. Multicellular species can't attach random
        ///   cells to themselves anymore.
        /// </summary>
        public static bool CanBind(this ref OrganelleContainer organelleContainer, ref SpeciesMember species)
        {
            return species.Species is MicrobeSpecies && organelleContainer.HasBindingAgent;
        }

        public static bool CanUnbind(this ref OrganelleContainer organelleContainer, ref SpeciesMember species,
            in Entity entity)
        {
            return species.Species is MicrobeSpecies && entity.Has<MicrobeColony>();
        }

        public static void CreateOrganelleLayout(this ref OrganelleContainer container, ICellProperties cellProperties)
        {
            container.Organelles?.Clear();

            container.Organelles ??= new OrganelleLayout<PlacedOrganelle>();

            foreach (var organelleTemplate in cellProperties.Organelles)
            {
                container.Organelles.Add(new PlacedOrganelle(organelleTemplate.Definition, organelleTemplate.Position,
                    organelleTemplate.Orientation)
                {
                    Upgrades = organelleTemplate.Upgrades,
                });
            }

            container.CalculateOrganelleLayoutStatistics();

            container.AllOrganellesDivided = false;
        }

        /// <summary>
        ///   Resets a created layout of organelles on an existing microbe. This variant exists as this can perform
        ///   some extra operations not yet valid when initially creating a layout.
        /// </summary>
        public static void ResetOrganelleLayout(this ref OrganelleContainer container, in Entity entity,
            ICellProperties cellProperties, Species baseReproductionCostFrom)
        {
            container.CreateOrganelleLayout(cellProperties);

            // Reproduction progress is lost
            container.AllOrganellesDivided = false;

            ref var reproduction = ref entity.Get<ReproductionStatus>();
            reproduction.SetupRequiredBaseReproductionCompounds(baseReproductionCostFrom);

            // Unbind if a colony's master cell removed its binding agent.
            if (!container.HasBindingAgent && entity.Has<MicrobeColony>())
            {
                throw new NotImplementedException();

                // Colony.RemoveFromColony(this);
            }

            ref var status = ref entity.Get<MicrobeStatus>();

            // Make chemoreception update happen immediately in case the settings changed so that new information is
            // used earlier
            status.TimeUntilChemoreceptionUpdate = 0;

            if (entity.Has<EarlyMulticellularSpeciesMember>())
            {
                throw new NotImplementedException();

                // ResetMulticellularProgress();
            }
        }

        /// <summary>
        ///   Marks that the organelles have changed. Has to be called for things to be refreshed.
        /// </summary>
        public static void OnOrganellesChanged(this ref OrganelleContainer container)
        {
            container.OrganelleVisualsCreated = false;

            // TODO: should there be a specific system that refreshes this data?
            // CreateOrganelleLayout might need changes in that case to call this method immediately
            container.CalculateOrganelleLayoutStatistics();
        }

        /// <summary>
        ///   Returns a list of tuples, representing all possible compound targets. These are not all clouds that the
        ///   microbe can smell using the instanced organelles that add chemoreception capability;
        ///   only the best candidate of each compound type.
        /// </summary>
        /// <param name="container">The current organelles to use</param>
        /// <param name="entity">
        ///   Entity doing the smelling, this is required to perform a check for microbe colony membership and access
        ///   that data so this method may need to traverse quite a lot of data.
        /// </param>
        /// <param name="position">The position the smelling entity is at</param>
        /// <param name="clouds">CompoundCloudSystem to scan</param>
        /// <returns>
        ///   A list of tuples. Each tuple contains the type of compound, the color of the line (if any needs to be drawn),
        ///   and the location where the compound is located.
        /// </returns>
        public static List<(Compound Compound, Color Colour, Vector3 Target)> PerformCompoundDetection(
            this ref OrganelleContainer container, in Entity entity, Vector3 position,
            IReadonlyCompoundClouds clouds)
        {
            HashSet<(Compound Compound, float Range, float MinAmount, Color Colour)> collectedUniqueCompoundDetections;

            // Colony lead cell uses all the chemoreceptors in the colony to make them all work
            if (entity.Has<MicrobeColony>())
            {
                // TODO: reimplement recursive colony smell settings collection
                throw new NotImplementedException("colony smelling not reimplemented yet");
            }
            else
            {
                if (container.ActiveCompoundDetections == null)
                    return new List<(Compound Compound, Color Colour, Vector3 Target)>();

                collectedUniqueCompoundDetections = container.ActiveCompoundDetections;
            }

            var detections = new List<(Compound Compound, Color Colour, Vector3 Target)>();

            foreach (var (compound, range, minAmount, colour) in collectedUniqueCompoundDetections)
            {
                var detectedCompound = clouds.FindCompoundNearPoint(position, compound, range, minAmount);

                if (detectedCompound != null)
                {
                    detections.Add((compound, colour, detectedCompound.Value));
                }
            }

            return detections;
        }

        public static void CalculateOrganelleLayoutStatistics(this ref OrganelleContainer container)
        {
            container.AvailableEnzymes?.Clear();
            container.AvailableEnzymes ??= new Dictionary<Enzyme, int>();

            // Cells have a minimum of at least one unit of lipase enzyme
            container.AvailableEnzymes[Lipase.Value] = 1;

            container.AgentVacuoleCount = 0;
            container.OrganellesCapacity = 0;
            container.HasSignalingAgent = false;
            container.HasBindingAgent = false;

            // TODO: rotation speed calculation
            // TODO: rotation penalty from size
            // TODO: rotation speed from cilia
            // Lower value is faster rotation
            container.RotationSpeed = 0.2f;

            if (container.Organelles == null)
                throw new InvalidOperationException("Organelle list needs to be initialized first");

            container.HexCount = container.Organelles.HexCount;

            foreach (var organelle in container.Organelles)
            {
                if (organelle.HasComponent<AgentVacuoleComponent>())
                    ++container.AgentVacuoleCount;

                if (organelle.HasComponent<SignalingAgentComponent>())
                    container.HasSignalingAgent = true;

                if (organelle.HasComponent<BindingAgentComponent>())
                    container.HasBindingAgent = true;

                container.OrganellesCapacity = organelle.StorageCapacity;

                if (organelle.StoredEnzymes.Count > 0)
                {
                    foreach (var enzyme in organelle.StoredEnzymes)
                    {
                        // Filter out invalid enzyme values
                        if (enzyme.Value <= 0)
                            continue;

                        container.AvailableEnzymes.TryGetValue(enzyme.Key, out var existing);
                        container.AvailableEnzymes[enzyme.Key] = existing + enzyme.Value;
                    }
                }
            }

            // TODO: slime jets implementation
        }

        /// <summary>
        ///   Calculates the reproduction progress for a cell, used to show how close the player is getting to the editor.
        /// </summary>
        /// <returns>The total reproduction progress</returns>
        public static float CalculateReproductionProgress(this ref OrganelleContainer organelleContainer,
            ref SpeciesMember speciesMember, in Entity entity, CompoundBag storedCompounds,
            WorldGenerationSettings worldSettings,
            out Dictionary<Compound, float> gatheredCompounds, out Dictionary<Compound, float> totalCompounds)
        {
            // Calculate total compounds needed to split all organelles
            totalCompounds = organelleContainer.CalculateTotalReproductionCompounds(entity, speciesMember.Species);

            // Calculate how many compounds the cell already has absorbed to grow
            gatheredCompounds = organelleContainer.CalculateAlreadyAbsorbedCompounds(
                ref entity.Get<ReproductionStatus>(),
                entity, speciesMember.Species);

            // Add the currently held compounds, but only if configured as this can be pretty confusing for players
            // to have the bars in ready to reproduce state for a while before the time limited reproduction actually
            // catches up
            if (Constants.ALWAYS_SHOW_STORED_COMPOUNDS_IN_REPRODUCTION_PROGRESS ||
                !worldSettings.LimitReproductionCompoundUseSpeed)
            {
                foreach (var key in gatheredCompounds.Keys.ToList())
                {
                    float value = Math.Max(0.0f, storedCompounds.GetCompoundAmount(key) -
                        Constants.ORGANELLE_GROW_STORAGE_MUST_HAVE_AT_LEAST);

                    if (value > 0)
                    {
                        float existing = gatheredCompounds[key];

                        // Only up to the total needed
                        float total = totalCompounds[key];

                        gatheredCompounds[key] = Math.Min(total, existing + value);
                    }
                }
            }

            float totalFraction = 0;

            foreach (var entry in totalCompounds)
            {
                if (gatheredCompounds.TryGetValue(entry.Key, out var gathered) && entry.Value != 0)
                    totalFraction += gathered / entry.Value;
            }

            return totalFraction / totalCompounds.Count;
        }

        /// <summary>
        ///   Calculates total compounds needed for a cell to reproduce, used by calculateReproductionProgress to
        ///   calculate the fraction done.
        /// </summary>
        public static Dictionary<Compound, float> CalculateTotalReproductionCompounds(
            this ref OrganelleContainer organelleContainer, in Entity entity, Species species)
        {
            if (entity.Has<MicrobeColony>())
            {
                throw new NotImplementedException();

                // return CalculateTotalBodyPlanCompounds();
            }

            var result = organelleContainer.CalculateNonDuplicateOrganelleInitialCompositionTotals();

            result.Merge(species.BaseReproductionCost);

            return result;
        }

        public static Dictionary<Compound, float> CalculateNonDuplicateOrganelleInitialCompositionTotals(
            this ref OrganelleContainer organelleContainer)
        {
            if (organelleContainer.Organelles == null)
                throw new InvalidOperationException("OrganelleContainer must be initialized first");

            var result = new Dictionary<Compound, float>();

            foreach (var organelle in organelleContainer.Organelles)
            {
                if (organelle.IsDuplicate)
                    continue;

                result.Merge(organelle.Definition.InitialComposition);
            }

            return result;
        }

        /// <summary>
        ///   Calculates how much compounds organelles have already absorbed
        /// </summary>
        public static Dictionary<Compound, float> CalculateAlreadyAbsorbedCompounds(
            this ref OrganelleContainer organelleContainer, ref ReproductionStatus baseReproductionInfo,
            in Entity entity, Species species)
        {
            if (organelleContainer.Organelles == null)
                throw new InvalidOperationException("OrganelleContainer must be initialized first");

            var result = new Dictionary<Compound, float>();

            foreach (var organelle in organelleContainer.Organelles)
            {
                if (organelle.IsDuplicate)
                    continue;

                if (organelle.WasSplit)
                {
                    // Organelles are reset on split, so we use the full
                    // cost as the gathered amount
                    result.Merge(organelle.Definition.InitialComposition);
                    continue;
                }

                organelle.CalculateAbsorbedCompounds(result);
            }

            if (entity.Has<MicrobeColony>())
            {
                throw new NotImplementedException();

                // result.Merge(compoundsUsedForMulticellularGrowth);
            }
            else
            {
                // For single microbes the base reproduction cost needs to be calculated here
                // TODO: can we make this more efficient somehow
                foreach (var entry in species.BaseReproductionCost)
                {
                    float remaining = 0;

                    baseReproductionInfo.MissingCompoundsForBaseReproduction?.TryGetValue(entry.Key, out remaining);

                    var used = entry.Value - remaining;

                    result.TryGetValue(entry.Key, out var alreadyUsed);

                    result[entry.Key] = alreadyUsed + used;
                }
            }

            return result;
        }
    }
}
