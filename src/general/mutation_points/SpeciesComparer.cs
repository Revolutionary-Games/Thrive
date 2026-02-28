using System;

/// <summary>
///   Compares base Species' properties for MP cost calculations
/// </summary>
public static class SpeciesComparer
{
    public static double GetRequiredMutationPoints(IReadOnlySpecies speciesA, IReadOnlySpecies speciesB,
        double maxSingleActionCost, double costMultiplier = 1)
    {
        // Behaviour comparison once it costs MP
        // speciesA.Behaviour

        double cost = 0;

        cost += CalculateToleranceCost(speciesA.Tolerances, speciesB.Tolerances, maxSingleActionCost, costMultiplier);

        // Apparently there are no other base species changes that cost MP currently...

        return cost;
    }

    public static double CalculateToleranceCost(IReadOnlyEnvironmentalTolerances oldTolerances,
        IReadOnlyEnvironmentalTolerances newTolerances, double maxSingleActionCost, double costMultiplier = 1)
    {
        // Calculate all changes
        var temperatureChange = Math.Abs(oldTolerances.PreferredTemperature - newTolerances.PreferredTemperature);
        var temperatureToleranceChange =
            Math.Abs(oldTolerances.TemperatureTolerance - newTolerances.TemperatureTolerance);
        var oxygenChange = Math.Abs(oldTolerances.OxygenResistance - newTolerances.OxygenResistance);
        var uvChange = Math.Abs(oldTolerances.UVResistance - newTolerances.UVResistance);

        // Pressure change is slightly tricky to calculate as from a pair of numbers we need to create 2 linked but
        // separate costs
        var minimumPressureChange = Math.Abs(oldTolerances.PressureMinimum - newTolerances.PressureMinimum);
        var pressureToleranceChange = Math.Abs(oldTolerances.PressureTolerance - newTolerances.PressureTolerance);

        // Then add up the costs based on the changes
        // TODO: this can't apply the max single action cost as otherwise *all* tolerance changes could be done at once
        // as long as the player sacrificed all of their MP to do it. To fix this we would need to make it so that a
        // single slider step is clamped to the max cost and then that is multiplied with the change. We don't know
        // the slider steps here so for now we can't do that. But we need the parameter to be able to implement that
        // in the future.
        _ = maxSingleActionCost;

        return temperatureChange * Constants.TOLERANCE_CHANGE_MP_PER_TEMPERATURE * costMultiplier +
            temperatureToleranceChange * Constants.TOLERANCE_CHANGE_MP_PER_TEMPERATURE_TOLERANCE * costMultiplier +
            minimumPressureChange * Constants.TOLERANCE_CHANGE_MP_PER_PRESSURE_MINIMUM * costMultiplier +
            pressureToleranceChange * Constants.TOLERANCE_CHANGE_MP_PER_PRESSURE_TOLERANCE * costMultiplier +
            oxygenChange * Constants.TOLERANCE_CHANGE_MP_PER_OXYGEN * costMultiplier +
            uvChange * Constants.TOLERANCE_CHANGE_MP_PER_UV * costMultiplier;
    }
}
