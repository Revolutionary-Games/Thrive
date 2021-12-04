namespace AutoEvo
{
    using System;
    using System.Collections.Generic;
    using Godot;

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
        private readonly List<Patch> patches;

        public ForceExtinction(List<Patch> patches)
        {
            this.patches = patches;
        }

        public bool CanRunConcurrently => false;

        public int TotalSteps => 1;

        public bool RunStep(RunResults results)
        {
            var populationsByPatch = results.GetPopulationsByPatch(true, true, true);

            foreach (Patch patch in patches)
            {
                var speciesInPatch = populationsByPatch[patch];
                RunStepInPatch(results, patch, speciesInPatch);
            }

            return true;
        }

        private bool RunStepInPatch(RunResults results, Patch patch, Dictionary<Species, long> speciesInPatch)
        {
            var newSpeciesCount = results.GetNewSpeciesResults(patch).Count;

            // Only bother if we're above the limit
            if (speciesInPatch.Count + newSpeciesCount <= Constants.AUTO_EVO_MAXIMUM_SPECIES_IN_PATCH)
            {
                return true;
            }

            GD.Print("Running extinction step in patch ", patch.Name, ".");

            var orderedSpeciesInPatch = speciesInPatch.GetSortedKeyArray();

            // Remove worst-faring species, except for the player's species
            // TODO: Use auto-evo configuratio instead of constant
            // TODO: if we remove everything, better skip the sorting
            var speciesToRemoveCount = Math.Max(
                speciesInPatch.Count + newSpeciesCount - Constants.AUTO_EVO_MAXIMUM_SPECIES_IN_PATCH,
                orderedSpeciesInPatch.Length);

            for (int i = 0; i < speciesToRemoveCount; i++)
            {
                if (orderedSpeciesInPatch[i].PlayerSpecies)
                    continue;

                GD.Print("Forced extinction of species ", orderedSpeciesInPatch[i].FormattedName,
                    " in patch ", patch.Name, ".");
                results.AddPopulationResultForSpecies(orderedSpeciesInPatch[i], patch, 0);
            }

            return true;
        }
    }
}
