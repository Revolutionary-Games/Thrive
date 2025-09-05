using System;
using System.Collections.Generic;

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

    public static double CalculateToleranceCost(EnvironmentalTolerances oldTolerances,
        EnvironmentalTolerances newTolerances)
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

    protected override double CalculateBaseCostInternal()
    {
        return CalculateToleranceCost(OldTolerances, NewTolerances);
    }

    protected override (double Cost, double RefundCost) CalculateCostInternal(
        IReadOnlyList<EditorCombinableActionData> history, int insertPosition)
    {
        bool foundOther = false;
        var cost = CalculateBaseCostInternal();
        double refund = 0;

        var count = history.Count;
        for (int i = 0; i < insertPosition && i < count; ++i)
        {
            var other = history[i];

            // Calculate total MP cost of all moves
            if (other is ToleranceActionData toleranceActionData)
            {
                if (!foundOther)
                {
                    foundOther = true;
                    cost = CalculateToleranceCost(toleranceActionData.OldTolerances, NewTolerances);
                }

                refund += other.GetCalculatedSelfCost() - other.GetCalculatedRefundCost();
            }
        }

        return (cost, refund);
    }

    protected override bool CanMergeWithInternal(CombinableActionData other)
    {
        // These must always merge with other tolerance actions, because otherwise the undo history step count is going
        // to explore
        // TODO: maybe shouldn't merge if separate sliders only are changed to make better undo history?
        return other is ToleranceActionData;
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
