namespace AutoEvo;

using System;
using System.Collections.Generic;

/// <summary>
///   Configuration for a run of the population simulation part of auto-evo
/// </summary>
public class SimulationConfiguration
{
    public SimulationConfiguration(IAutoEvoConfiguration autoEvoConfiguration, PatchMap initialConditions,
        WorldGenerationSettings worldSettings, int steps = 1)
    {
        AutoEvoConfiguration = autoEvoConfiguration;
        OriginalMap = initialConditions;
        WorldSettings = worldSettings;
        StepsLeft = Math.Max(1, steps);
    }

    public IAutoEvoConfiguration AutoEvoConfiguration { get; }
    public PatchMap OriginalMap { get; }
    public WorldGenerationSettings WorldSettings { get; }
    public int StepsLeft { get; set; }

    /// <summary>
    ///   Results of the run are stored here
    /// </summary>
    public RunResults Results { get; set; } = new();

    /// <summary>
    ///   Dictionary of species replacements to perform before running the simulation.
    /// </summary>
    /// <remarks>
    ///   <para>
    ///     The keys in this dictionary are the old species, which when processed are instead immediately replaced by
    ///     their respective values. So this is a mapping from an old species to a new one that should be used instead
    ///     of the old one in the simulation.
    ///   </para>
    /// </remarks>
    /// <value>The extra species.</value>
    public Dictionary<Species, Species> ReplacedSpecies { get; set; } = new();

    /// <summary>
    ///   Migrations to apply before running the simulation.
    /// </summary>
    public List<Tuple<Species, SpeciesMigration>> Migrations { get; set; } = new();

    /// <summary>
    ///   If not empty, only the specified patches are ran
    /// </summary>
    public ISet<Patch> PatchesToRun { get; set; } = new HashSet<Patch>();

    /// <summary>
    ///   If set to true then species energy sources will be stored for display to the player
    /// </summary>
    public bool CollectEnergyInformation { get; set; }

    /// <summary>
    ///   Sets the patches to be simulated to be ones where the species is present (population > 0)
    /// </summary>
    /// <param name="species">The species to check for in the <see cref="OriginalMap"/> patches</param>
    public void SetPatchesToRunBySpeciesPresence(Species species)
    {
        PatchesToRun.Clear();

        foreach (var patchEntry in OriginalMap.Patches)
        {
            if (patchEntry.Value.GetSpeciesSimulationPopulation(species) > 0)
            {
                PatchesToRun.Add(patchEntry.Value);
            }
        }
    }
}
