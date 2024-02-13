namespace Components
{
    using System.Collections.Generic;

    /// <summary>
    ///   General info about the reproduction status of a creature
    /// </summary>
    [JSONDynamicTypeAllowed]
    public struct ReproductionStatus
    {
        public Dictionary<Compound, float>? MissingCompoundsForBaseReproduction;

        // TODO: remove if unused for now (is currently unused -hhyyrylainen)
        public bool ReadyToReproduce;

        public ReproductionStatus(IReadOnlyDictionary<Compound, float> baseReproductionCost)
        {
            MissingCompoundsForBaseReproduction = baseReproductionCost.CloneShallow();

            ReadyToReproduce = false;
        }
    }

    public static class ReproductionStatusHelpers
    {
        /// <summary>
        ///   Sets up the base reproduction cost that is on top of the normal costs (for microbes)
        /// </summary>
        public static void SetupRequiredBaseReproductionCompounds(this ref ReproductionStatus reproductionStatus,
            Species species)
        {
            reproductionStatus.MissingCompoundsForBaseReproduction ??= new Dictionary<Compound, float>();

            reproductionStatus.MissingCompoundsForBaseReproduction.Clear();
            reproductionStatus.MissingCompoundsForBaseReproduction.Merge(species.BaseReproductionCost);
        }

        public static void CalculateAlreadyUsedBaseReproductionCompounds(this ref ReproductionStatus reproductionStatus,
            Species species, Dictionary<Compound, float> resultReceiver)
        {
            if (reproductionStatus.MissingCompoundsForBaseReproduction == null)
                return;

            foreach (var totalCost in species.BaseReproductionCost)
            {
                if (!reproductionStatus.MissingCompoundsForBaseReproduction.TryGetValue(totalCost.Key,
                        out var left))
                {
                    // If we used any unknown values (which are 0) to calculate the absorbed amounts, this would be
                    // vastly incorrect
                    continue;
                }

                var absorbed = totalCost.Value - left;

                if (!(absorbed > 0))
                    continue;

                resultReceiver.TryGetValue(totalCost.Key, out var alreadyAbsorbed);
                resultReceiver[totalCost.Key] = alreadyAbsorbed + absorbed;
            }
        }
    }
}
