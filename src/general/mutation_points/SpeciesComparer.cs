using System;

/// <summary>
///   Compares base Species' properties for MP cost calculations
/// </summary>
public static class SpeciesComparer
{
    public static double GetRequiredMutationPoints(IReadOnlySpecies speciesA, IReadOnlySpecies speciesB)
    {
        // Behaviour comparison once it costs MP
        // speciesA.Behaviour

        double cost = 0;

        cost += CalculateToleranceCost(speciesA.Tolerances, speciesB.Tolerances);

        // Apparently there are no other base species changes that cost MP currently...

        return cost;
    }

    public static double CalculateToleranceCost(IReadOnlyEnvironmentalTolerances oldTolerances,
        IReadOnlyEnvironmentalTolerances newTolerances)
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
        var maximumPressureChange = Math.Abs(oldTolerances.PressureMaximum - newTolerances.PressureMaximum);

        // As moving one slider can end up changing the other value as well, we take the average of the change to take
        // that implicit doubled cost into account
        var totalPressureChangeAverage = (maximumPressureChange + minimumPressureChange) * 0.5;

        // Calculate pressure tolerance range change
        var oldRange = Math.Abs(oldTolerances.PressureMaximum - oldTolerances.PressureMinimum);
        var newRange = Math.Abs(newTolerances.PressureMaximum - newTolerances.PressureMinimum);
        var pressureToleranceChange = Math.Abs(oldRange - newRange);

        // Then add up the costs based on the changes
        return temperatureChange * Constants.TOLERANCE_CHANGE_MP_PER_TEMPERATURE +
            temperatureToleranceChange * Constants.TOLERANCE_CHANGE_MP_PER_TEMPERATURE_TOLERANCE +
            totalPressureChangeAverage * Constants.TOLERANCE_CHANGE_MP_PER_PRESSURE +
            pressureToleranceChange * Constants.TOLERANCE_CHANGE_MP_PER_PRESSURE_TOLERANCE +
            oxygenChange * Constants.TOLERANCE_CHANGE_MP_PER_OXYGEN +
            uvChange * Constants.TOLERANCE_CHANGE_MP_PER_UV;
    }
}
