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
    private readonly List<Species>? extraSpecies;
    private readonly List<Species>? excludedSpecies;
    private readonly bool collectEnergyInfo;

    public CalculatePopulation(IAutoEvoConfiguration configuration, WorldGenerationSettings worldSettings,
        PatchMap map, List<Species>? extraSpecies = null, List<Species>? excludedSpecies = null,
        bool collectEnergyInfo = false)
    {
        this.configuration = configuration;
        this.worldSettings = worldSettings;
        this.map = map;
        this.extraSpecies = extraSpecies;
        this.excludedSpecies = excludedSpecies;
        this.collectEnergyInfo = collectEnergyInfo;
    }

    public int TotalSteps => 1;

    public bool CanRunConcurrently { get; set; } = true;

    public bool RunStep(RunResults results)
    {
        // ReSharper disable RedundantArgumentDefaultValue
        var config = new SimulationConfiguration(configuration, map, worldSettings)
        {
            Results = results,
            CollectEnergyInformation = collectEnergyInfo,
        };

        // ReSharper restore RedundantArgumentDefaultValue

        if (extraSpecies != null)
            config.ExtraSpecies = extraSpecies;

        if (excludedSpecies != null)
            config.ExcludedSpecies = excludedSpecies;

        // Directly feed the population results to the main results object

        // TODO: allow passing in a random seed

        // PopulationSimulation.Simulate(config, null, new XoShiRo256starstar());

        MichePopulation.Simulate(config, null, new XoShiRo256starstar());

        return true;
    }
}
