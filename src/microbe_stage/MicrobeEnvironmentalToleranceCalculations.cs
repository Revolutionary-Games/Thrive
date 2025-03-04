using System;
using System.Collections.Generic;
using System.Diagnostics;

/// <summary>
///   Helper class that contains all the math for environmental tolerances in one place (though the microbe editor and
///   especially <see cref="TolerancesEditorSubComponent"/> has some extra range checking code which,
///   if changed here, must be changed there as well)
/// </summary>
public static class MicrobeEnvironmentalToleranceCalculations
{
    /// <summary>
    ///   Calculates the total overall score of environmental tolerance without giving the sub-scores
    /// </summary>
    /// <param name="species">Species to calculate for</param>
    /// <param name="environment">Environment the species is in</param>
    /// <returns>
    ///   The overall score, which ranges from 0 (not surviving at all) to 1 (well adapted),
    ///   and over 1 (for perfectly adapted, which gives bonuses)
    /// </returns>
    public static float CalculateTotalToleranceScore(MicrobeSpecies species, BiomeConditions environment)
    {
        var score = CalculateTolerances(species, environment);
        return score.OverallScore;
    }

    public static ToleranceResult CalculateTolerances(MicrobeSpecies species, BiomeConditions environment)
    {
        return CalculateTolerances(species.Tolerances, species.Organelles, environment);
    }

    public static ToleranceResult CalculateTolerances(EnvironmentalTolerances speciesTolerances,
        IReadOnlyList<OrganelleTemplate> organelles, BiomeConditions environment)
    {
        var result = new ToleranceResult();

        var resolvedTolerances = new ToleranceValues
        {
            PreferredTemperature = speciesTolerances.PreferredTemperature,
            TemperatureTolerance = speciesTolerances.TemperatureTolerance,
            PressureMinimum = speciesTolerances.PressureMinimum,
            PressureMaximum = speciesTolerances.PressureMaximum,
            OxygenResistance = speciesTolerances.OxygenResistance,
            UVResistance = speciesTolerances.UVResistance,
        };

        var noExtraEffects = resolvedTolerances;

        ApplyOrganelleEffectsOnTolerances(organelles, ref resolvedTolerances);

        // Tolerances can't go below minimum values.
        // Otherwise, species adding hydrogenosomes in the vents can be too negatively protected against oxygen.
        if (resolvedTolerances.PressureMinimum < 0)
            resolvedTolerances.PressureMinimum = 0;

        if (resolvedTolerances.OxygenResistance < 0)
            resolvedTolerances.OxygenResistance = 0;

        if (resolvedTolerances.UVResistance < 0)
            resolvedTolerances.UVResistance = 0;

        CalculateTolerancesInternal(resolvedTolerances, noExtraEffects, environment, result);

        return result;
    }

    public static void ApplyOrganelleEffectsOnTolerances(IReadOnlyList<OrganelleTemplate> organelles,
        ref ToleranceValues tolerances)
    {
        int organelleCount = organelles.Count;
        for (int i = 0; i < organelleCount; ++i)
        {
            var organelleDefinition = organelles[i].Definition;

            if (organelleDefinition.AffectsTolerances)
            {
                tolerances.TemperatureTolerance += organelleDefinition.ToleranceModifierTemperatureRange;
                tolerances.OxygenResistance += organelleDefinition.ToleranceModifierOxygen;
                tolerances.UVResistance += organelleDefinition.ToleranceModifierUV;
                tolerances.PressureMinimum -= organelleDefinition.ToleranceModifierPressureRange;
                tolerances.PressureMaximum += organelleDefinition.ToleranceModifierPressureRange;
            }
        }
    }

