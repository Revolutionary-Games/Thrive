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
    [JSONDynamicTypeAllowed]
    public struct OrganelleContainer
    {
        /// <summary>
        ///   Instances of all the organelles in this entity. This is saved but components are not saved. This means
        ///   that components are re-created when a save is loaded.
        /// </summary>
        public OrganelleLayout<PlacedOrganelle>? Organelles;

        public Dictionary<Enzyme, int>? AvailableEnzymes;

        // The following few component vectors exist to allow access ti update the state of a few organelle components
        // from various systems to update the visuals state

        // TODO: maybe move these component caches to a separate component to reduce this component's size?
        /// <summary>
        ///   The slime jets attached to this microbe. JsonIgnore as the components add themselves to this list each
        ///   load (as they are recreated).
        /// </summary>
        [JsonIgnore]
        public List<SlimeJetComponent>? SlimeJets;

        /// <summary>
        ///   Flagellum components that need to be animated when the cell is moving at top speed
        /// </summary>
        [JsonIgnore]
        public List<MovementComponent>? ThrustComponents;

        // Note that this exists here for the potential future need that MicrobeMovementSystem will need to use cilia
        // and reduce rotation rate if not enough ATP to rotate at full speed
        // ReSharper disable once CollectionNeverQueried.Global
        /// <summary>
        ///   Cilia components that need to be animated when the cell is rotating fast
        /// </summary>
        [JsonIgnore]
        public List<CiliaComponent>? RotationComponents;

        /// <summary>
        ///   Compound detections set by chemoreceptor organelles.
        /// </summary>
        [JsonIgnore]
        public HashSet<(Compound Compound, float Range, float MinAmount, Color Colour)>? ActiveCompoundDetections;

        /// <summary>
        ///   Compound detections set by chemoreceptor organelles.
        /// </summary>
        [JsonIgnore]
        public HashSet<(Species TargetSpecies, float Range, Color Colour)>? ActiveSpeciesDetections;

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

        /// <summary>
        ///   Lower values are faster rotation
        /// </summary>
        public float RotationSpeed;

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
        ///   Reset this if organelles are changed. Otherwise <see cref="SlimeJets"/> etc. variables won't work
        ///   correctly
        /// </summary>
        [JsonIgnore]
        public bool OrganelleComponentsCached;

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

        public static bool CanBind(this ref OrganelleContainer organelleContainer, Species species)
        {
            return species is MicrobeSpecies && organelleContainer.HasBindingAgent;
        }

        /// <summary>
        ///   Returns true if this entity can bind with the target
        /// </summary>
        public static bool CanBindWith(this ref OrganelleContainer organelleContainer, Species ourSpecies,
            Entity other)
        {
            // Can only bind with microbes
            if (!other.Has<MicrobeSpeciesMember>())
                return false;

            // Things with missing binding agents can't bind (this is just an extra safety check and an excuse to make
            // organelleContainer parameter be actually used)
            if (!organelleContainer.HasBindingAgent)
                return false;

            // Cannot hijack the player
            if (other.Has<PlayerMarker>())
                return false;

            // Cannot bind with other species (this explicitly doesn't use the ID check as this is a pretty important
            // thing to never go wrong by binding a cell that shouldn't be bound to)
            if (other.Get<MicrobeSpeciesMember>().Species != ourSpecies)
                return false;

            // Cannot hijack other colonies (TODO: yet)
            if (other.Has<MicrobeColony>() || other.Has<MicrobeColonyMember>())
                return false;

            // Can't bind with dead things
            if (other.Get<Health>().Dead)
                return false;

            // Other must have membrane created (but not absolutely necessarily up to date)
            if (other.Get<CellProperties>().CreatedMembrane == null)
                return false;

            return true;
        }

        public static bool CanUnbind(this ref OrganelleContainer organelleContainer, ref SpeciesMember species,
            in Entity entity)
        {
            return species.Species is MicrobeSpecies &&
                (entity.Has<MicrobeColony>() || entity.Has<MicrobeColonyMember>());
        }

        public static void CreateOrganelleLayout(this ref OrganelleContainer container, ICellDefinition cellDefinition,
            List<Hex> workMemory1, List<Hex> workMemory2)
        {
            // Set an initial rotation rate that will be reset after this is properly calculated
            container.RotationSpeed = 0.5f;

            container.Organelles?.Clear();

            container.Organelles ??= new OrganelleLayout<PlacedOrganelle>();

            foreach (var organelleTemplate in cellDefinition.Organelles)
            {
                container.Organelles.AddFast(new PlacedOrganelle(organelleTemplate.Definition,
                    organelleTemplate.Position,
                    organelleTemplate.Orientation, organelleTemplate.Upgrades), workMemory1, workMemory2);
            }

            container.CalculateOrganelleLayoutStatistics();

            container.AllOrganellesDivided = false;

            // Reset this to notify the visuals system that it needs to check the new changed organelles
            container.OrganelleVisualsCreated = false;
        }

        /// <summary>
        ///   Resets a created layout of organelles on an existing microbe. This variant exists as this can perform
        ///   some extra operations not yet valid when initially creating a layout.
        /// </summary>
        public static void ResetOrganelleLayout(this ref OrganelleContainer container,
            ref CompoundStorage storageToUpdate, ref BioProcesses bioProcessesToUpdate, in Entity entity,
            ICellDefinition cellDefinition, Species baseReproductionCostFrom, IWorldSimulation worldSimulation,
            List<Hex> workMemory1, List<Hex> workMemory2)
        {
            container.CreateOrganelleLayout(cellDefinition, workMemory1, workMemory2);
            container.UpdateEngulfingSizeData(ref entity.Get<Engulfer>(), ref entity.Get<Engulfable>(),
                cellDefinition.IsBacteria);

            // Reproduction progress is lost
            container.AllOrganellesDivided = false;

            ref var reproduction = ref entity.Get<ReproductionStatus>();
            reproduction.SetupRequiredBaseReproductionCompounds(baseReproductionCostFrom);

            // Unbind if a colony's master cell removed its binding agent.
            if (!container.HasBindingAgent && entity.Has<MicrobeColony>())
            {
                var recorder = worldSimulation.StartRecordingEntityCommands();
                MicrobeColonyHelpers.UnbindAll(entity, recorder);

                worldSimulation.FinishRecordingEntityCommands(recorder);
            }

            ref var status = ref entity.Get<MicrobeStatus>();

            // Make chemoreception update happen immediately in case the settings changed so that new information is
            // used earlier
            status.TimeUntilChemoreceptionUpdate = 0;

            if (entity.Has<EarlyMulticellularSpeciesMember>())
            {
                ref var growth = ref entity.Get<MulticellularGrowth>();

                growth.ResetMulticellularProgress(entity, worldSimulation);
            }

            container.UpdateCompoundBagStorageFromOrganelles(ref storageToUpdate);

            container.RecalculateOrganelleBioProcesses(ref bioProcessesToUpdate);

            // Rescale health in case max health changed (for example the player picked a new membrane)
            ref var health = ref entity.Get<Health>();
            if (!health.Dead && health.CurrentHealth > 0 && health.MaxHealth > 0)
            {
                health.RescaleMaxHealth(HealthHelpers.CalculateMicrobeHealth(cellDefinition.MembraneType,
                    cellDefinition.MembraneRigidity));
            }
        }

        /// <summary>
        ///   Marks that the organelles have changed. Has to be called for things to be refreshed.
        /// </summary>
        public static void OnOrganellesChanged(this ref OrganelleContainer container, ref CompoundStorage storage,
            ref BioProcesses bioProcesses, ref Engulfer engulfer, ref Engulfable engulfable,
            ref CellProperties cellProperties)
        {
            container.OrganelleVisualsCreated = false;
            container.OrganelleComponentsCached = false;

            container.CalculateOrganelleLayoutStatistics();
            container.UpdateEngulfingSizeData(ref engulfer, ref engulfable, cellProperties.IsBacteria);
            container.UpdateCompoundBagStorageFromOrganelles(ref storage);

            container.RecalculateOrganelleBioProcesses(ref bioProcesses);
        }

        public static void RecalculateOrganelleBioProcesses(this ref OrganelleContainer container,
            ref BioProcesses bioProcesses)
        {
            if (container.Organelles != null)
            {
                ProcessSystem.ComputeActiveProcessList(container.Organelles.Organelles,
                    ref bioProcesses.ActiveProcesses);
            }
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
        ///   A list of tuples. Each tuple contains the type of compound, the color of the line (if any needs to be
        ///   drawn), and the location where the compound is located.
        /// </returns>
        public static List<(Compound Compound, Color Colour, Vector3 Target)>? PerformCompoundDetection(
            this ref OrganelleContainer container, in Entity entity, Vector3 position,
            IReadonlyCompoundClouds clouds)
        {
            HashSet<(Compound Compound, float Range, float MinAmount, Color Colour)> collectedUniqueCompoundDetections;

            // Colony lead cell uses all the chemoreceptors in the colony to make them all work
            if (entity.Has<MicrobeColony>())
            {
                ref var colony = ref entity.Get<MicrobeColony>();
                var collected = colony.CollectUniqueCompoundDetections();

                if (collected == null)
                    return null;

                collectedUniqueCompoundDetections = collected;
            }
            else
            {
                if (container.ActiveCompoundDetections == null)
                    return null;

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

        public static List<(Species Species, Entity Entity, Color Colour, Vector3 Target)>? PerformMicrobeDetections(
            this ref OrganelleContainer container, in Entity entity, Vector3 position,
            ISpeciesMemberLocationData microbePositionData)
        {
            HashSet<(Species Species, float Range, Color Colour)> collectedUniqueSpeciesDetections;

            if (entity.Has<MicrobeColony>())
            {
                ref var colony = ref entity.Get<MicrobeColony>();
                var collected = colony.CollectUniqueSpeciesDetections();

                if (collected == null)
                    return null;

                collectedUniqueSpeciesDetections = collected;
            }
            else
            {
                if (container.ActiveSpeciesDetections == null)
                    return null;

                collectedUniqueSpeciesDetections = container.ActiveSpeciesDetections;
            }

            var detections = new List<(Species Species, Entity Entity, Color Colour, Vector3 Target)>();

            foreach (var (species, range, colour) in collectedUniqueSpeciesDetections)
            {
                if (microbePositionData.FindSpeciesNearPoint(position, species, range, out var foundEntity,
                        out var foundPosition))
                {
                    detections.Add((species, foundEntity, colour, foundPosition));
                }
            }

            return detections;
        }

        public static void CalculateOrganelleLayoutStatistics(this ref OrganelleContainer container)
        {
            container.AvailableEnzymes?.Clear();
            container.AvailableEnzymes ??= new Dictionary<Enzyme, int>();

            // TODO: should the cached components (like slime jets) be cleared here? or is it better to keep the old
            // components around for a little bit?
            // container.SlimeJets?.Clear(); etc...

            container.OrganelleComponentsCached = false;

            // Cells have a minimum of at least one unit of lipase enzyme
            container.AvailableEnzymes[Lipase.Value] = 1;

            container.AgentVacuoleCount = 0;
            container.OrganellesCapacity = 0;
            container.HasSignalingAgent = false;
            container.HasBindingAgent = false;

            if (container.Organelles == null)
                throw new InvalidOperationException("Organelle list needs to be initialized first");

            container.HexCount = container.Organelles.HexCount;

            foreach (var organelle in container.Organelles)
            {
                foreach (var organelleComponent in organelle.Components)
                {
                    if (organelleComponent is AgentVacuoleComponent)
                    {
                        ++container.AgentVacuoleCount;
                    }
                    else if (organelleComponent is SlimeJetComponent slimeJetComponent)
                    {
                        container.SlimeJets ??= new List<SlimeJetComponent>();
                        container.SlimeJets.Add(slimeJetComponent);
                    }
                    else if (organelleComponent is MovementComponent thrustComponent)
                    {
                        container.ThrustComponents ??= new List<MovementComponent>();
                        container.ThrustComponents.Add(thrustComponent);
                    }
                    else if (organelleComponent is CiliaComponent rotationComponent)
                    {
                        container.RotationComponents ??= new List<CiliaComponent>();
                        container.RotationComponents.Add(rotationComponent);
                    }
                }

                if (organelle.Definition.HasSignalingFeature)
                    container.HasSignalingAgent = true;

                if (organelle.Definition.HasBindingFeature)
                    container.HasBindingAgent = true;

                container.OrganellesCapacity +=
                    MicrobeInternalCalculations.GetNominalCapacityForOrganelle(organelle.Definition,
                        organelle.Upgrades);

                var enzymes = organelle.GetEnzymes();

                if (enzymes.Count > 0)
                {
                    foreach (var enzyme in enzymes)
                    {
                        // Filter out invalid enzyme values
                        if (enzyme.Value <= 0)
                        {
                            if (enzyme.Value < 0)
                            {
                                GD.PrintErr("Enzyme amount in organelle is negative");
                            }

                            continue;
                        }

                        container.AvailableEnzymes.TryGetValue(enzyme.Key, out var existing);
                        container.AvailableEnzymes[enzyme.Key] = existing + enzyme.Value;
                    }
                }
            }
        }

        public static void UpdateEngulfingSizeData(this ref OrganelleContainer container,
            ref Engulfer engulfer, ref Engulfable engulfable, bool isBacteria)
        {
            float multiplier = 1;

            // Eukaryotic size increase to buff engulfing to match the visual size
            if (!isBacteria)
            {
                multiplier = Constants.EUKARYOTIC_ENGULF_SIZE_MULTIPLIER;
            }

            engulfer.EngulfingSize = container.HexCount * multiplier;
            engulfer.EngulfStorageSize = container.HexCount * multiplier;

            engulfable.BaseEngulfSize = container.HexCount * multiplier;
        }

        /// <summary>
        ///   Updates the <see cref="CompoundBag"/> of a microbe to account for changes in organelles.
        /// </summary>
        /// <param name="container">Organelle data</param>
        /// <param name="compoundStorage">Target compound storage to update</param>
        public static void UpdateCompoundBagStorageFromOrganelles(this ref OrganelleContainer container,
            ref CompoundStorage compoundStorage)
        {
            if (container.Organelles == null)
                throw new InvalidOperationException("Organelle list needs to be initialized first");

            var compounds = compoundStorage.Compounds;

            compounds.NominalCapacity = container.OrganellesCapacity;

            MicrobeInternalCalculations.UpdateSpecificCapacities(compounds, container.Organelles);
        }

        /// <summary>
        ///   Finds the organelle components that are needed from the outside of the organelles and stores them in the
        ///   lists in the container component. Updated by a system after
        /// </summary>
        public static void FetchLayoutOrganelleComponents(this ref OrganelleContainer container)
        {
            container.SlimeJets?.Clear();
            container.ThrustComponents?.Clear();
            container.RotationComponents?.Clear();

            // This method can be safely called again if this happened to run too early
            if (container.Organelles == null)
                return;

            foreach (var organelle in container.Organelles)
            {
                foreach (var organelleComponent in organelle.Components)
                {
                    if (organelleComponent is SlimeJetComponent slimeJetComponent)
                    {
                        container.SlimeJets ??= new List<SlimeJetComponent>();
                        container.SlimeJets.Add(slimeJetComponent);
                    }
                    else if (organelleComponent is MovementComponent thrustComponent)
                    {
                        container.ThrustComponents ??= new List<MovementComponent>();
                        container.ThrustComponents.Add(thrustComponent);
                    }
                    else if (organelleComponent is CiliaComponent rotationComponent)
                    {
                        container.RotationComponents ??= new List<CiliaComponent>();
                        container.RotationComponents.Add(rotationComponent);
                    }
                }
            }

            container.OrganelleComponentsCached = true;
        }

        /// <summary>
        ///   Calculates the reproduction progress for a cell, used to show how close the player is getting to
        ///   the editor.
        /// </summary>
        /// <returns>The total reproduction progress</returns>
        public static float CalculateReproductionProgress(this ref OrganelleContainer organelleContainer,
            ref ReproductionStatus reproductionStatus, ref SpeciesMember speciesMember, in Entity entity,
            CompoundBag storedCompounds, WorldGenerationSettings worldSettings,
            Dictionary<Compound, float> gatheredCompounds, Dictionary<Compound, float> totalCompounds)
        {
            // Calculate total compounds needed to split all organelles
            organelleContainer.CalculateTotalReproductionCompounds(entity, speciesMember.Species, totalCompounds);

            // Calculate how many compounds the cell already has absorbed to grow
            organelleContainer.CalculateAlreadyAbsorbedCompounds(ref entity.Get<ReproductionStatus>(),
                entity, speciesMember.Species, gatheredCompounds);

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
        public static void CalculateTotalReproductionCompounds(this ref OrganelleContainer organelleContainer,
            in Entity entity, Species species, Dictionary<Compound, float> result)
        {
            // Multicellular species need to show their total body plan compounds. Other cells and even colonies just
            // use the normal progress calculation for a single cell.
            if (entity.Has<MulticellularGrowth>())
            {
                ref var growth = ref entity.Get<MulticellularGrowth>();

                // TODO: check that this is set to null in all the right places
                if (growth.TotalNeededForMulticellularGrowth == null)
                    growth.CalculateTotalBodyPlanCompounds(species);

                result.Clear();

                result.Merge(growth.TotalNeededForMulticellularGrowth ??
                    throw new Exception("Total body plan compounds calculation failed"));

                return;
            }

            organelleContainer.CalculateNonDuplicateOrganelleInitialCompositionTotals(result);

            result.Merge(species.BaseReproductionCost);
        }

        public static void CalculateNonDuplicateOrganelleInitialCompositionTotals(
            this ref OrganelleContainer organelleContainer, Dictionary<Compound, float> result)
        {
            if (organelleContainer.Organelles == null)
                throw new InvalidOperationException("OrganelleContainer must be initialized first");

            result.Clear();

            foreach (var organelle in organelleContainer.Organelles)
            {
                if (organelle.IsDuplicate)
                    continue;

                result.Merge(organelle.Definition.InitialComposition);
            }
        }

        /// <summary>
        ///   Calculates how much compounds organelles have already absorbed
        /// </summary>
        public static void CalculateAlreadyAbsorbedCompounds(this ref OrganelleContainer organelleContainer,
            ref ReproductionStatus baseReproductionInfo,
            in Entity entity, Species species, Dictionary<Compound, float> result)
        {
            if (organelleContainer.Organelles == null)
                throw new InvalidOperationException("OrganelleContainer must be initialized first");

            result.Clear();

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

            if (entity.Has<MulticellularGrowth>())
            {
                ref var multicellularGrowth = ref entity.Get<MulticellularGrowth>();

                if (multicellularGrowth.CompoundsUsedForMulticellularGrowth != null)
                    result.Merge(multicellularGrowth.CompoundsUsedForMulticellularGrowth);
            }
            else
            {
                // For single microbes the base reproduction cost needs to be calculated here
                baseReproductionInfo.CalculateAlreadyUsedBaseReproductionCompounds(species, result);
            }
        }

        public static float CalculateCellEntityWeight(int organelleCount)
        {
            return Constants.MICROBE_BASE_ENTITY_WEIGHT + organelleCount * Constants.ORGANELLE_ENTITY_WEIGHT;
        }
    }
}
