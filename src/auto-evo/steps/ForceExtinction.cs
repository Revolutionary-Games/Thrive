using Godot;

namespace AutoEvo
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
            GD.Print("!!!Running extinction step in patch ", patch.Name, "!");
            // TODO cache
            var speciesInPatch = results.GetPopulationsByPatch(true, true)[patch];

            var orderedSpeciesInPatch = new Species[speciesInPatch.Count];
            GD.Print(speciesInPatch.Count);
            foreach (var trash in speciesInPatch) GD.Print(trash.Key, " -> ", trash.Value);
            speciesInPatch.Keys.CopyTo(orderedSpeciesInPatch, 0);

            // Sorting by insertion, asymptotically sub-optimal but usually efficient on small datasets like here.
            for (int i = 1; i < orderedSpeciesInPatch.Length; i++)
            {
                var speciesToSort = orderedSpeciesInPatch[i];
                var population = speciesInPatch[speciesToSort];
                GD.Print(orderedSpeciesInPatch[i].FormattedName, " population: ", population);

                // Sort value at index i within the previous ones, already sorted from low to high
                for (int j = i; j >= 0; j--)
                {
                    // No smaller value found, just place it at the bottom.
                    if (j == 0)
                    {
                        orderedSpeciesInPatch[j] = speciesToSort;
                        break;
                    }

                    var referenceSpecies = orderedSpeciesInPatch[j - 1];

                    // If we are above the before index, just plug it here and stop.
                    // Note that the strict operator *may* favor more recent species for equality in population.
                    if (population > speciesInPatch[referenceSpecies])
                    {
                        orderedSpeciesInPatch[j] = speciesToSort;
                        break;
                    }

                    // Else just shift the hole to place the species in and continue.
                    orderedSpeciesInPatch[j] = referenceSpecies;
                }
            }

            // Remove worst-faring species, except for the player's species
            // TODO: Use auto-evo configuratio instead of constant
            for (int i = 0; i < orderedSpeciesInPatch.Length - Constants.AUTO_EVO_MAXIMUM_SPECIES_IN_PATCH; i++)
            {
                if (orderedSpeciesInPatch[i].PlayerSpecies)
                    continue;
                GD.Print("!!!Removed species ", orderedSpeciesInPatch[i].FormattedName, " in patch ", patch.Name, "!");
                results.AddPopulationResultForSpecies(orderedSpeciesInPatch[i], patch, 0);
            }

            return true;
        }
    }
}
