﻿namespace AutoEvo
{
    /// <summary>
    ///   Forces extinction of worse-faring species to limit the number of steps in next auto-evo run
    /// </summary>
    /// <remarks>
    ///   <para>
    ///     This was a hotfix to the 0.5.6 release.
    ///     TODO: Inquire more refined solution, such as random extinction with bias for smaller populations.
    ///   </para>
    /// </remarks>
    public class ForceExtinction : IRunStep
    {
        private readonly Patch patch;

        public ForceExtinction(Patch patch)
        {
            this.patch = patch;
        }

        public bool CanRunConcurrently => false;

        public int TotalSteps => 1;

        public bool RunStep(RunResults results)
        {
            var orderedSpeciesInPatch = new Species[patch.SpeciesInPatch.Count];
            patch.SpeciesInPatch.Keys.CopyTo(orderedSpeciesInPatch, 0);

            // Sorting by insertion, asymptotically sub-optimal but usually efficient on small datasets like here.
            for (int i = 1; i < orderedSpeciesInPatch.Length; i++)
            {
                var population = results.GetPopulationInPatch(orderedSpeciesInPatch[i], patch);

                // Sort value at index i within the previous ones, already sorted
                for (int j = i; j > 0; j--)
                {
                    var referenceSpecies = orderedSpeciesInPatch[j - 1];

                    // If we are above the before index, just plug it here and stop.
                    // Note that the strict operator *may* favor more recent species for equality in population.
                    if (population > results.GetPopulationInPatch(referenceSpecies, patch))
                    {
                        orderedSpeciesInPatch[j] = orderedSpeciesInPatch[i];
                        break;
                    }

                    // Else just shift the hole to place the species in and continue.
                    orderedSpeciesInPatch[j] = referenceSpecies;
                }
            }

            // Remove worst-faring species, except for the player's species
            // TODO: Use auto-evo configuratio instead of constant
            for (int i = Constants.AUTO_EVO_MAXIMUM_SPECIES_IN_PATCH; i < orderedSpeciesInPatch.Length; i++)
            {
                if (orderedSpeciesInPatch[i].PlayerSpecies)
                    continue;
                results.AddPopulationResultForSpecies(orderedSpeciesInPatch[i], patch, 0);
            }

            return true;
        }
    }
}
