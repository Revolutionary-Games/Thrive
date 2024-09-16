namespace AutoEvo;

using System.Collections.Generic;
using Xoshiro.PRNG64;

/// <summary>
///   Step that calculate the populations for all species
/// </summary>
public class CalculatePopulation : IRunStep
{
    private readonly IAutoEvoConfiguration configuration;
    private readonly PatchMap map;
    private readonly WorldGenerationSettings worldSettings;
    private readonly Dictionary<Species, Species>? replaceSpecies;
    private readonly bool collectEnergyInfo;

    public CalculatePopulation(IAutoEvoConfiguration configuration, WorldGenerationSettings worldSettings,
        PatchMap map, Dictionary<Species, Species>? replaceSpecies = null, bool collectEnergyInfo = false)
    {
        this.configuration = configuration;
        this.worldSettings = worldSettings;
        this.map = map;
        this.replaceSpecies = replaceSpecies;
        this.collectEnergyInfo = collectEnergyInfo;
    }

    public int TotalSteps => 1;

    public bool CanRunConcurrently => false;

    public bool RunStep(RunResults results)
    {
        // ReSharper disable RedundantArgumentDefaultValue
        var config = new SimulationConfiguration(configuration, map, worldSettings)
        {
            Results = results,
            CollectEnergyInformation = collectEnergyInfo,
        };

        // ReSharper restore RedundantArgumentDefaultValue

        if (replaceSpecies != null)
        {
            foreach (var entry in replaceSpecies)
            {
                config.ReplacedSpecies.Add(entry.Key, entry.Value);
            }
        }

        // Directly feed the population results to the main results object

        // TODO: allow passing in a random seed

        MichePopulation.Simulate(config, null, new XoShiRo256starstar());

        return true;
    }
}
