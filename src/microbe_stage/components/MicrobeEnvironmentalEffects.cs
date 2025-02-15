namespace Components;

/// <summary>
///   Has most of the variables controlling environmental effects on gameplay microbes
/// </summary>
[JSONDynamicTypeAllowed]
[ComponentIsReadByDefault]
public struct MicrobeEnvironmentalEffects
{
    public float OsmoregulationMultiplier;

    public float HealthMultiplier;

    /// <summary>
    ///   This is a copy of <see cref="BioProcesses.OverallSpeedModifier"/> to be able to transfer this data along
    ///   easily
    /// </summary>
    public float ProcessSpeedModifier;
}

public static class MicrobeEnvironmentalEffectsHelpers
{
    /// <summary>
    ///   Applies data from calculated tolerance data to components. Note that this doesn't refresh maximum health in
    ///   case that changes (if it has been initialised already)
    /// </summary>
    /// <param name="effects">Where to put the primary data on the effects</param>
    /// <param name="toleRanceData">Resolved tolerance data</param>
    /// <param name="bioProcesses">Additional state is applied here</param>
    public static void ApplyEffects(this ref MicrobeEnvironmentalEffects effects,
        in ResolvedMicrobeTolerances toleRanceData, ref BioProcesses bioProcesses)
    {
        effects.OsmoregulationMultiplier = toleRanceData.OsmoregulationModifier;

        effects.HealthMultiplier = toleRanceData.HealthModifier;

        effects.ProcessSpeedModifier = toleRanceData.ProcessSpeedModifier;
        bioProcesses.OverallSpeedModifier = toleRanceData.ProcessSpeedModifier;
    }
}
