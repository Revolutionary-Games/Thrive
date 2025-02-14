namespace AutoEvo;

using System;
using System.Collections.Generic;

public class ModifyEnvironmentalTolerance : IMutationStrategy<MicrobeSpecies>
{
    /// <summary>
    ///   Not repeatable as this strategy always uses as much MP as possible to reach the optimal environmental
    ///   tolerance values
    /// </summary>
    public bool Repeatable => false;

    public List<Tuple<MicrobeSpecies, float>>? MutationsOf(MicrobeSpecies baseSpecies, float mp, bool lawk,
        Random random)
    {
        var score = MicrobeEnvironmentalToleranceCalculations.CalculateTolerances(baseSpecies, TODO, cache);

        // This is outside the if statement to have a slightly more consistent number of random calls
        float skipChance = random.NextSingle();
        bool doingPerfect = false;

        // Do not generate mutations if adapted enough for the environment
        if (score.OverallScore >= 1.0f)
        {
            // But on random chance try to be adapted perfectly for a bonus
            if (skipChance > Constants.AUTO_EVO_TOLERANCE_PERFECT_CHANCE)
            {
                return null;
            }

            doingPerfect = true;
        }

        // TODO: could do a shallower clone here as organelles won't be modified so this doesn't need to clone anything
        // except the tolerances
        MicrobeSpecies? newSpecies = null;

        void SetupSpecies()
        {
            newSpecies ??= (MicrobeSpecies)baseSpecies.Clone();
        }

        if (score.TemperatureScore < 1 || doingPerfect)
        {
            SetupSpecies();
            mp -=  ?;
        }

        if (score.PressureScore < 1 || doingPerfect)
        {
        }

        if (score.OxygenScore < 1 || doingPerfect)
        {
        }

        if (score.UVScore < 1 || doingPerfect)
        {
        }

        if (newSpecies == null)
        {
            // Didn't find anything to do after all. This condition should be ensured to be rare as we wasted some
            // processing time here
            return null;
        }

        return [Tuple.Create(newSpecies, mp)];
    }
}
