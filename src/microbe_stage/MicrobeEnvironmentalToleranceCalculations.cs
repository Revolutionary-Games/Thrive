using System;
using System.Collections.Generic;
using System.Diagnostics;
using Godot;

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
    public static double CalculateTotalToleranceScore(MicrobeSpecies species, BiomeConditions environment)
    {
        var score = CalculateTolerances(species, environment);
        return score.OverallScore;
    }

    public static ToleranceResult CalculateTolerances(MicrobeSpecies species, IBiomeConditions environment)
    {
        return CalculateTolerances(species.Tolerances, species.Organelles, environment);
    }

    /// <summary>
    ///   Calculates effective tolerances given the species tolerances, organelles, and environmental conditions.
    /// </summary>
    /// <param name="speciesTolerances">Configured tolerances</param>
    /// <param name="organelles">Organelles that may affect the tolerances</param>
    /// <param name="environment">Environment that the tolerances need to match to not get debuffs</param>
    /// <param name="excludePositiveBuffs">
    ///   If true, excludes perfect adaptation bonuses. This is used to show debuffs in a way that no buffs can get
    ///   mixed in. Note that for the tooltips we separately generate "good enough" tolerances to not get bonuses or
    ///   debuffs instead of using this flag. So TODO: it would be nice to combine these two approaches that are almost
    ///   the same. But to get the new tolerance GUI visuals done, these two systems were left as separate (for now).
    /// </param>
    /// <returns>Calculated tolerance result</returns>
    public static ToleranceResult CalculateTolerances(IReadOnlyEnvironmentalTolerances speciesTolerances,
        IReadOnlyList<OrganelleTemplate> organelles, IBiomeConditions environment, bool excludePositiveBuffs = false)
    {
        var result = new ToleranceResult();

        var resolvedTolerances = new ToleranceValues
        {
            PreferredTemperature = speciesTolerances.PreferredTemperature,
            TemperatureTolerance = speciesTolerances.TemperatureTolerance,
            PressureMinimum = speciesTolerances.PressureMinimum,
            PressureTolerance = speciesTolerances.PressureTolerance,
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

        CalculateTolerancesInternal(resolvedTolerances, noExtraEffects, environment, result, excludePositiveBuffs);

        return result;
    }

    public static void ApplyOrganelleEffectsOnTolerances(IReadOnlyList<OrganelleTemplate> organelles,
        ref ToleranceValues tolerances)
    {
        float temperatureChange = 0;
        float oxygenChange = 0;
        float uvChange = 0;
        float pressureMinimumChange = 0;
        float pressureToleranceChange = 0;

        int organelleCount = organelles.Count;
        for (int i = 0; i < organelleCount; ++i)
        {
            var organelleDefinition = organelles[i].Definition;

            if (organelleDefinition.AffectsTolerances)
            {
                // Buffer all changes so that float rounding doesn't cause us issues
                temperatureChange += organelleDefinition.ToleranceModifierTemperatureRange;
                oxygenChange += organelleDefinition.ToleranceModifierOxygen;
                uvChange += organelleDefinition.ToleranceModifierUV;
                pressureToleranceChange += organelleDefinition.ToleranceModifierPressureTolerance;
            }
        }

        // Then apply all at once
        tolerances.TemperatureTolerance += temperatureChange;
        tolerances.OxygenResistance += oxygenChange;
        tolerances.UVResistance += uvChange;
        tolerances.PressureMinimum -= pressureMinimumChange;
        tolerances.PressureTolerance += pressureToleranceChange;
    }

    public static void GenerateToleranceEffectSummariesByOrganelle(IReadOnlyList<OrganelleTemplate> organelles,
        ToleranceModifier modifier, Dictionary<OrganelleDefinition, float> result)
    {
        result.Clear();

        int organelleCount = organelles.Count;
        for (int i = 0; i < organelleCount; ++i)
        {
            var organelleDefinition = organelles[i].Definition;
            if (!organelleDefinition.ToleranceEffects.TryGetValue(modifier, out var value))
                continue;

            result.TryGetValue(organelleDefinition, out var existingValue);

            result[organelleDefinition] = existingValue + value;
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

        var temperatureScore = (float)data.TemperatureScore;
        if (temperatureScore < 1)
        {
            result.ProcessSpeedModifier *=
                Math.Max(Constants.TOLERANCE_TEMPERATURE_SPEED_MODIFIER_MIN, temperatureScore);

            result.OsmoregulationModifier *= Math.Min(Constants.TOLERANCE_TEMPERATURE_OSMOREGULATION_MAX,
                2 - temperatureScore);

            result.HealthModifier *= Math.Max(Constants.TOLERANCE_TEMPERATURE_HEALTH_MIN, temperatureScore);
        }
        else if (data.TemperatureScore > 1)
        {
            result.ProcessSpeedModifier *=
                Math.Max(Constants.TOLERANCE_TEMPERATURE_SPEED_BUFF_MAX, temperatureScore);
        }

        var pressureScore = (float)data.PressureScore;
        if (pressureScore < 1)
        {
            result.ProcessSpeedModifier *=
                Math.Max(Constants.TOLERANCE_PRESSURE_SPEED_MODIFIER_MIN, pressureScore);
            result.OsmoregulationModifier *=
                Math.Min(Constants.TOLERANCE_PRESSURE_OSMOREGULATION_MAX, 2 - pressureScore);
            result.HealthModifier *= Math.Max(Constants.TOLERANCE_PRESSURE_HEALTH_MIN, pressureScore);
        }
        else if (data.PressureScore > 1)
        {
            result.HealthModifier *= Math.Max(Constants.TOLERANCE_PRESSURE_HEALTH_BUFF_MAX, pressureScore);
        }

        var oxygenScore = (float)data.OxygenScore;
        if (oxygenScore < 1)
        {
            result.HealthModifier *= Math.Max(Constants.TOLERANCE_OXYGEN_HEALTH_MIN, oxygenScore);
            result.OsmoregulationModifier *=
                Math.Min(Constants.TOLERANCE_OXYGEN_OSMOREGULATION_MAX, 2 - oxygenScore);
        }

        var uvScore = (float)data.UVScore;
        if (uvScore < 1)
        {
            result.HealthModifier *= Math.Max(Constants.TOLERANCE_UV_HEALTH_MIN, uvScore);
            result.OsmoregulationModifier *= Math.Min(Constants.TOLERANCE_UV_OSMOREGULATION_MAX, 2 - uvScore);
        }

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
        in ToleranceValues noExtraEffects, IBiomeConditions environment, ToleranceResult result,
        bool excludePositiveBuffs)
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

        // Need to get minimum pressure a little bit below perfect pressure to make sure microbes can adjust perfectly
        result.PerfectPressureAdjustment = patchPressure - speciesTolerances.PressureMinimum -
            Constants.TOLERANCE_PERFECT_THRESHOLD_PRESSURE * 0.5f;

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
            if (!excludePositiveBuffs)
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
                result.TemperatureScore = 1;
            }
        }
        else
        {
            // Adequately adapted, but could be made perfect
            result.TemperatureRangeSizeAdjustment =
                Constants.TOLERANCE_PERFECT_THRESHOLD_TEMPERATURE - noExtraEffects.TemperatureTolerance;

            result.TemperatureScore = 1;
        }

        var pressureMaximum = speciesTolerances.PressureMinimum + speciesTolerances.PressureTolerance;

        // Pressure
        if (patchPressure > pressureMaximum)
        {
            // Too high pressure

            if (patchPressure - pressureMaximum >
                Constants.TOLERANCE_MAXIMUM_SURVIVABLE_PRESSURE_DIFFERENCE)
            {
                result.PressureScore = 0;
            }
            else
            {
                result.PressureScore = 1 - (patchPressure - pressureMaximum) /
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
            if (noExtraEffects.PressureTolerance <= Constants.TOLERANCE_PERFECT_THRESHOLD_PRESSURE)
            {
                // Perfectly adapted
                if (!excludePositiveBuffs)
                {
                    var perfectionFactor = Constants.TOLERANCE_PERFECT_PRESSURE_SCORE *
                        (1 - noExtraEffects.PressureTolerance / Constants.TOLERANCE_PERFECT_THRESHOLD_PRESSURE);
                    result.PressureScore = 1 + perfectionFactor;
                }
                else
                {
                    result.PressureScore = 1;
                }
            }
            else
            {
                // Adequately adapted, but could be made perfect
                result.PressureRangeSizeAdjustment = Constants.TOLERANCE_PERFECT_THRESHOLD_PRESSURE -
                    noExtraEffects.PressureTolerance;

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

        // Make the overall score an average of all values
        result.OverallScore =
            (result.TemperatureScore + result.PressureScore + result.OxygenScore + result.UVScore) / 4;

        // But if missing something, ensure the score is not 1.
        // This should happen only when all the scores are mostly 1,
        // but something is ever so slightly off and due to rounding, it would appear as a 1
        if (missingSomething && result.OverallScore >= 1)
        {
            // Don't allow any perfect scores to be over 1 any more as the below calculation will not manage to bring
            // the score down
            result.OverallScore =
                (Math.Min(result.TemperatureScore, 1) + Math.Min(result.PressureScore, 1) +
                    Math.Min(result.OxygenScore, 1) + Math.Min(result.UVScore, 1)) / 4;

            // If that fixed it, then return immediately without further necessary adjustments
            if (result.OverallScore < 1)
                return;

            // Find the stat that is slightly off due to tiny value rounding and apply a bit of penalty
            if (speciesTolerances.OxygenResistance < requiredOxygenResistance)
            {
                result.OverallScore -= result.PerfectOxygenAdjustment;
            }

            if (speciesTolerances.UVResistance < requiredUVResistance)
                result.OverallScore -= result.PerfectUVAdjustment;

            if (result.OverallScore >= 1)
            {
#if DEBUG
                if (Debugger.IsAttached)
                    Debugger.Break();
#endif

                GD.PrintErr(
                    "Couldn't find a way to adjust tolerance score that should not be perfect to not be perfect");
                GD.PrintErr("Applying epsilon amount of penalty to the score");
                result.OverallScore -= MathUtils.EPSILON;
            }
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
        public float PressureTolerance;
        public float OxygenResistance;
        public float UVResistance;
    }
}

public class ToleranceResult
{
    // The scores are doubles to avoid rounding problems where score is 1 but something is missing
    public double OverallScore;

    public double TemperatureScore;

    /// <summary>
    ///   How to adjust the preferred temperature to get to the exact value in the biome
    /// </summary>
    public float PerfectTemperatureAdjustment;

    /// <summary>
    ///   How to adjust the tolerance range of temperature to qualify as perfectly adapted
    /// </summary>
    public float TemperatureRangeSizeAdjustment;

    public double PressureScore;
    public float PerfectPressureAdjustment;
    public float PressureRangeSizeAdjustment;

    public double OxygenScore;
    public float PerfectOxygenAdjustment;

    public double UVScore;
    public float PerfectUVAdjustment;
}
