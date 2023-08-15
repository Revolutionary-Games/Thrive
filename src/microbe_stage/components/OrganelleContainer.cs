namespace Components
{
    using System;
    using System.Collections.Generic;
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
    }

    public static class OrganelleContainerExtensions
    {
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

            if (container.Organelles == null)
                throw new InvalidOperationException("Organelle list needs to be initialized first");

            foreach (var organelle in container.Organelles)
            {
                if (organelle.HasComponent<AgentVacuoleComponent>())
                    ++container.AgentVacuoleCount;

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
