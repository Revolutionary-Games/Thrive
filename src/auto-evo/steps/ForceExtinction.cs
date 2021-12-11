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
    ///     TODO: Come up with a more refined solution, such as random extinction with bias for smaller populations.
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
            // We gather populations that may be targeted in the patch, depending on the parameters,
            // this may exclude migrated or newly created species.
            // Excluded species are only protected for one generation. The player is a target as well,
            // although they will be rescued before extinction can apply to them.
            var targetSpeciesPopulationsByPatch = results.GetPopulationsByPatch(
                !configuration.ProtectMigrationsFromSpeciesCap, true);

            var newSpecies = results.GetNewSpecies();

            foreach (var patch in patches)
            {
                IEnumerable<KeyValuePair<Species, long>> targetEnumerator = targetSpeciesPopulationsByPatch[patch];

                if (configuration.ProtectNewCellsFromSpeciesCap)
                {
                    targetEnumerator = targetEnumerator.Where(s => !newSpecies.Contains(s.Key));
                }

                var targetSpecies = targetEnumerator.ToList();

                // Only bother if we're above the limit
                if (targetSpecies.Count <= configuration.MaximumSpeciesInPatch)
                    continue;

                GD.Print("Running extinction step in patch ", patch.Name, ". ",
                    "Total count:", targetSpecies.Count);

                var orderedTargetSpecies = targetSpecies.OrderBy(s => s.Value).Select(s => s.Key);

                var speciesToRemoveCount = targetSpecies.Count - Math.Max(configuration.MaximumSpeciesInPatch, 0);

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
