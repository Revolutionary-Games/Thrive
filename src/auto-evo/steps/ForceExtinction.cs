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
    ///     This was a hotfix to the 0.5.6.1 release.
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
            // We gather populations that may be targeted in the patch,
            // Depending on the parameters, this may exclude migrated or newly created species.
            // Excluded species are only protected for one generation, so the player is a target as well,
            // although they will be rescued if failing.
            var targetSpeciesPopulationsByPatch = results.GetPopulationsByPatch(
                !configuration.ProtectMigrationsFromSpeciesCap, !configuration.AllowNoMutation,
                !configuration.ProtectNewCellsFromSpeciesCap);

            foreach (var patch in patches)
            {
                var targetSpecies = targetSpeciesPopulationsByPatch[patch];

                // This does not take player into account as the player species can never go extinct this way.
                var protectedSpeciesCount = 0;

                if (configuration.ProtectNewCellsFromSpeciesCap)
                    protectedSpeciesCount += results.GetNewSpeciesResults(patch).Count;
                if (configuration.ProtectMigrationsFromSpeciesCap)
                    protectedSpeciesCount += results.GetMigrationsTo(patch).Count;

                // Only bother if we're above the limit
                if (targetSpecies.Count + protectedSpeciesCount <= configuration.MaximumSpeciesInPatch)
                    continue;

                GD.Print("Running extinction step in patch ", patch.Name, ". ",
                    "Total count:", targetSpecies.Count + protectedSpeciesCount);

                var orderedTargetSpecies = targetSpecies.OrderBy(s => s.Value).Select(s => s.Key);

                var speciesToRemoveCount = targetSpecies.Count - Math.Max(
                    configuration.MaximumSpeciesInPatch - protectedSpeciesCount, 0);

                // Remove worst-faring species in targets (which again exclude *temporarily* protected ones).
                foreach (var speciesToRemove in orderedTargetSpecies.Take(speciesToRemoveCount))
                {
                    // We rescue the player if needed
                    if (speciesToRemove.PlayerSpecies)
                        continue;

                    GD.Print("Forced extinction of species ", speciesToRemove.FormattedName,
                        " in patch ", patch.Name, ".");
                    results.KillSpeciesInPatch(speciesToRemove, patch,
                        configuration.RefundMigrationsInExtinctions);
                }
            }

            return true;
        }
    }
}
