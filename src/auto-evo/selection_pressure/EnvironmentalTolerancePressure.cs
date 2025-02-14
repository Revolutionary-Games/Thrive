namespace AutoEvo;

using Newtonsoft.Json;

/// <summary>
///   Makes sure species need to be adapted well enough to the environmental conditions in their patch to survive. Also
///   has the part to generate mutations to better match the environment.
/// </summary>
[JSONDynamicTypeAllowed]
public class EnvironmentalTolerancePressure : SelectionPressure
{
    // Needed for translation extraction
    // ReSharper disable ArrangeObjectCreationWhenTypeEvident
    private static readonly LocalizedString NameString = new LocalizedString("MICHE_ENVIRONMENTAL_TOLERANCE");

    // ReSharper restore ArrangeObjectCreationWhenTypeEvident

    public EnvironmentalTolerancePressure(float weight) : base(weight, [new ModifyEnvironmentalTolerance()])
    {
    }

    [JsonIgnore]
    public override LocalizedString Name => NameString;

    public override float Score(Species species, Patch patch, SimulationCache cache)
    {
        if (species is not MicrobeSpecies microbeSpecies)
            return 0;

        // Use scores to encourage species to be adapted to their environment
        return MicrobeEnvironmentalToleranceCalculations.CalculateTotalToleranceScore(microbeSpecies, patch.Biome,
            cache);
    }

    public override float GetEnergy(Patch patch)
    {
        return 0;
    }
}