    public static void GenerateToleranceProblemList(ToleranceResult data, in ResolvedMicrobeTolerances problemNumbers,
        Action<string> resultCallback)
    {
        if (problemNumbers.HealthModifier < 1 || problemNumbers.ProcessSpeedModifier < 1 ||
            problemNumbers.OsmoregulationModifier > 1)
        {
            // Osmoregulation modifier works in reverse (i.e. higher value is worse)
            resultCallback.Invoke(Localization.Translate("TOLERANCES_UNSUITABLE_DEBUFFS")
                .FormatSafe($"+{(problemNumbers.OsmoregulationModifier - 1) * 100:0.#}",
                    -Math.Round((1 - problemNumbers.ProcessSpeedModifier) * 100, 1),
                    -Math.Round((1 - problemNumbers.HealthModifier) * 100, 1)));
        }

        if (data.TemperatureScore < 1)
        {
            if (data.PerfectTemperatureAdjustment < 0)
            {
                resultCallback.Invoke(Localization.Translate("TOLERANCES_TOO_HIGH_TEMPERATURE")
                    .FormatSafe(Math.Round(-data.PerfectTemperatureAdjustment, 1)));
            }
            else
            {
                resultCallback.Invoke(Localization.Translate("TOLERANCES_TOO_LOW_TEMPERATURE")
                    .FormatSafe(Math.Round(data.PerfectTemperatureAdjustment, 1)));
            }
        }

        if (data.PressureScore < 1)
        {
            if (data.PerfectPressureAdjustment < 0)
            {
                // TODO: show the numbers in megapascals when makes sense
                resultCallback.Invoke(Localization.Translate("TOLERANCES_TOO_HIGH_PRESSURE")
                    .FormatSafe(Math.Round(-data.PerfectPressureAdjustment / 1000, 1)));
            }
            else
            {
                resultCallback.Invoke(Localization.Translate("TOLERANCES_TOO_LOW_PRESSURE")
                    .FormatSafe(Math.Round(data.PerfectPressureAdjustment / 1000, 1)));
            }
        }

        if (data.OxygenScore < 1)
        {
            resultCallback.Invoke(Localization.Translate("TOLERANCES_TOO_LOW_OXYGEN_PROTECTION")
                .FormatSafe(Math.Round(data.PerfectOxygenAdjustment * 100, 1)));
        }

        if (data.UVScore < 1)
        {
            resultCallback.Invoke(Localization.Translate("TOLERANCES_TOO_LOW_UV_PROTECTION")
                .FormatSafe(Math.Round(data.PerfectUVAdjustment * 100, 1)));
        }
    }

    public static ResolvedMicrobeTolerances ResolveToleranceValues(ToleranceResult data)
    {
        var result = default(ResolvedMicrobeTolerances);

        result.ProcessSpeedModifier = 1;
        result.HealthModifier = 1;
        result.OsmoregulationModifier = 1;

        if (data.TemperatureScore < 1)
        {
            result.ProcessSpeedModifier *= Math.Max(0.9f, data.TemperatureScore);

            result.OsmoregulationModifier *= Math.Min(1.1f, 2 - data.TemperatureScore);

            result.HealthModifier *= Math.Max(0.9f, data.TemperatureScore);
        }
        else if (data.TemperatureScore > 1)
        {
            result.ProcessSpeedModifier *= Math.Max(1.1f, data.TemperatureScore);
        }

        if (data.PressureScore < 1)
        {
            result.ProcessSpeedModifier *= Math.Max(0.9f, data.PressureScore);
            result.OsmoregulationModifier *= Math.Min(1.1f, 2 - data.PressureScore);
            result.HealthModifier *= Math.Max(0.5f, data.PressureScore);
        }
        else if (data.PressureScore > 1)
        {
            result.HealthModifier *= Math.Max(1.2f, data.PressureScore);
        }

        if (data.OxygenScore < 1)
        {
            result.HealthModifier *= Math.Max(0.5f, data.OxygenScore);
            result.OsmoregulationModifier *= Math.Min(1.5f, 2 - data.OxygenScore);
        }

        if (data.UVScore < 1)
        {
            result.HealthModifier *= Math.Max(0.5f, data.UVScore);
            result.OsmoregulationModifier *= Math.Min(1.5f, 2 - data.UVScore);
        }

        // TODO: figure out why really bad pressure score species appear in the first generation
        /*if(data.PressureScore <= 0)
            Debugger.Break();*/

#if DEBUG
        if (result.OsmoregulationModifier <= MathUtils.EPSILON)
        {
            if (Debugger.IsAttached)
                Debugger.Break();

            throw new Exception("Osmoregulation modifier is 0");
        }

        if (result.ProcessSpeedModifier <= MathUtils.EPSILON)
        {
            if (Debugger.IsAttached)
                Debugger.Break();

            throw new Exception("Speed modifier is 0");
        }

        if (result.HealthModifier <= MathUtils.EPSILON)
        {
            if (Debugger.IsAttached)
                Debugger.Break();

            throw new Exception("Health modifier is 0");
        }
#endif

        return result;
    }

