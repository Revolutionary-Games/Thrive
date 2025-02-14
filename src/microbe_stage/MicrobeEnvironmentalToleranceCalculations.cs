using System;
using AutoEvo;

/// <summary>
///   Helper class that contains all the math for environmental tolerances in one place (though the microbe editor and
///   especially <see cref="TolerancesEditorSubComponent"/> has some extra range checking code, which if changed here
///   must be changed there as well)
/// </summary>
public static class MicrobeEnvironmentalToleranceCalculations
{
    /// <summary>
    ///   Calculates the total overall score of environmental tolerance without giving the sub-scores
    /// </summary>
    /// <param name="species">Species to calculate for</param>
    /// <param name="environment">Environment the species is in</param>
    /// <param name="cache">Cache for speeding up the calculation</param>
    /// <returns>
    ///   The overall score, which ranges from 0 (not surviving at all) to 1 (well adapted),
    ///   and over 1 (for perfectly adapted, which gives bonuses)
    /// </returns>
    public static float CalculateTotalToleranceScore(MicrobeSpecies species, BiomeConditions environment,
        SimulationCache cache)
    {
        var score = CalculateTolerances(species, environment, cache);
        return score.OverallScore;
    }

    public static ToleranceResult CalculateTolerances(MicrobeSpecies species, BiomeConditions environment,
        SimulationCache cache)
    {
        var tolerance = species.Tolerances;
        var result = new ToleranceResult();

        var patchTemperature = environment.GetCompound(Compound.Temperature, CompoundAmountType.Biome).Ambient;
        var patchPressure = environment.Pressure;
        var requiredOxygenResistance = environment.CalculateOxygenResistanceFactor();
        var requiredUVResistance = environment.CalculateUVFactor();

        bool missingSomething = false;

        // Always write the targets for becoming perfectly adapted
        result.PerfectTemperatureAdjustment = patchTemperature - tolerance.PreferredTemperature;
        result.PerfectPressureAdjustment = patchPressure - tolerance.PreferredPressure;
        result.PerfectOxygenAdjustment = requiredOxygenResistance - tolerance.OxygenResistance;
        result.PerfectUVAdjustment = requiredUVResistance - tolerance.UVResistance;

        // Temperature
        if (patchTemperature > tolerance.PreferredTemperature + tolerance.TemperatureTolerance ||
            patchTemperature < tolerance.PreferredTemperature - tolerance.TemperatureTolerance)
        {
            // Not adapted to the temperature
            var adjustmentSize = Math.Abs(result.PerfectTemperatureAdjustment);

            if (adjustmentSize > Constants.TOLERANCE_MAXIMUM_SURVIVABLE_TEMPERATURE_DIFFERENCE)
            {
                result.TemperatureScore = 0;
            }
            else
            {
                result.TemperatureScore =
                    1 - adjustmentSize / Constants.TOLERANCE_MAXIMUM_SURVIVABLE_TEMPERATURE_DIFFERENCE;
            }

            missingSomething = true;
        }
        else if (tolerance.TemperatureTolerance <= Constants.TOLERANCE_PERFECT_THRESHOLD_TEMPERATURE)
        {
            // Perfectly adapted
            var perfectionFactor = Constants.TOLERANCE_PERFECT_TEMPERATURE_SCORE *
                (1 - (tolerance.TemperatureTolerance / Constants.TOLERANCE_PERFECT_THRESHOLD_TEMPERATURE));
            result.TemperatureScore = 1 + perfectionFactor;
        }
        else
        {
            // Adequately adapted, but could be made perfect
            result.TemperatureRangeSizeAdjustment =
                tolerance.TemperatureTolerance - Constants.TOLERANCE_PERFECT_THRESHOLD_TEMPERATURE;

            result.TemperatureScore = 1;
        }

        // Pressure
        if (patchPressure > tolerance.PressureMaximum)
        {
            // Too high pressure

            if (patchPressure - tolerance.PressureMaximum > Constants.TOLERANCE_MAXIMUM_SURVIVABLE_PRESSURE_DIFFERENCE)
            {
                result.PressureScore = 0;
            }
            else
            {
                result.PressureScore = 1 - (patchPressure - tolerance.PressureMaximum) /
                    Constants.TOLERANCE_MAXIMUM_SURVIVABLE_PRESSURE_DIFFERENCE;
            }

            missingSomething = true;
        }
        else if (patchPressure < tolerance.PressureMinimum)
        {
            // Too low pressure
            if (tolerance.PressureMinimum - patchPressure > Constants.TOLERANCE_MAXIMUM_SURVIVABLE_PRESSURE_DIFFERENCE)
            {
                result.PressureScore = 0;
            }
            else
            {
                result.PressureScore = 1 - (tolerance.PressureMinimum - patchPressure) /
                    Constants.TOLERANCE_MAXIMUM_SURVIVABLE_PRESSURE_DIFFERENCE;
            }

            missingSomething = true;
        }
        else
        {
            var range = Math.Abs(tolerance.PressureMaximum - tolerance.PressureMinimum);

            if (range <=
                Constants.TOLERANCE_PERFECT_THRESHOLD_PRESSURE)
            {
                // Perfectly adapted
                var perfectionFactor = Constants.TOLERANCE_PERFECT_PRESSURE_SCORE *
                    (1 - (range / Constants.TOLERANCE_PERFECT_THRESHOLD_PRESSURE));
                result.PressureScore = 1 + perfectionFactor;
            }
            else
            {
                // Adequately adapted, but could be made perfect
                result.PressureRangeSizeAdjustment =
                    range - Constants.TOLERANCE_PERFECT_THRESHOLD_PRESSURE;

                result.PressureScore = 1;
            }
        }

        // Oxygen Resistance
        if (tolerance.OxygenResistance < requiredOxygenResistance)
        {
            // Not adapted to the oxygen requirement
            result.OxygenScore = 1 - result.PerfectOxygenAdjustment;
            missingSomething = true;
        }
        else
        {
            result.OxygenScore = 1;
        }

        // UV Resistance
        if (tolerance.UVResistance < requiredUVResistance)
        {
            // Not adapted to the UV requirement
            result.UVScore = 1 - result.PerfectUVAdjustment;
            missingSomething = true;
        }
        else
        {
            result.UVScore = 1;
        }

        // Make overall score an average of all values
        result.OverallScore =
            (result.TemperatureScore + result.PressureScore + result.OxygenScore + result.UVScore) / 4;

        // But if missing something, ensure the score is not 1
        if (missingSomething && result.OverallScore >= 1)
        {
            // TODO: maybe a lower value here or some other adjustment?
            result.OverallScore = 0.99f;
        }

        return result;
    }

    public static void GenerateToleranceProblemList(ToleranceResult data, Action<string> resultCallback)
    {
        if (data.TemperatureScore < 1)
        {
            throw new NotImplementedException();
        }

        if (data.PressureScore < 1)
        {
            throw new NotImplementedException();
        }

        if (data.OxygenScore < 1)
        {
            throw new NotImplementedException();
        }

        if (data.UVScore < 1)
        {
            throw new NotImplementedException();
        }
    }

    public class ToleranceResult
    {
        public float OverallScore;

        public float TemperatureScore;
        public float PerfectTemperatureAdjustment;
        public float TemperatureRangeSizeAdjustment;

        public float PressureScore;
        public float PerfectPressureAdjustment;
        public float PressureRangeSizeAdjustment;

        public float OxygenScore;
        public float PerfectOxygenAdjustment;

        public float UVScore;
        public float PerfectUVAdjustment;
    }
}
