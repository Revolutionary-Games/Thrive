/// <summary>
///   Auto-evo configuration parameters to be used
/// </summary>
[SupportsCustomizedRegistryType(typeof(AutoEvoConfiguration))]
public interface IAutoEvoConfiguration : IRegistryAssignable
{
    public int MutationsPerSpecies { get; }

    public int MoveAttemptsPerSpecies { get; }

    public bool StrictNicheCompetition { get; }
}

public static class AutoEvoConfigurationHelpers
{
    public static void Check(this IAutoEvoConfiguration configuration)
    {
        if (configuration.MutationsPerSpecies < 0)
        {
            throw new InvalidRegistryDataException("AutoEvoConfiguration", configuration.GetType().Name,
                "Mutations per species must be positive");
        }

        if (configuration.MoveAttemptsPerSpecies < 0)
        {
            throw new InvalidRegistryDataException("AutoEvoConfiguration", configuration.GetType().Name,
                "Move attempts per species must be positive");
        }
    }

    public static AutoEvoConfiguration Clone(this IAutoEvoConfiguration configuration)
    {
        return new AutoEvoConfiguration
        {
            MoveAttemptsPerSpecies = configuration.MoveAttemptsPerSpecies,
            MutationsPerSpecies = configuration.MutationsPerSpecies,
            StrictNicheCompetition = configuration.StrictNicheCompetition,
        };
    }
}
