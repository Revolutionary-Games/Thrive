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

    public override bool WantsMergeWith(CombinableActionData other)
    {
        // These must always merge with other tolerance actions, because otherwise the undo history step count is going
        // to explore
        return other is ToleranceActionData;
    }

    protected override double CalculateCostInternal()
    {
        // Calculate all changes
        var temperatureChange = Math.Abs(OldTolerances.PreferredTemperature - NewTolerances.PreferredTemperature);
        var temperatureToleranceChange =
            Math.Abs(OldTolerances.TemperatureTolerance - NewTolerances.TemperatureTolerance);
        double pressureChange = Math.Abs(OldTolerances.PreferredPressure - NewTolerances.PreferredPressure);
        double minPressureChange = Math.Abs(OldTolerances.PressureMinimum - NewTolerances.PressureMinimum);
        double maxPressureChange = Math.Abs(OldTolerances.PressureMaximum - NewTolerances.PressureMaximum);

        // Calculate pressure range change as moving of the middle point (so scale the total change to half as the same
        // change moves the min and max points at the same time)
        var pressureToleranceChange = (minPressureChange + maxPressureChange) * 0.5;

        var oxygenChange = Math.Abs(OldTolerances.OxygenResistance - NewTolerances.OxygenResistance);
        var uvChange = Math.Abs(OldTolerances.UVResistance - NewTolerances.UVResistance);

        // Then add up the costs based on the changes
        return temperatureChange * Constants.TOLERANCE_CHANGE_MP_PER_TEMPERATURE +
            temperatureToleranceChange * Constants.TOLERANCE_CHANGE_MP_PER_TEMPERATURE_TOLERANCE +
            pressureChange * Constants.TOLERANCE_CHANGE_MP_PER_PRESSURE +
            pressureToleranceChange * Constants.TOLERANCE_CHANGE_MP_PER_PRESSURE_TOLERANCE +
            oxygenChange * Constants.TOLERANCE_CHANGE_MP_PER_OXYGEN +
            uvChange * Constants.TOLERANCE_CHANGE_MP_PER_UV;
    }

    protected override ActionInterferenceMode GetInterferenceModeWithGuaranteed(CombinableActionData other)
    {
        if (other is not ToleranceActionData otherTolerance)
            return ActionInterferenceMode.NoInterference;

        // If the value has been changed back to a previous value
        if (NewTolerances.EqualsApprox(otherTolerance.OldTolerances) &&
            otherTolerance.NewTolerances.EqualsApprox(OldTolerances))
        {
            return ActionInterferenceMode.CancelsOut;
        }

        // If the value has been changed twice
        if (NewTolerances.EqualsApprox(otherTolerance.OldTolerances) ||
            otherTolerance.NewTolerances.EqualsApprox(OldTolerances))
        {
            return ActionInterferenceMode.Combinable;
        }

        return ActionInterferenceMode.NoInterference;
    }

    protected override CombinableActionData CombineGuaranteed(CombinableActionData other)
    {
        var otherTolerance = (ToleranceActionData)other;

        if (OldTolerances.EqualsApprox(otherTolerance.NewTolerances))
            return new ToleranceActionData(NewTolerances, otherTolerance.OldTolerances);

        return new ToleranceActionData(otherTolerance.NewTolerances, OldTolerances);
    }

    protected override void MergeGuaranteed(CombinableActionData other)
    {
        var otherTolerance = (ToleranceActionData)other;

        if (OldTolerances.EqualsApprox(otherTolerance.NewTolerances))
        {
            // Handle cancels out
            if (NewTolerances.EqualsApprox(otherTolerance.OldTolerances))
            {
                NewTolerances = otherTolerance.NewTolerances;
                return;
            }

            OldTolerances = otherTolerance.OldTolerances;
            return;
        }

        NewTolerances = otherTolerance.NewTolerances;
    }
}
