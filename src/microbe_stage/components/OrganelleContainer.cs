namespace Components
{
    using System;
    using System.Collections.Generic;
    using DefaultEcs;
    using Godot;
    using Newtonsoft.Json;
    using Systems;

    /// <summary>
    ///   Entity that contains <see cref="PlacedOrganelle"/>
    /// </summary>
    public struct OrganelleContainer
    {
        // probably can do with just having the CommandSignaler component (so this property is not required)
        // public bool HasSignalingAgent;

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

        // TODO: add the following variables only if really needed
        // private bool organelleMaxRenderPriorityDirty = true;
        // private int cachedOrganelleMaxRenderPriority;

        public bool HasSignalingAgent;

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

        // TODO: maybe put the process list refresh variable here and a some new system to regenerate the process list?
        // instead of just doing it when changing the organelles?
    }

    public static class OrganelleContainerExtensions
    {
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
            ref this OrganelleContainer container, in Entity entity, Vector3 position,
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

        public static void CreateOrganelleLayout(ref this OrganelleContainer container, ICellProperties cellProperties)
        {
            container.Organelles?.Clear();
            container.AllOrganellesDivided = false;

            container.Organelles ??= new OrganelleLayout<PlacedOrganelle>();

            foreach (var organelleTemplate in cellProperties.Organelles)
            {
                container.Organelles.Add(new PlacedOrganelle(organelleTemplate.Definition, organelleTemplate.Position,
                    organelleTemplate.Orientation));
            }

            container.CalculateOrganelleLayoutStatistics();

            container.OrganelleVisualsCreated = false;
        }

        public static void CalculateOrganelleLayoutStatistics(ref this OrganelleContainer container)
        {
            container.AvailableEnzymes?.Clear();
            container.AvailableEnzymes ??= new Dictionary<Enzyme, int>();

            container.AgentVacuoleCount = 0;
            container.OrganellesCapacity = 0;
            container.HasSignalingAgent = false;

            if (container.Organelles == null)
                throw new InvalidOperationException("Organelle list needs to be initialized first");

            foreach (var organelle in container.Organelles)
            {
                if (organelle.HasComponent<AgentVacuoleComponent>())
                    ++container.AgentVacuoleCount;

                if (organelle.HasComponent<SignalingAgentComponent>())
                    container.HasSignalingAgent = true;

                container.OrganellesCapacity = organelle.StorageCapacity;

                if (organelle.StoredEnzymes.Count > 0)
                {
                    foreach (var enzyme in organelle.StoredEnzymes)
                    {
                        container.AvailableEnzymes.TryGetValue(enzyme.Key, out var existing);
                        container.AvailableEnzymes[enzyme.Key] = existing + enzyme.Value;
                    }
                }
            }

            // TODO: slime jets implementation
        }
    }
}