    private static void CalculateTolerancesInternal(in ToleranceValues speciesTolerances,
        in ToleranceValues noExtraEffects, BiomeConditions environment, ToleranceResult result)
    {
        var patchTemperature = environment.GetCompound(Compound.Temperature, CompoundAmountType.Biome).Ambient;
        var patchPressure = environment.Pressure;
        var requiredOxygenResistance = environment.CalculateOxygenResistanceFactor();
        var requiredUVResistance = environment.CalculateUVFactor();

        bool missingSomething = false;

        // Always write the targets for becoming perfectly adapted
        result.PerfectTemperatureAdjustment = patchTemperature - speciesTolerances.PreferredTemperature;
        result.PerfectOxygenAdjustment = requiredOxygenResistance - speciesTolerances.OxygenResistance;
        result.PerfectUVAdjustment = requiredUVResistance - speciesTolerances.UVResistance;

        // Need to get the average pressure value from the max and min to know how much to adjust
        result.PerfectPressureAdjustment =
            patchPressure - (speciesTolerances.PressureMaximum + speciesTolerances.PressureMinimum) * 0.5f;

        // TODO: the root cause of https://github.com/Revolutionary-Games/Thrive/issues/5928 is probably somewhere in
        // the following lines of code

        // Temperature
        if (patchTemperature > speciesTolerances.PreferredTemperature + speciesTolerances.TemperatureTolerance ||
            patchTemperature < speciesTolerances.PreferredTemperature - speciesTolerances.TemperatureTolerance)
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
        else if (noExtraEffects.TemperatureTolerance <= Constants.TOLERANCE_PERFECT_THRESHOLD_TEMPERATURE)
        {
            // Perfect adaptation ranges are calculated without the effects of organelles as they would otherwise
            // be really hard to apply

            // Perfectly adapted
            var perfectionFactor = Constants.TOLERANCE_PERFECT_TEMPERATURE_SCORE *
                (1 - (noExtraEffects.TemperatureTolerance / Constants.TOLERANCE_PERFECT_THRESHOLD_TEMPERATURE));
            result.TemperatureScore = 1 + perfectionFactor;
        }
        else
        {
            // Adequately adapted, but could be made perfect
            result.TemperatureRangeSizeAdjustment =
                Constants.TOLERANCE_PERFECT_THRESHOLD_TEMPERATURE - noExtraEffects.TemperatureTolerance;

            result.TemperatureScore = 1;
        }

        // Pressure
        if (patchPressure > speciesTolerances.PressureMaximum)
        {
            // Too high pressure

            if (patchPressure - speciesTolerances.PressureMaximum >
                Constants.TOLERANCE_MAXIMUM_SURVIVABLE_PRESSURE_DIFFERENCE)
            {
                result.PressureScore = 0;
            }
            else
            {
                result.PressureScore = 1 - (patchPressure - speciesTolerances.PressureMaximum) /
                    Constants.TOLERANCE_MAXIMUM_SURVIVABLE_PRESSURE_DIFFERENCE;
            }

            missingSomething = true;
        }
        else if (patchPressure < speciesTolerances.PressureMinimum)
        {
            // Too low pressure
            if (speciesTolerances.PressureMinimum - patchPressure >
                Constants.TOLERANCE_MAXIMUM_SURVIVABLE_PRESSURE_DIFFERENCE)
            {
                result.PressureScore = 0;
            }
            else
            {
                result.PressureScore = 1 - (speciesTolerances.PressureMinimum - patchPressure) /
                    Constants.TOLERANCE_MAXIMUM_SURVIVABLE_PRESSURE_DIFFERENCE;
            }

            missingSomething = true;
        }
        else
        {
            var range = Math.Abs(noExtraEffects.PressureMaximum - noExtraEffects.PressureMinimum);

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
                result.PressureRangeSizeAdjustment = Constants.TOLERANCE_PERFECT_THRESHOLD_PRESSURE - range;

                result.PressureScore = 1;
            }
        }

        // Oxygen Resistance
        if (speciesTolerances.OxygenResistance < requiredOxygenResistance)
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
        if (speciesTolerances.UVResistance < requiredUVResistance)
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
    }

    /// <summary>
    ///   Final tolerance values to use in calculations
    /// </summary>
    public struct ToleranceValues
    {
        public float PreferredTemperature;
        public float TemperatureTolerance;
        public float PressureMinimum;
        public float PressureMaximum;
        public float OxygenResistance;
        public float UVResistance;

        public void CopyFrom(EnvironmentalTolerances tolerances)
        {
            PreferredTemperature = tolerances.PreferredTemperature;
            TemperatureTolerance = tolerances.TemperatureTolerance;
            PressureMinimum = tolerances.PressureMinimum;
            PressureMaximum = tolerances.PressureMaximum;
            OxygenResistance = tolerances.OxygenResistance;
            UVResistance = tolerances.UVResistance;
        }
    }
}

public class ToleranceResult
{
    public float OverallScore;

    public float TemperatureScore;

    /// <summary>
    ///   How to adjust the preferred temperature to get to the exact value in the biome
    /// </summary>
    public float PerfectTemperatureAdjustment;

    /// <summary>
    ///   How to adjust the tolerance range of temperature to qualify as perfectly adapted
    /// </summary>
    public float TemperatureRangeSizeAdjustment;

    public float PressureScore;
    public float PerfectPressureAdjustment;
    public float PressureRangeSizeAdjustment;

    public float OxygenScore;
    public float PerfectOxygenAdjustment;

    public float UVScore;
    public float PerfectUVAdjustment;
}
