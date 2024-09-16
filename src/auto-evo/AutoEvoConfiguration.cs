/// <summary>
///   Settings for auto-evo that have been (potentially) tweaked
/// </summary>
[CustomizedRegistryType]
public class AutoEvoConfiguration : IAutoEvoConfiguration
{
    public int MutationsPerSpecies { get; set; }

    public int MoveAttemptsPerSpecies { get; set; }

    public bool StrictNicheCompetition { get; set; }
}
