namespace AutoEvo
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Linq;
    using System.Text;
    using Godot;

    /// <summary>
    ///   Container for results before they are applied.
    /// </summary>
    /// <remarks>
    ///   <para>
    ///     This is needed as earlier parts of an auto-evo run may not affect the latter parts
    ///   </para>
    /// </remarks>
    public class RunResults
    {
        /// <summary>
        ///   The per-species results
        /// </summary>
        /// <remarks>
        ///   <para>
        ///     This is a concurrent collection as multiple threads can read this at the same time. But when modifying
        ///     there's always an explicit lock so there doesn't seem to be a problem if this isn't a concurrent
        ///     collection, but just for piece of mind (and it doesn't seem to impact the performance much)
        ///     this is one.
        ///   </para>
        /// </remarks>
        private readonly ConcurrentDictionary<Species, SpeciesResult> results =
            new ConcurrentDictionary<Species, SpeciesResult>();

        public void AddMutationResultForSpecies(Species species, Species mutated)
        {
            MakeSureResultExistsForSpecies(species);

            results[species].MutatedProperties = mutated;
        }

        public void AddPopulationResultForSpecies(Species species, Patch patch, long newPopulation)
        {
            MakeSureResultExistsForSpecies(species);

            results[species].NewPopulationInPatches[patch] = Math.Max(newPopulation, 0);
        }

        public void AddMigrationResultForSpecies(Species species, Patch fromPatch, Patch toPatch, long populationAmount)
        {
            AddMigrationResultForSpecies(species, new SpeciesMigration(fromPatch, toPatch, populationAmount));
        }

        public void AddMigrationResultForSpecies(Species species, SpeciesMigration migration)
        {
            MakeSureResultExistsForSpecies(species);

            results[species].SpreadToPatches.Add(migration);
        }

        public void ApplyResults(GameWorld world, bool skipMutations)
        {
            foreach (var entry in results)
            {
                if (!skipMutations && entry.Value.MutatedProperties != null)
                {
                    entry.Key.ApplyMutation(entry.Value.MutatedProperties);
                }

                foreach (var populationEntry in entry.Value.NewPopulationInPatches)
                {
                    var patch = world.Map.GetPatch(populationEntry.Key.ID);

                    if (patch != null)
                    {
                        // We ignore the return value as population results are added for all existing patches for all
                        // species (if the species is not in the patch the population is 0 in the results)
                        patch.UpdateSpeciesPopulation(entry.Key, populationEntry.Value);
                    }
                    else
                    {
                        GD.PrintErr("RunResults has population of a species for invalid patch");
                    }
                }

                foreach (var spreadEntry in entry.Value.SpreadToPatches)
                {
                    var from = world.Map.GetPatch(spreadEntry.From.ID);
                    var to = world.Map.GetPatch(spreadEntry.To.ID);

                    if (from == null || to == null)
                    {
                        GD.PrintErr("RunResults has a species migration to/from an invalid patch");
                        continue;
                    }

                    long remainingPopulation = from.GetSpeciesPopulation(entry.Key) - spreadEntry.Population;
                    long newPopulation = to.GetSpeciesPopulation(entry.Key) + spreadEntry.Population;

                    if (!from.UpdateSpeciesPopulation(entry.Key, remainingPopulation))
                    {
                        GD.PrintErr("RunResults failed to update population for a species in a patch it moved from");
                    }

                    if (!to.UpdateSpeciesPopulation(entry.Key, newPopulation))
                    {
                        if (!to.AddSpecies(entry.Key, newPopulation))
                        {
                            GD.PrintErr("RunResults failed to update population and also add species failed on " +
                                "migration target patch");
                        }
                    }
                }
            }
        }

        /// <summary>
        ///   Sums up the populations of a species (ignores negative population)
        /// </summary>
        /// <remarks>
        ///   <para>
        ///     Throws an exception if no population is found
        ///   </para>
        /// </remarks>
        public long GetGlobalPopulation(Species species, bool resolveMoves = false)
        {
            long result = 0;

            foreach (var entry in results[species].NewPopulationInPatches)
            {
                var population = entry.Value;

                if (resolveMoves)
                {
                    foreach (var migration in results[species].SpreadToPatches)
                    {
                        if (migration.From == entry.Key)
                        {
                            population -= migration.Population;
                        }
                        else if (migration.To == entry.Key)
                        {
                            population += migration.Population;
                        }
                    }
                }

                result += Math.Max(population, 0);
            }

            // Find patches that were moved to but don't include population results
            if (resolveMoves)
            {
                foreach (var migration in results[species].SpreadToPatches)
                {
                    bool found = false;

                    foreach (var populationResult in results[species].NewPopulationInPatches)
                    {
                        if (migration.To == populationResult.Key)
                        {
                            found = true;
                            break;
                        }
                    }

                    if (!found)
                    {
                        result += Math.Max(migration.Population, 0);
                    }
                }
            }

            return result;
        }

        /// <summary>
        ///   variant of GetGlobalPopulation for a single patch
        /// </summary>
        public long GetPopulationInPatch(Species species, Patch patch)
        {
            return Math.Max(results[species].NewPopulationInPatches[patch], 0);
        }

        /// <summary>
        ///   Prints to log a summary of the results
        /// </summary>
        public void PrintSummary(Patch patch, PatchMap previousPopulations = null)
        {
            GD.Print("Start of auto-evo results summary (entries: ", results.Count, ")");

            GD.Print(MakeSummary(patch, previousPopulations));

            GD.Print("End of results summary");
        }

        /// <summary>
        ///   Makes summary text
        /// </summary>
        /// <param name="patch">The patch for which the summary should be generated</param>
        /// <param name="previousPopulations">If provided comparisons to previous populations is included</param>
        /// <param name="playerReadable">If true ids are removed from the output</param>
        /// <param name="effects">If not null these effects are applied to the population numbers</param>
        public string MakeSummary(Patch patch, PatchMap previousPopulations = null,
            bool playerReadable = false, List<ExternalEffect> effects = null)
        {
            const bool resolveMoves = true;

            var builder = new StringBuilder(500);

            string PatchString()
            {
                var builder2 = new StringBuilder(80);

                if (!playerReadable)
                {
                    builder2.Append(patch.ID);
                }

                builder2.Append(' ');
                builder2.Append(TranslationServer.Translate(patch.Name));

                return builder2.ToString();
            }

            void OutputPopulationForPatch(Species species, long population)
            {
                if (population > 0)
                {
                    builder.Append(PatchString());
                    builder.Append(' ');
                    builder.Append(TranslationServer.Translate("POPULATION"));
                    builder.Append(' ');
                    builder.Append(population);
                }
                else
                {
                    builder.Append(' ');
                    builder.Append(string.Format(CultureInfo.CurrentCulture,
                        TranslationServer.Translate("WENT_EXTINCT_IN"), PatchString()));
                }

                if (previousPopulations != null)
                {
                    builder.Append(' ');
                    builder.Append(TranslationServer.Translate("PREVIOUS"));
                    builder.Append(' ');
                    builder.Append(previousPopulations.GetPatch(patch.ID).GetSpeciesPopulation(species));
                }

                builder.Append('\n');
            }

            foreach (var entry in results.Values)
            {
                var playerDied = effects?.Any(e => e.PlayerDeath) == true;

                long adjustedPopulation = entry.NewPopulationInPatches[patch];

                if (resolveMoves)
                {
                    adjustedPopulation +=
                        CountSpeciesSpreadPopulation(entry.Species, patch);
                }

                // Apply external effects
                if (effects != null && previousPopulations != null &&
                    previousPopulations.CurrentPatch.ID == patch.ID)
                {
                    foreach (var effect in effects)
                    {
                        if (effect.Species == entry.Species)
                        {
                            var speciesPopulation = effect.Patch.GetSpeciesPopulation(effect.Species);
                            adjustedPopulation +=
                                effect.Constant + (long)(speciesPopulation * effect.Coefficient)
                                - speciesPopulation;
                        }
                    }
                }

                if (adjustedPopulation <= 0 &&
                    previousPopulations?.GetPatch(patch.ID).GetSpeciesPopulation(entry.Species) <= 0 &&
                    !playerDied &&
                    GetGlobalPopulation(entry.Species, resolveMoves) > 0)
                    continue;

                builder.Append(playerReadable ? entry.Species.FormattedName : entry.Species.FormattedIdentifier);
                builder.Append(":\n");

                if (entry.MutatedProperties != null)
                {
                    builder.Append(' ');
                    builder.Append(TranslationServer.Translate("RUN_RESULT_HAS_A_MUTATION"));

                    if (!playerReadable)
                    {
                        builder.Append(", ");
                        builder.Append(TranslationServer.Translate("RUN_RESULT_GENE_CODE"));
                        builder.Append(' ');
                        builder.Append(entry.MutatedProperties.StringCode);
                    }

                    builder.Append('\n');
                }

                // As the populations are added to all patches, even when the species is not there, we remove those
                // from output if there is currently no population in a patch and there isn't one in
                // previousPopulations
                var include = false;

                if (adjustedPopulation > 0)
                {
                    include = true;
                }
                else
                {
                    if (previousPopulations?.GetPatch(patch.ID).GetSpeciesPopulation(entry.Species) >
                        0 || playerDied)
                    {
                        include = true;
                    }
                }

                if (include)
                    OutputPopulationForPatch(entry.Species, adjustedPopulation);

                if (entry.SpreadToPatches.Count > 0)
                {
                    builder.Append(' ');
                    builder.Append(TranslationServer.Translate("RUN_RESULT_SPREAD_TO_PATCHES"));
                    builder.Append('\n');

                    foreach (var spreadEntry in entry.SpreadToPatches)
                    {
                        if (playerReadable)
                        {
                            builder.Append("  ");
                            builder.Append(string.Format(CultureInfo.CurrentCulture,
                                TranslationServer.Translate("RUN_RESULT_BY_SENDING_POPULATION"),
                                TranslationServer.Translate(spreadEntry.To.Name), spreadEntry.Population,
                                TranslationServer.Translate(spreadEntry.From.Name)));
                        }
                        else
                        {
                            builder.Append("  ");
                            builder.Append(TranslationServer.Translate(spreadEntry.To.Name));
                            builder.Append(" pop: ");
                            builder.Append(spreadEntry.Population);
                            builder.Append(" from: ");
                            builder.Append(TranslationServer.Translate(spreadEntry.From.Name));
                        }

                        builder.Append('\n');
                    }
                }

                if (GetGlobalPopulation(entry.Species, resolveMoves) <= 0)
                {
                    builder.Append(' ');
                    builder.Append(TranslationServer.Translate("WENT_EXTINCT_FROM_PLANET"));
                    builder.Append('\n');
                }
                else if (resolveMoves)
                {
                    // Also print new patches the species moved to (as the moves don't get
                    // included in newPopulationInPatches
                    foreach (var spreadEntry in entry.SpreadToPatches)
                    {
                        var found = false;

                        var to = spreadEntry.To;

                        foreach (var populationEntry in entry.NewPopulationInPatches)
                        {
                            if (populationEntry.Key == to)
                            {
                                found = true;
                                break;
                            }
                        }

                        if (!found)
                        {
                            OutputPopulationForPatch(entry.Species, CountSpeciesSpreadPopulation(entry.Species, to));
                        }
                    }
                }

                if (playerReadable)
                    builder.Append('\n');
            }

            return builder.ToString();
        }

        private void MakeSureResultExistsForSpecies(Species species)
        {
            lock (results)
            {
                if (results.ContainsKey(species))
                    return;

                results[species] = new SpeciesResult(species);
            }
        }

        private long CountSpeciesSpreadPopulation(Species species,
            Patch targetPatch)
        {
            long totalPopulation = 0;

            if (!results.ContainsKey(species))
            {
                GD.PrintErr("RunResults: no species entry found for counting spread population");
                return -1;
            }

            foreach (var entry in results[species].SpreadToPatches)
            {
                if (entry.From == targetPatch)
                {
                    totalPopulation -= entry.Population;
                }
                else if (entry.To == targetPatch)
                {
                    totalPopulation += entry.Population;
                }
            }

            return totalPopulation;
        }

        public class SpeciesResult
        {
            public Species Species;

            public Dictionary<Patch, long> NewPopulationInPatches = new Dictionary<Patch, long>();

            /// <summary>
            ///   null means no changes
            /// </summary>
            public Species MutatedProperties;

            /// <summary>
            ///   List of patches this species has spread to
            /// </summary>
            public List<SpeciesMigration> SpreadToPatches = new List<SpeciesMigration>();

            public SpeciesResult(Species species)
            {
                Species = species ?? throw new ArgumentException("species is null");
            }
        }
    }
}
