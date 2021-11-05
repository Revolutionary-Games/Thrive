namespace AutoEvo
{
    using System;
    using System.Collections.Generic;

    /// <summary>
    ///   Configuration for a run of the population simulation part of auto-evo
    /// </summary>
    public class SimulationConfiguration
    {
        public SimulationConfiguration(PatchMap initialConditions, int steps = 1)
        {
            OriginalMap = initialConditions;
            StepsLeft = Math.Max(1, steps);
        }

        public PatchMap OriginalMap { get; }
        public int StepsLeft { get; set; }

        /// <summary>
        ///   Results of the run are stored here
        /// </summary>
        public RunResults Results { get; set; } = new RunResults();

        /// <summary>
        ///   List of species to ignore in the map for simulation.
        /// </summary>
        /// <remarks>
        ///   <para>
        ///     If there is an extra species matching index of an entry here, the population of the species here is used
        ///     for the initial population of the extra species.
        ///   </para>
        /// </remarks>
        /// <value>The excluded species.</value>
        public List<Species> ExcludedSpecies { get; set; } = new List<Species>();

        /// <summary>
        ///   List of extra species to simulate in addition to the ones in the map.
        /// </summary>
        /// <remarks>
        ///   <para>
        ///     If a population for the extra species is not found through ExcludedSpecies the global population from
        ///     the Species object is used for all patches.
        ///   </para>
        /// </remarks>
        /// <value>The extra species.</value>
        public List<Species> ExtraSpecies { get; set; } = new List<Species>();

        /// <summary>
        ///   Migrations to apply before running the simulation.
        /// </summary>
        public List<Tuple<Species, SpeciesMigration>> Migrations { get; set; } =
            new List<Tuple<Species, SpeciesMigration>>();

        /// <summary>
        ///   If not empty, only the specified patches are ran
        /// </summary>
        /// <remarks>
        ///    <para>
        ///      TODO: change migration finding and mutation finding to only simulate patches they need
        ///    </para>
        /// </remarks>
        public ISet<Patch> PatchesToRun { get; set; } = new HashSet<Patch>();
    }
}
