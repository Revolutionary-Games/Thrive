namespace AutoEvo
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
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
        private readonly AutoEvoConfiguration configuration;

        public ForceExtinction(List<Patch> patches, AutoEvoConfiguration configuration)
        {
            this.patches = patches;
            this.configuration = configuration;
        }

        public bool CanRunConcurrently => false;

        public int TotalSteps => 1;

        public bool RunStep(RunResults results)
        {
            var populationsByPatch = results.GetPopulationsByPatch(!configuration.ProtectMigrationsFromSpeciesCap,
                !configuration.AllowNoMutation, !configuration.ProtectNewCellsFromSpeciesCap);

            foreach (Patch patch in patches)
            {
                var speciesInPatch = populationsByPatch[patch];

                // This does not take player into account as the species can never go extinct this way.
                var protectedSpeciesCount = 0;

                if (configuration.ProtectNewCellsFromSpeciesCap)
                    protectedSpeciesCount += results.GetNewSpeciesResults(patch).Count;
                if (configuration.ProtectMigrationsFromSpeciesCap)
                    protectedSpeciesCount += results.GetMigrationsTo(patch).Count;

                // Only bother if we're above the limit
                if (speciesInPatch.Count + protectedSpeciesCount <= configuration.MaximumSpeciesInPatch)
                    continue;

                GD.Print("Running extinction step in patch ", patch.Name, ". ",
                    "Total count:", speciesInPatch.Count + protectedSpeciesCount);

                // Sort the species in the patch, unless protected species fill up all the place already...
                var orderedSpeciesInPatch = (configuration.MaximumSpeciesInPatch > protectedSpeciesCount) ?
                    speciesInPatch.Keys.OrderBy(s => speciesInPatch[s]).ToArray() :
                    speciesInPatch.Keys.ToArray();

                var speciesToRemoveCount = speciesInPatch.Count - Math.Max(
                    configuration.MaximumSpeciesInPatch - protectedSpeciesCount, 0);

                // Remove worst-faring species, except for the player's and protected species
                for (int i = 0; i < speciesToRemoveCount; i++)
                {
                    if (orderedSpeciesInPatch[i].PlayerSpecies)
                        continue;

                    GD.Print("Forced extinction of species ", orderedSpeciesInPatch[i].FormattedName,
                        " in patch ", patch.Name, ".");
                    results.KillSpeciesInPatch(orderedSpeciesInPatch[i], patch,
                        configuration.RefundMigrationsInExtinctions);
                }
            }

            return true;
        }
    }
}
