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
            GD.Print("!RUNning debug step");
            var populationsByPatch = results.GetPopulationsByPatch(true, true, false);

            foreach (Patch patch in patches)
            {
                var speciesInPatch = populationsByPatch[patch];
                var newSpeciesCount = results.GetNewSpeciesResults(patch).Count;

                GD.Print("Debuggin extinction step in patch ", patch.Name, ". Count: ", speciesInPatch.Count + newSpeciesCount);
                foreach (var temp in speciesInPatch) GD.Print(temp.Key.FormattedName, " -> ", temp.Value);
                foreach (var temp in results.GetNewSpeciesResults(patch).Keys) GD.Print("New: ", temp.FormattedName);

                // Only bother if we're above the limit
                if (speciesInPatch.Count + newSpeciesCount <= Constants.AUTO_EVO_MAXIMUM_SPECIES_IN_PATCH)
                {
                    continue;
                }

                GD.Print("Running extinction step in patch ", patch.Name, ".");

                var orderedSpeciesInPatch = speciesInPatch.GetSortedKeyArray();

                // Remove worst-faring species, except for the player's species
                // TODO: Use auto-evo configuratio instead of constant
                // TODO: if we remove everything, better skip the sorting
                var speciesToRemoveCount = Math.Min(
                    speciesInPatch.Count + newSpeciesCount - Constants.AUTO_EVO_MAXIMUM_SPECIES_IN_PATCH,
                    orderedSpeciesInPatch.Length);

                for (int i = 0; i < speciesToRemoveCount; i++)
                {
                    if (orderedSpeciesInPatch[i].PlayerSpecies)
                        continue;

                    GD.Print("Forced extinction of species ", orderedSpeciesInPatch[i].FormattedName,
                        " in patch ", patch.Name, ".");
                    results.KillSpeciesInPatch(orderedSpeciesInPatch[i], patch);
                }
            }

            return true;
        }
    }
}
