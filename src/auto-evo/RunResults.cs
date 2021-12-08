namespace AutoEvo
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Globalization;
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

        public enum NewSpeciesType
        {
            /// <summary>
            ///   New species was created as there was lack of species / empty niche so to speak for it to fill
            /// </summary>
            FillNiche,

            /// <summary>
            ///   The new species was added due to experiencing different selection pressure
            /// </summary>
            SplitDueToMutation,
        }

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

        /// <summary>
        ///   Removes migrations specified for species for to/from patches where it split
        /// </summary>
        /// <param name="species">The species to check</param>
        public void RemoveMigrationsForSplitPatches(Species species)
        {
            SpeciesResult result;

            lock (results)
            {
                if (!results.TryGetValue(species, out result))
                    return;
            }

            if (result.SplitOffPatches == null)
                return;

            result.SpreadToPatches.RemoveAll(s =>
                result.SplitOffPatches.Contains(s.From) || result.SplitOffPatches.Contains(s.To));
        }

        public void AddNewSpecies(Species species, IEnumerable<KeyValuePair<Patch, long>> initialPatches,
            NewSpeciesType addType, Species parentSpecies)
        {
            MakeSureResultExistsForSpecies(species);

            results[species].NewlyCreated = addType;
            results[species].SplitFrom = parentSpecies;

            foreach (var initialPatch in initialPatches)
            {
                results[species].NewPopulationInPatches[initialPatch.Key] = Math.Max(initialPatch.Value, 0);
            }
        }

        public void AddSplitResultForSpecies(Species species, Species splitSpecies, List<Patch> patchesToConvert)
        {
            if (patchesToConvert == null || patchesToConvert.Count < 1)
                throw new ArgumentException("split patches is missing", nameof(patchesToConvert));

            MakeSureResultExistsForSpecies(species);
            MakeSureResultExistsForSpecies(splitSpecies);

            results[species].SplitOff = splitSpecies;
            results[species].SplitOffPatches = patchesToConvert;

            results[splitSpecies].NewlyCreated = NewSpeciesType.SplitDueToMutation;
            results[splitSpecies].SplitFrom = species;
        }

        public void ApplyResults(GameWorld world, bool skipMutations)
        {
            foreach (var entry in results)
            {
                if (entry.Value.NewlyCreated != null)
                {
                    world.RegisterAutoEvoCreatedSpecies(entry.Key);
                }

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

                if (entry.Value.NewlyCreated != null)
                {
                    // If we split off from a species that didn't take a population hit, we need to register ourselves
                    bool register = false;
                    if (entry.Value.SplitFrom == null)
                    {
                        register = true;
                    }
                    else if (results[entry.Value.SplitFrom].SplitOff != entry.Key)
                    {
                        register = true;
                    }

                    if (register)
                    {
                        foreach (var populationEntry in entry.Value.NewPopulationInPatches)
                        {
                            var patch = world.Map.GetPatch(populationEntry.Key.ID);

                            if (patch?.AddSpecies(entry.Key, populationEntry.Value) != true)
                            {
                                GD.PrintErr(
                                    "RunResults has new species with invalid patch or it was failed to be added");
                            }
                        }
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

                if (entry.Value.SplitOff != null)
                {
                    if (entry.Value.SplitOffPatches != null)
                    {
                        // Set populations to 0 for the patches that moved and replace the results for the split off
                        // species with those
                        foreach (var splitOffPatch in entry.Value.SplitOffPatches)
                        {
                            var patch = world.Map.GetPatch(splitOffPatch.ID);

                            if (patch == null)
                            {
                                GD.PrintErr("RunResults has a species split in an invalid patch");
                                continue;
                            }

                            var population = patch.GetSpeciesPopulation(entry.Key);

                            if (population <= 0)
                                continue;

                            if (!patch.UpdateSpeciesPopulation(entry.Key, 0))
                            {
                                GD.PrintErr("RunResults failed to update population for a species that split");
                            }

                            if (!patch.AddSpecies(entry.Value.SplitOff, population))
                            {
                                GD.PrintErr("RunResults failed to add species to patch that split off");
                            }
                        }
                    }
                    else
                    {
                        GD.PrintErr("List of split off patches is null, can't actually perform the split");
                    }
                }
            }
        }

        /// <summary>
        ///   Sums up the populations of a species (ignores negative population)
        /// </summary>
        /// <param name="species">The species to calculate population for</param>
        /// <param name="resolveMigrations">If true migrations effects on population are taken into account</param>
        /// <param name="resolveSplits">If true species splits are taken into account in population numbers</param>
        /// <returns>The global population</returns>
        /// <remarks>
        ///   <para>
        ///     Throws an exception if no population is found
        ///   </para>
        /// </remarks>
        public long GetGlobalPopulation(Species species, bool resolveMigrations = false, bool resolveSplits = false)
        {
            long result = 0;

            foreach (var entry in results[species].NewPopulationInPatches)
            {
                if (!resolveMigrations)
                {
                    result += Math.Max(entry.Value, 0);
                    continue;
                }

                var adjustedPopulation = entry.Value;

                foreach (var migration in results[species].SpreadToPatches)
                {
                    if (migration.From == entry.Key)
                    {
                        adjustedPopulation -= migration.Population;
                    }
                    else if (migration.To == entry.Key)
                    {
                        adjustedPopulation += migration.Population;
                    }
                }

                if (resolveSplits && results[species].SplitOffPatches?.Contains(entry.Key) == true)
                {
                    adjustedPopulation = 0;
                }

                result += Math.Max(adjustedPopulation, 0);
            }

            // Find patches that were migrated to but don't include population results
            if (resolveMigrations)
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

            // Patches that were this split off in
            if (resolveSplits && results[species].SplitFrom != null)
            {
                var splitFromData = results[results[species].SplitFrom];

                if (splitFromData.SplitOffPatches != null && splitFromData.SplitOff == species)
                {
                    foreach (var patch in splitFromData.SplitOffPatches)
                    {
                        result += Math.Max(splitFromData.NewPopulationInPatches[patch], 0);
                    }
                }
            }

            return result;
        }

        /// <summary>
        ///   Variant of GetGlobalPopulation for a single patch
        /// </summary>
        public long GetPopulationInPatch(Species species, Patch patch)
        {
            return Math.Max(results[species].NewPopulationInPatches[patch], 0);
        }

        /// <summary>
        ///   Variant of GetGlobalPopulation for a single patch that returns null if patch not found
        /// </summary>
        public long? GetPopulationInPatchIfExists(Species species, Patch patch)
        {
            if (results[species].NewPopulationInPatches.TryGetValue(patch, out long population))
            {
                return Math.Max(population, 0);
            }

            return null;
        }

        /// <summary>
        ///   Returns all patches that have population for the given species
        /// </summary>
        /// <param name="species">The species to get population for</param>
        /// <returns>The patches along with the population amount</returns>
        public IEnumerable<KeyValuePair<Patch, long>> GetPopulationInPatches(Species species)
        {
            foreach (var newPopulationInPatch in results[species].NewPopulationInPatches)
            {
                if (newPopulationInPatch.Value <= 0)
                    continue;

                yield return newPopulationInPatch;
            }
        }

        /// <summary>
        ///   Prints to log a summary of the results
        /// </summary>
        public void PrintSummary(GameWorld world, PatchMap previousPopulations = null)
        {
            GD.Print("Start of auto-evo results summary (entries: ", results.Count, ")");

            GD.Print(MakeSummary(world, previousPopulations));

            GD.Print("End of results summary");
        }

        /// <summary>
        ///   Makes summary text
        /// </summary>
        /// <param name="world">The current global game data</param>
        /// <param name="previousPopulations">If provided comparisons to previous populations is included</param>
        /// <param name="playerReadable">if true ids are removed from the output</param>
        /// <param name="effects">if not null these effects are applied to the population numbers</param>
        /// <returns>The generated summary text</returns>
        public string MakeSummary(GameWorld world, PatchMap previousPopulations = null,
            bool playerReadable = false, List<ExternalEffect> effects = null)
        {
            const bool resolveMigrations = true;
            const bool resolveSplits = true;

            var builder = new StringBuilder(500);

            string PatchString(Patch patch)
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

            void OutputPopulationForPatch(Species species, Patch patch, long population)
            {
                builder.Append("  ");
                var patchName = PatchString(patch);

                if (population > 0)
                {
                    builder.Append(patchName);
                    builder.Append(' ');
                    builder.Append(TranslationServer.Translate("POPULATION"));
                    builder.Append(' ');
                    builder.Append(population);
                }
                else
                {
                    // For some reason this line had one more space padding than the case that the population
                    // wasn't extinct in this patch

                    builder.Append(string.Format(CultureInfo.CurrentCulture,
                        TranslationServer.Translate("WENT_EXTINCT_IN"), patchName));
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
                builder.Append(playerReadable ? entry.Species.FormattedName : entry.Species.FormattedIdentifier);
                builder.Append(":\n");

                if (entry.SplitFrom != null)
                {
                    builder.Append(' ');
                    builder.Append(string.Format(CultureInfo.CurrentCulture,
                        TranslationServer.Translate("RUN_RESULT_SPLIT_FROM"),
                        playerReadable ? entry.SplitFrom.FormattedName : entry.SplitFrom.FormattedIdentifier));

                    builder.Append('\n');
                }

                if (entry.NewlyCreated != null)
                {
                    builder.Append(' ');

                    switch (entry.NewlyCreated.Value)
                    {
                        case NewSpeciesType.FillNiche:
                            builder.Append(TranslationServer.Translate("RUN_RESULT_NICHE_FILL"));
                            world.LogWorldEvent("[u]" + entry.Species.FormattedName + "[/u]" + " has split into " + entry.Species.FormattedName + " to fill a niche");
                            break;
                        case NewSpeciesType.SplitDueToMutation:
                            builder.Append(TranslationServer.Translate("RUN_RESULT_SELECTION_PRESSURE_SPLIT"));
                            world.LogWorldEvent("[u]" + entry.Species.FormattedName + "[/u]" + " has split into " + entry.Species.FormattedName + " due to differing selection pressures");
                            break;
                        default:
                            GD.PrintErr("Unhandled newly created species type: ", entry.NewlyCreated.Value);
                            builder.Append(entry.NewlyCreated.Value);
                            break;
                    }

                    builder.Append('\n');
                }

                if (entry.SplitOff != null)
                {
                    if (entry.SplitOffPatches == null)
                        throw new InvalidOperationException("List of split off patches is null");

                    builder.Append(' ');
                    builder.Append(string.Format(CultureInfo.CurrentCulture,
                        TranslationServer.Translate("RUN_RESULT_SPLIT_OFF_TO"),
                        playerReadable ? entry.SplitOff.FormattedName : entry.SplitOff.FormattedIdentifier));
                    builder.Append('\n');

                    foreach (var patch in entry.SplitOffPatches)
                    {
                        builder.Append("   ");

                        builder.Append(TranslationServer.Translate(patch.Name));
                        builder.Append('\n');
                    }
                }

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

                if (entry.SpreadToPatches.Count > 0)
                {
                    builder.Append(' ');
                    builder.Append(TranslationServer.Translate("RUN_RESULT_SPREAD_TO_PATCHES"));
                    builder.Append('\n');

                    var numberOfPatches = 0;

                    foreach (var spreadEntry in entry.SpreadToPatches)
                    {
                        ++numberOfPatches;

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

                    if (numberOfPatches < 2)
                    {
                        world.LogWorldEvent("[u]" + entry.Species.FormattedName + "[/u]" + " has migrated to " + TranslationServer.Translate(entry.SpreadToPatches[0].To.Name));
                    }
                    else
                    {
                        world.LogWorldEvent("[u]" + entry.Species.FormattedName + "[/u]" + " has migrated to multiple patches");
                    }
                }

                builder.Append(' ');
                builder.Append(TranslationServer.Translate("RUN_RESULT_POP_IN_PATCHES"));
                builder.Append('\n');

                foreach (var patchPopulation in entry.NewPopulationInPatches)
                {
                    long adjustedPopulation = patchPopulation.Value;

                    if (resolveMigrations)
                    {
                        adjustedPopulation +=
                            CountSpeciesSpreadPopulation(entry.Species, patchPopulation.Key);
                    }

                    if (resolveSplits)
                    {
                        if (entry.SplitOffPatches?.Contains(patchPopulation.Key) == true)
                        {
                            // All population splits off
                            adjustedPopulation = 0;
                        }
                    }

                    // Apply external effects
                    if (effects != null && previousPopulations != null &&
                        previousPopulations.CurrentPatch.ID == patchPopulation.Key.ID)
                    {
                        foreach (var effect in effects)
                        {
                            if (effect.Species == entry.Species)
                            {
                                adjustedPopulation +=
                                    effect.Constant + (long)(effect.Species.Population * effect.Coefficient)
                                    - effect.Species.Population;
                            }
                        }
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
                        if (previousPopulations?.GetPatch(patchPopulation.Key.ID).GetSpeciesPopulation(entry.Species) >
                            0)
                        {
                            include = true;
                        }
                    }

                    if (include)
                        OutputPopulationForPatch(entry.Species, patchPopulation.Key, adjustedPopulation);
                }

                // Also print new patches the species moved to (as the moves don't get
                // included in newPopulationInPatches
                if (resolveMigrations)
                {
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
                            OutputPopulationForPatch(entry.Species, to,
                                CountSpeciesSpreadPopulation(entry.Species, to));
                        }
                    }
                }

                // Print populations from splits
                // Warning suppressed on resolveSplits to allow keeping the variable
                // ReSharper disable once RedundantLogicalConditionalExpressionOperand
                if (resolveSplits && entry.SplitFrom != null)
                {
                    var splitFrom = results[entry.SplitFrom];

                    // Skip if the SplitFrom variable was used just to indicate this didn't pop out of thin air
                    if (splitFrom.SplitOff == entry.Species)
                    {
                        foreach (var patchPopulation in splitFrom.SplitOffPatches)
                        {
                            OutputPopulationForPatch(entry.Species, patchPopulation,
                                splitFrom.NewPopulationInPatches[patchPopulation]);
                        }
                    }
                }

                var globalPopulation = GetGlobalPopulation(entry.Species, resolveMigrations, resolveSplits);
                var previousTotalPopulations = previousPopulations.GetSpeciesPopulation(entry.Species);

                if (globalPopulation <= 0)
                {
                    builder.Append(' ');
                    builder.Append(TranslationServer.Translate("WENT_EXTINCT_FROM_PLANET"));
                    builder.Append('\n');

                    world.LogWorldEvent("[u]" + entry.Species.FormattedName + "[/u]" + " has gone extinct!",
                        "res://assets/textures/gui/bevel/SuicideIcon.png");
                }
                else
                {
                    if (previousPopulations != null && previousTotalPopulations > 0 &&
                        globalPopulation != previousTotalPopulations)
                    {
                        if (globalPopulation > previousTotalPopulations)
                        {
                            world.LogWorldEvent(
                                "[u]" + entry.Species.FormattedName + "[/u]" + " populations have increased to " + globalPopulation,
                                "res://assets/textures/gui/bevel/increase.png");
                        }
                        else
                        {
                            world.LogWorldEvent(
                                "[u]" + entry.Species.FormattedName + "[/u]" + " populations have decreased to " + globalPopulation,
                                "res://assets/textures/gui/bevel/decrease.png");
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
            if (species == null)
                throw new ArgumentException("species to add result to is null", nameof(species));

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

            /// <summary>
            ///   If not null, this is a new species that was created
            /// </summary>
            public NewSpeciesType? NewlyCreated;

            /// <summary>
            ///   If set, the specified species split off from this species taking all the population listed in
            ///   <see cref="SplitOffPatches"/>
            /// </summary>
            public Species SplitOff;

            /// <summary>
            ///   Patches that moved to the split off population
            /// </summary>
            public List<Patch> SplitOffPatches;

            /// <summary>
            ///   Info on which species this split from. Not used for anything other than informational display
            /// </summary>
            public Species SplitFrom;

            public SpeciesResult(Species species)
            {
                Species = species ?? throw new ArgumentException("species is null");
            }
        }
    }
}
