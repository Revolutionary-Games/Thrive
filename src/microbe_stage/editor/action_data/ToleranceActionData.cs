using System;

[JSONAlwaysDynamicType]
public class ToleranceActionData : EditorCombinableActionData
{
    public EnvironmentalTolerances OldTolerances;
    public EnvironmentalTolerances NewTolerances;

    public ToleranceActionData(EnvironmentalTolerances oldTolerances, EnvironmentalTolerances newTolerances)
    {
        OldTolerances = oldTolerances;
        NewTolerances = newTolerances;
    }

    protected override int CalculateCostInternal()
    {
        // Calculate all changes
        var temperatureChange = Math.Abs(OldTolerances.PreferredTemperature - NewTolerances.PreferredTemperature);
        var temperatureToleranceChange =
            Math.Abs(OldTolerances.TemperatureTolerance - NewTolerances.TemperatureTolerance);
        double pressureChange = Math.Abs(OldTolerances.PreferredPressure - NewTolerances.PreferredPressure);
        double minPressureChange = Math.Abs(OldTolerances.PressureToleranceMin - NewTolerances.PressureToleranceMin);
        double maxPressureChange = Math.Abs(OldTolerances.PressureToleranceMax - NewTolerances.PressureToleranceMax);

        var pressureToleranceChange = (minPressureChange + maxPressureChange) * 0.5;

        var oxygenChange = Math.Abs(OldTolerances.OxygenResistance - NewTolerances.OxygenResistance);
        var uvChange = Math.Abs(OldTolerances.UVResistance - NewTolerances.UVResistance);

        // Then add up the costs based on the changes
        return (int)Math.Round(temperatureChange * Constants.TOLERANCE_CHANGE_MP_PER_TEMPERATURE +
            temperatureToleranceChange * Constants.TOLERANCE_CHANGE_MP_PER_TEMPERATURE_TOLERANCE +
            pressureChange * Constants.TOLERANCE_CHANGE_MP_PER_PRESSURE +
            pressureToleranceChange * Constants.TOLERANCE_CHANGE_MP_PER_PRESSURE_TOLERANCE +
            oxygenChange * Constants.TOLERANCE_CHANGE_MP_PER_OXYGEN +
            uvChange * Constants.TOLERANCE_CHANGE_MP_PER_UV);
    }

    protected override ActionInterferenceMode GetInterferenceModeWithGuaranteed(CombinableActionData other)
    {
        if (other is not ToleranceActionData toleranceActionData)
            return ActionInterferenceMode.NoInterference;

        // If changed back to original then the actions cancel out
        if (OldTolerances.Equals(toleranceActionData.NewTolerances))
            return ActionInterferenceMode.CancelsOut;

        return ActionInterferenceMode.Combinable;
    }

    protected override CombinableActionData CombineGuaranteed(CombinableActionData other)
    {
        var otherData = (ToleranceActionData)other;

        return new ToleranceActionData(OldTolerances, otherData.NewTolerances);
    }
}
