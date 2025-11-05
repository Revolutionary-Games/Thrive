namespace AutoEvo;

using System;
using System.Collections.Generic;
using System.Diagnostics;
using Godot;

public class ModifyEnvironmentalTolerance : IMutationStrategy<MicrobeSpecies>
{
    /// <summary>
    ///   Not repeatable as this strategy always uses as much MP as possible to reach the optimal environmental
    ///   tolerance values
    /// </summary>
    public bool Repeatable => false;

    public List<Tuple<MicrobeSpecies, double>>? MutationsOf(MicrobeSpecies baseSpecies, double mp, bool lawk,
        Random random, BiomeConditions biomeToConsider)
    {
        if (mp <= 0)
            return null;

#if DEBUG
        var originalMp = mp;
#endif

        var score = MicrobeEnvironmentalToleranceCalculations.CalculateTolerances(baseSpecies, biomeToConsider);

        // This is outside the if statement to have a slightly more consistent number of random calls (though this
        // might not really matter in practice)
        float skipChance = random.NextSingle();
        float perfectTemperatureChance = random.NextSingle();
        float perfectPressureChance = random.NextSingle();
        bool doingPerfect = false;

        // Do not generate mutations if adapted enough for the environment
        if (score.OverallScore >= 1.0f - MathUtils.EPSILON * 2)
        {
            // But on random chance try to be adapted perfectly for a bonus
            if (skipChance > Constants.AUTO_EVO_TOLERANCE_PERFECT_CHANCE_IF_ADAPTED)
            {
                return null;
            }

            doingPerfect = true;
        }

        bool changes = false;

        // Technically, this could be done a lot later, but that needs all branches here to allocate this separately
        var newTolerances = baseSpecies.Tolerances.Clone();

        // TODO: with limited MP it might be better to prioritise something else than temperature
        // And skipping other tolerance changes if not enough MP to perfect something but also fix a problem
        if (score.TemperatureScore < 1 ||
            doingPerfect || perfectTemperatureChance <= Constants.AUTO_EVO_TOLERANCE_PERFECT_CHANCE_OTHER)
        {
            if (score.TemperatureScore < 1 || Math.Abs(score.PerfectTemperatureAdjustment) > MathUtils.EPSILON)
            {
                var maxChange = mp / Constants.TOLERANCE_CHANGE_MP_PER_TEMPERATURE;

                float change;

                if (score.PerfectTemperatureAdjustment < 0)
                {
                    // Round here so that we don't accidentally round the max change upwards inflating how much change
                    // is allowed to happen
                    change = (float)Math.Max(score.PerfectTemperatureAdjustment, -maxChange);
                }
                else
                {
                    change = (float)Math.Min(score.PerfectTemperatureAdjustment, maxChange);
                }

                newTolerances.PreferredTemperature += change;

                // Need to remember to adjust MP
                mp -= Math.Abs(change) * Constants.TOLERANCE_CHANGE_MP_PER_TEMPERATURE;
            }
            else
            {
                // Trying to perfect this
                var maxChange = -(float)(mp / Constants.TOLERANCE_CHANGE_MP_PER_TEMPERATURE_TOLERANCE);

                // To perfect temperature it needs to be always negative
#if DEBUG
                if (score.TemperatureRangeSizeAdjustment > 0)
                {
                    if (Debugger.IsAttached)
                        Debugger.Break();
                    throw new Exception("Temperature range size adjustment is not negative");
                }
#endif

                var change = Math.Max(score.TemperatureRangeSizeAdjustment, maxChange);

                newTolerances.TemperatureTolerance += change;

                mp -= Math.Abs(change) * Constants.TOLERANCE_CHANGE_MP_PER_TEMPERATURE_TOLERANCE;
            }

            changes = true;
        }

        // TODO: sometimes when the tolerance range is too low, should this try to increase the range instead of trying
        // to just adjust the preferred exact value?

        var anyPressureAdjustmentPossible = Math.Abs(score.PerfectPressureAdjustment) > MathUtils.EPSILON;

        if ((score.PressureScore < 1 || anyPressureAdjustmentPossible || doingPerfect ||
                perfectPressureChance <= Constants.AUTO_EVO_TOLERANCE_PERFECT_CHANCE_OTHER) && mp > 0)
        {
            if (score.PressureScore < 1 || anyPressureAdjustmentPossible)
            {
                var maxChange = mp / Constants.TOLERANCE_CHANGE_MP_PER_PRESSURE;

                // Calculate in doubles as the pressure stuff needs many decimals
                double change;

                if (score.PerfectPressureAdjustment < 0)
                {
                    change = Math.Max(score.PerfectPressureAdjustment, -maxChange);
                }
                else
                {
                    change = Math.Min(score.PerfectPressureAdjustment, maxChange);
                }

                // These are adjusted in the same direction to keep the same range as before
                newTolerances.PressureMinimum = Math.Max(newTolerances.PressureMinimum + (float)change, 0);
                newTolerances.PressureMaximum = Math.Max(newTolerances.PressureMaximum + (float)change, 0);

                mp -= Math.Abs(change) * Constants.TOLERANCE_CHANGE_MP_PER_PRESSURE;
            }
            else
            {
                // Trying to perfect this, which is much harder than the other cases as the middle point also will
                // change (potentially)
                var changePotential = -(mp / Constants.TOLERANCE_CHANGE_MP_PER_PRESSURE_TOLERANCE);

                // The top range needs to always go down, and the bottom range needs always to go up
#if DEBUG
                if (score.PressureRangeSizeAdjustment > 0)
                {
                    if (Debugger.IsAttached)
                        Debugger.Break();
                    throw new Exception("Pressure range size adjustment is not negative");
                }
#endif

                // TODO: either split this more equally or consider if it should be the other way around

                var halfAdjustment = score.PressureRangeSizeAdjustment * 0.5f;

                // Multiply potential by 0.5 to ensure the min range also gets some MP to change
                var maxChange = Math.Max(changePotential * 0.5, halfAdjustment);

                newTolerances.PressureMaximum = (float)(newTolerances.PressureMaximum + maxChange);

                // Recalculate change potential for the other part of the calculation
                mp -= Math.Abs(maxChange) * Constants.TOLERANCE_CHANGE_MP_PER_PRESSURE_TOLERANCE;
                changePotential = -(mp / Constants.TOLERANCE_CHANGE_MP_PER_PRESSURE_TOLERANCE);

                var minChange = Math.Max(changePotential, halfAdjustment);

                newTolerances.PressureMinimum = (float)(newTolerances.PressureMinimum - minChange);

                mp -= Math.Abs(minChange) * Constants.TOLERANCE_CHANGE_MP_PER_PRESSURE_TOLERANCE;
            }

            changes = true;
        }

        if (score.OxygenScore < 1 && mp > 0)
        {
            var maxChange = mp / Constants.TOLERANCE_CHANGE_MP_PER_OXYGEN;

            // Oxygen value can only be too low, so it is always increased
            var change = Math.Min(score.PerfectOxygenAdjustment, maxChange);

#if DEBUG
            if (score.PerfectOxygenAdjustment < 0)
            {
                if (Debugger.IsAttached)
                    Debugger.Break();
                throw new Exception("Oxygen perfection adjustment should not be negative");
            }
#endif

            newTolerances.OxygenResistance += (float)change;

            mp -= Math.Abs(change) * Constants.TOLERANCE_CHANGE_MP_PER_OXYGEN;
            changes = true;
        }

        if (score.UVScore < 1 && mp > 0)
        {
            var maxChange = mp / Constants.TOLERANCE_CHANGE_MP_PER_UV;

            var change = Math.Min(score.PerfectUVAdjustment, maxChange);

#if DEBUG
            if (score.PerfectUVAdjustment < 0)
            {
                if (Debugger.IsAttached)
                    Debugger.Break();
                throw new Exception("UV perfection adjustment should not be negative");
            }
#endif

            newTolerances.UVResistance += (float)change;

            mp -= Math.Abs(change) * Constants.TOLERANCE_CHANGE_MP_PER_UV;
            changes = true;
        }

        if (!changes)
        {
            // Didn't find anything to do after all. This condition should be ensured to be rare as we wasted some
            // processing time here
            return null;
        }

        // TODO: could do a shallower clone here as organelles won't be modified, so this doesn't need to clone
        // anything except the tolerances
        var newSpecies = (MicrobeSpecies)baseSpecies.Clone();
        newSpecies.Tolerances.CopyFrom(newTolerances);

#if DEBUG
        newSpecies.Tolerances.SanityCheck();

        var newScore = MicrobeEnvironmentalToleranceCalculations.CalculateTolerances(newSpecies, biomeToConsider);

        if (newScore.OverallScore < score.OverallScore - MathUtils.EPSILON)
        {
            GD.PrintErr($"Tolerance change in auto-evo caused score to get worse from: {score.OverallScore} " +
                $"to {newScore.OverallScore}");

            if (Debugger.IsAttached)
                Debugger.Break();
        }

        if (mp > originalMp)
        {
            GD.PrintErr($"Tolerance change in auto-evo accidentally increased MP to {mp}");

            if (Debugger.IsAttached)
                Debugger.Break();
        }
#endif

        return [Tuple.Create(newSpecies, mp)];
    }
}
