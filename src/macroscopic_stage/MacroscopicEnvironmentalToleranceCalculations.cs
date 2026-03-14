/// <summary>
///   Macroscopic variant of <see cref="MicrobeEnvironmentalToleranceCalculations"/> that is specialized for bigger
///   creatures.
/// </summary>
public static class MacroscopicEnvironmentalToleranceCalculations
{
    public static ToleranceResult CalculateTolerances(EnvironmentalTolerances speciesTolerances,
        MetaballLayout<MacroscopicMetaball> metaballs, IBiomeConditions environment, bool excludePositiveBuffs = false)
    {
        // TODO: macroscopic tolerances
        return new ToleranceResult
        {
            OverallScore = 1,
            OxygenScore = 1,
            TemperatureScore = 1,
            PressureScore = 1,
            UVScore = 1,
        };
    }
}
