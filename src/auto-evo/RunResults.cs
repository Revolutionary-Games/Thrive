namespace AutoEvo
{
    using System;
    using System.Collections;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Linq;
    using Godot;

    /// <summary>
    ///   Container for results before they are applied.
    /// </summary>
    /// <remarks>
    ///   <para>
    ///     This is needed as earlier parts of an auto-evo run may not affect the latter parts
    ///   </para>
    /// </remarks>
    public class RunResults : IEnumerable<KeyValuePair<Species, RunResults.SpeciesResult>>
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
        private readonly ConcurrentDictionary<Species, SpeciesResult> results = new();

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

        /// <summary>
        ///   Per-species results. Species are cloned if they've changed to retain contemporary data and set to null if
        ///   they haven't to reduce save file size.
        /// </summary>
        /// <returns>The per-species results with changed species cloned and unchanged species set to null</returns>
        public Dictionary<uint, SpeciesRecordLite> GetSpeciesRecords()
        {
            return results.ToDictionary(r => r.Key.ID, r => new SpeciesRecordLite(
                HasSpeciesChanged(r.Value) ? (Species)r.Key.Clone() : null, r.Key.Population,
                r.Value.MutatedProperties?.ID, r.Value.SplitFrom?.ID));
        }

        /// <summary>
        ///   Per-species results with all species data. All species are cloned.
        /// </summary>
        /// <returns>The per-species results with all species cloned</returns>
        public Dictionary<uint, SpeciesRecordFull> GetFullSpeciesRecords()
        {
            return results.ToDictionary(r => r.Key.ID,
                r => new SpeciesRecordFull((Species)r.Key.Clone(), r.Key.Population, r.Value.MutatedProperties?.ID,
                    r.Value.SplitFrom?.ID));
        }

        public void AddMutationResultForSpecies(Species species, Species? mutated)
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
            if (populationAmount <= 0)
                throw new ArgumentException("Invalid population migration amount");

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

        public void AddNewSpecies(Species species, IEnumerable<KeyValuePair<Patch, long>> initialPopulationInPatches,
            NewSpeciesType addType, Species parentSpecies)
        {
            MakeSureResultExistsForSpecies(species);

            results[species].NewlyCreated = addType;
            results[species].SplitFrom = parentSpecies;

            foreach (var patchPopulation in initialPopulationInPatches)
            {
                results[species].NewPopulationInPatches[patchPopulation.Key] = Math.Max(patchPopulation.Value, 0);
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

        public void KillSpeciesInPatch(Species species, Patch patch, bool refundMigrations = false)
        {
            AddPopulationResultForSpecies(species, patch, 0);

            var speciesResult = results[species];

            // We copy migration list to be able to modify it
            foreach (var migration in speciesResult.SpreadToPatches.ToList())
            {
                if (speciesResult.SplitOff != null && speciesResult.SplitOffPatches?.Contains(patch) == true)
                    continue;

                if (migration.To == patch)
                {
                    speciesResult.SpreadToPatches.Remove(migration);

                    // We may still penalize the origin patch, the migration would just have died off on its way.
                    // TODO: It would be nice to leave some trace of this happening, so that it can be tracked
                    // why the population in this patch was reduced.
                    if (!refundMigrations)
                        speciesResult.NewPopulationInPatches[migration.From] -= migration.Population;
                }
            }
        }

        public void AddTrackedEnergyForSpecies(MicrobeSpecies species, Patch patch, FoodSource niche,
            float speciesFitness, float speciesEnergy, float totalFitness)
        {
            if (niche == null)
                throw new ArgumentException("niche is missing", nameof(niche));

            MakeSureResultExistsForSpecies(species);

            var dataReceiver = results[species].GetEnergyResults(patch);

            var nicheDescription = niche.GetDescription();
            dataReceiver.PerNicheEnergy[nicheDescription] = new SpeciesPatchEnergyResults.NicheInfo
            {
                CurrentSpeciesFitness = speciesFitness,
                CurrentSpeciesEnergy = speciesEnergy,
                TotalFitness = totalFitness,
                TotalAvailableEnergy = niche.TotalEnergyAvailable(),
            };
        }

        public void AddTrackedEnergyConsumptionForSpecies(MicrobeSpecies species, Patch patch,
            long unadjustedPopulation, float totalEnergy, float individualCost)
        {
            MakeSureResultExistsForSpecies(species);

            var dataReceiver = results[species].GetEnergyResults(patch);

            dataReceiver.UnadjustedPopulation = unadjustedPopulation;
            dataReceiver.TotalEnergyGathered = totalEnergy;
            dataReceiver.IndividualCost = individualCost;
        }

        /// <summary>
        ///   Checks if species has results. Species doesn't have results if it was extinct or was not part of the run
        /// </summary>
        /// <param name="species">The species to check</param>
        /// <returns>True if the species has results</returns>
        public bool SpeciesHasResults(Species species)
        {
            lock (results)
            {
                return results.ContainsKey(species);
            }
        }

        /// <summary>
        ///   Creates a blank results for the player species
        /// </summary>
        /// <param name="playerSpecies">The player species</param>
        /// <param name="patchesToFillResultsFor">
        ///   Patches to add results for if the species is missing from results. All patches in the used map need
        ///   to be included here, otherwise population counting will throw an exception later.
        /// </param>
        /// <exception cref="ArgumentException">If species is not valid</exception>
        /// <remarks>
        ///   <para>
        ///     When the player has 0 global population, but has not lost the game due to making it to the editor,
        ///     the player species wouldn't have any results, but as that breaks a lot of stuff we need to create blank
        ///     results for the player in that case. <see cref="AutoEvoRun.AddPlayerSpeciesPopulationChangeClampStep"/>
        ///     handles calling this.
        ///   </para>
        /// </remarks>
        public void AddPlayerSpeciesBlankResult(Species playerSpecies, IEnumerable<Patch> patchesToFillResultsFor)
        {
            if (!playerSpecies.PlayerSpecies)
                throw new ArgumentException("Species must be player species");

            lock (results)
            {
                if (results.ContainsKey(playerSpecies))
                    return;

                var result = new SpeciesResult(playerSpecies);

                // All patches need to have a population result for population counting to work
                foreach (var patch in patchesToFillResultsFor)
                {
                    result.NewPopulationInPatches[patch] = 0;
                }

                results[playerSpecies] = result;
            }
        }

        public void ApplyResults(GameWorld world, bool skipMutations)
        {
            foreach (var entry in results)
            {
                if (entry.Value.NewlyCreated != null)
                {
                    // Summary creation needs to already have species IDs so it might have already registered this
                    world.RegisterAutoEvoCreatedSpeciesIfNotAlready(entry.Key);
                }

                if (!skipMutations && entry.Value.MutatedProperties != null)
                {
                    entry.Key.ApplyMutation(entry.Value.MutatedProperties);
                }

                foreach (var populationEntry in entry.Value.NewPopulationInPatches)
                {
                    var patch = world.Map.GetPatch(populationEntry.Key.ID);

                    // We ignore the return value as population results are added for all existing patches for all
                    // species (if the species is not in the patch the population is 0 in the results)
                    patch.UpdateSpeciesSimulationPopulation(entry.Key, populationEntry.Value);
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

                            if (patch.AddSpecies(entry.Key, populationEntry.Value) != true)
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

                    long remainingPopulation = from.GetSpeciesSimulationPopulation(entry.Key) - spreadEntry.Population;
                    long newPopulation = to.GetSpeciesSimulationPopulation(entry.Key) + spreadEntry.Population;

                    if (!from.UpdateSpeciesSimulationPopulation(entry.Key, remainingPopulation))
                    {
                        GD.PrintErr("RunResults failed to update population for a species in a patch it moved from");
                    }

                    if (!to.UpdateSpeciesSimulationPopulation(entry.Key, newPopulation))
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
                        // Set populations to 0 for the patches that split off and use the populations for the split
                        // off species
                        foreach (var splitOffPatch in entry.Value.SplitOffPatches)
                        {
                            var patch = world.Map.GetPatch(splitOffPatch.ID);

                            var population = patch.GetSpeciesSimulationPopulation(entry.Key);

                            if (population <= 0)
                                continue;

                            if (!patch.UpdateSpeciesSimulationPopulation(entry.Key, 0))
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

            world.Map.DiscardGameplayPopulations();
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
        ///     Throws an exception if no population result is found for the species
        ///   </para>
        /// </remarks>
        public long GetGlobalPopulation(Species species, bool resolveMigrations = false, bool resolveSplits = false)
        {
            return GetSpeciesPopulationsByPatch(species, resolveMigrations, resolveSplits).Sum(e => e.Value);
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
        ///   Computes the final population of all species, by patch.
        /// </summary>
        /// <param name="resolveMigrations">If true migrations effects on population are taken into account</param>
        /// <param name="resolveSplits">If true species splits are taken into account in population numbers</param>
        /// <returns>The global population</returns>
        public Dictionary<Patch, Dictionary<Species, long>> GetPopulationsByPatch(bool resolveMigrations = false,
            bool resolveSplits = false)
        {
            var speciesInPatches = new Dictionary<Patch, Dictionary<Species, long>>();
            foreach (var species in results.Keys)
            {
                var populations = GetSpeciesPopulationsByPatch(species, resolveMigrations, resolveSplits);
                foreach (var patchEntry in populations)
                {
                    if (!speciesInPatches.TryGetValue(patchEntry.Key, out var populationsInPatch))
                    {
                        populationsInPatch = new Dictionary<Species, long>();
                        speciesInPatches[patchEntry.Key] = populationsInPatch;
                    }

                    populationsInPatch.Add(species, patchEntry.Value);
                }
            }

            return speciesInPatches;
        }

        /// <summary>
        ///   Computes the final population of a species, by patch.
        /// </summary>
        /// <remarks>
        ///   <para>
        ///     Species are only returned if their population is above 0 (before migrations *from*
        ///     the patch are applied).
        ///   </para>
        /// </remarks>
        /// <param name="species">The species to calculate population for</param>
        /// <param name="resolveMigrations">If true migrations effects on population are taken into account</param>
        /// <param name="resolveSplits">If true species splits are taken into account in population numbers</param>
        /// <exception cref="ArgumentException">If the given species has no results</exception>
        /// <exception cref="InvalidOperationException">If there is invalid results data encountered</exception>
        /// <returns>A dictionary of patch and the populations in that patch for species</returns>
        public Dictionary<Patch, long> GetSpeciesPopulationsByPatch(Species species,
            bool resolveMigrations = false, bool resolveSplits = false)
        {
            var populationResults = new Dictionary<Patch, long>();

            if (!results.TryGetValue(species, out var speciesResult))
                throw new ArgumentException("Species " + species.FormattedName + " not found in results.");

            // Get natural variations
            foreach (var patchPopulationEntry in speciesResult.NewPopulationInPatches)
            {
                // This check is first so that empty patches are not returned as per the remarks
                if (patchPopulationEntry.Value <= 0)
                    continue;

                if (resolveSplits && results[species].SplitOffPatches?.Contains(patchPopulationEntry.Key) == true)
                    continue;

                populationResults[patchPopulationEntry.Key] = patchPopulationEntry.Value;
            }

            if (resolveMigrations)
            {
                // Apply migrations
                foreach (var migration in speciesResult.SpreadToPatches)
                {
                    if (migration.From == null || migration.To == null)
                        throw new InvalidOperationException("Found invalid migration in auto-evo results: null patch!");

                    // Zero population migrations are not valid, though the migration add method should have verified
                    // this already
                    if (migration.Population <= 0)
                    {
                        throw new ArgumentException(
                            "Found invalid migration in auto-evo results: negative or null population!");
                    }

                    // Only consider possible migrations
                    if (populationResults.TryGetValue(migration.From, out var populationsFrom))
                    {
                        populationResults.TryGetValue(migration.To, out var populationsTo);

                        // In the case the population is lower than migration, we assume as much population moves
                        // as possible. This is possible to happen because this code can be called after the population
                        // numbers have been calculated meaning that migrations that use *previous* step population
                        // don't look valid here
                        var migrationAmount = Math.Max(Math.Min(migration.Population, populationsFrom), 0);

                        // We remove the migrating population, and species that reach zero population...
                        if (populationsFrom - migrationAmount == 0)
                        {
                            populationResults.Remove(migration.From);
                        }
                        else
                        {
                            populationResults[migration.From] = populationsFrom - migrationAmount;
                        }

                        // ...and we add the new population to the moved to patch.
                        populationResults[migration.To] = populationsTo + migrationAmount;
                    }
                }
            }

            return populationResults;
        }

        public List<Species> GetNewSpecies()
        {
            var newSpecies = new List<Species>();

            foreach (var resultEntry in results)
            {
                if (resultEntry.Value.NewlyCreated != null)
                    newSpecies.Add(resultEntry.Key);
            }

            return newSpecies;
        }

        public Dictionary<Species, SpeciesMigration> GetMigrationsTo(Patch patch)
        {
            var migrationsToPatch = new Dictionary<Species, SpeciesMigration>();

            foreach (var resultEntry in results)
            {
                foreach (var migration in resultEntry.Value.SpreadToPatches)
                {
                    // Theoretically, nothing prevents migration from several patches, so no continue.
                    if (migration.To == patch)
                        migrationsToPatch.Add(resultEntry.Key, migration);
                }
            }

            return migrationsToPatch;
        }

        public Dictionary<Patch, SpeciesPatchEnergyResults> GetPatchEnergyResults(Species species)
        {
            return results[species].EnergyResults;
        }

        /// <summary>
        ///   Summary creation needs to already have species IDs set, so this makes sure all new species are registered
        ///   and have proper IDs set.
        /// </summary>
        /// <param name="world">World to register new species in</param>
        public void RegisterNewSpeciesForSummary(GameWorld world)
        {
            foreach (var entry in results.Values)
            {
                if (entry.NewlyCreated != null)
                {
                    world.RegisterAutoEvoCreatedSpeciesIfNotAlready(entry.Species);
                }
            }
        }

        /// <summary>
        ///   Makes summary text
        /// </summary>
        /// <param name="previousPopulations">If provided comparisons to previous populations is included</param>
        /// <param name="playerReadable">If true ids are removed from the output</param>
        /// <param name="effects">
        ///   If not null these effects are applied to the population numbers.
        ///   Must be final effects with <see cref="ExternalEffect.Coefficient"/> set to 1 created by
        ///   <see cref="AutoEvoRun.CalculateAndApplyFinalExternalEffectSizes"/>
        /// </param>
        /// <returns>The generated summary text</returns>
        public LocalizedStringBuilder MakeSummary(PatchMap? previousPopulations = null,
            bool playerReadable = false, List<ExternalEffect>? effects = null)
        {
            if (previousPopulations != null && previousPopulations.CurrentPatch == null)
                throw new ArgumentException("When previous populations is set, it must have current patch set");

            const bool resolveMigrations = true;
            const bool resolveSplits = true;

            var builder = new LocalizedStringBuilder(500);

            LocalizedStringBuilder PatchString(Patch patch)
            {
                var builder2 = new LocalizedStringBuilder(80);

                // Patch visibility is ignored if the output is not read by the player
                if (!playerReadable)
                {
                    builder2.Append(patch.ID);
                    builder2.Append(' ');
                    builder2.Append(patch.Name);

                    return builder2;
                }

                builder2.Append(' ');
                builder2.Append(patch.VisibleName);

                return builder2;
            }

            void OutputPopulationForPatch(Species species, Patch patch, long population)
            {
                builder.Append("  ");
                var patchName = PatchString(patch);

                if (population > 0)
                {
                    builder.Append(patchName);
                    builder.Append(' ');
                    builder.Append(new LocalizedString("POPULATION_COLON"));
                    builder.Append(' ');
                    builder.Append(population);
                }
                else
                {
                    // For some reason this line had one more space padding than the case that the population
                    // wasn't extinct in this patch

                    builder.Append(new LocalizedString("WENT_EXTINCT_IN", patchName));
                }

                if (previousPopulations != null)
                {
                    builder.Append(' ');
                    builder.Append(new LocalizedString("PREVIOUS_COLON"));
                    builder.Append(' ');
                    builder.Append(previousPopulations.GetPatch(patch.ID).GetSpeciesSimulationPopulation(species));
                }

                builder.Append('\n');
            }

            foreach (var entry in
                     results.Values.OrderByDescending(s => s.Species.PlayerSpecies)
                         .ThenBy(s => s.Species.FormattedName))
            {
                builder.Append(playerReadable ? entry.Species.FormattedNameBbCode : entry.Species.FormattedIdentifier);
                builder.Append(":\n");

                if (entry.SplitFrom != null)
                {
                    builder.Append(' ');
                    builder.Append(new LocalizedString("RUN_RESULT_SPLIT_FROM",
                        playerReadable ? entry.SplitFrom.FormattedNameBbCode : entry.SplitFrom.FormattedIdentifier));

                    builder.Append('\n');
                }

                if (entry.NewlyCreated != null)
                {
                    builder.Append(' ');

                    switch (entry.NewlyCreated.Value)
                    {
                        case NewSpeciesType.FillNiche:
                            builder.Append(new LocalizedString("RUN_RESULT_NICHE_FILL"));
                            break;
                        case NewSpeciesType.SplitDueToMutation:
                            builder.Append(new LocalizedString("RUN_RESULT_SELECTION_PRESSURE_SPLIT"));
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
                    builder.Append(new LocalizedString("RUN_RESULT_SPLIT_OFF_TO",
                        playerReadable ? entry.SplitOff.FormattedNameBbCode : entry.SplitOff.FormattedIdentifier));
                    builder.Append('\n');

                    foreach (var patch in entry.SplitOffPatches)
                    {
                        builder.Append("   ");

                        if (playerReadable)
                        {
                            builder.Append(patch.VisibleName);
                        }
                        else
                        {
                            builder.Append(patch.Name);
                        }

                        builder.Append('\n');
                    }
                }

                if (entry.MutatedProperties != null)
                {
                    builder.Append(' ');
                    builder.Append(new LocalizedString("SPECIES_HAS_A_MUTATION"));

                    if (!playerReadable)
                    {
                        builder.Append(", ");
                        builder.Append(new LocalizedString("RUN_RESULT_GENE_CODE"));
                        builder.Append(' ');
                        builder.Append(entry.MutatedProperties.StringCode);
                    }

                    builder.Append('\n');
                }

                if (entry.SpreadToPatches.Count > 0)
                {
                    builder.Append(' ');
                    builder.Append(new LocalizedString("SPREAD_TO_PATCHES"));
                    builder.Append('\n');

                    foreach (var spreadEntry in entry.SpreadToPatches)
                    {
                        if (playerReadable)
                        {
                            builder.Append("  ");
                            builder.Append(new LocalizedString("RUN_RESULT_BY_SENDING_POPULATION",
                                spreadEntry.To.VisibleName, spreadEntry.Population,
                                spreadEntry.From.VisibleName));
                        }
                        else
                        {
                            builder.Append("  ");
                            builder.Append(spreadEntry.To.Name);
                            builder.Append(" pop: ");
                            builder.Append(spreadEntry.Population);
                            builder.Append(" from: ");
                            builder.Append(spreadEntry.From.Name);
                        }

                        builder.Append('\n');
                    }
                }

                builder.Append(' ');
                builder.Append(new LocalizedString("POPULATION_IN_PATCHES"));
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
                        if (previousPopulations?.GetPatch(patchPopulation.Key.ID)
                                .GetSpeciesSimulationPopulation(entry.Species) > 0)
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
                        if (splitFrom.SplitOffPatches == null)
                            throw new Exception("Split off patches is null for a split species");

                        foreach (var patchPopulation in splitFrom.SplitOffPatches)
                        {
                            OutputPopulationForPatch(entry.Species, patchPopulation,
                                splitFrom.NewPopulationInPatches[patchPopulation]);
                        }
                    }
                }

                if (GetGlobalPopulation(entry.Species, resolveMigrations, resolveSplits) <= 0)
                {
                    builder.Append(' ');
                    builder.Append(new LocalizedString("WENT_EXTINCT_FROM_PLANET"));
                    builder.Append('\n');
                }

                if (playerReadable)
                    builder.Append('\n');
            }

            return builder;
        }

        public void LogResultsToTimeline(GameWorld world, List<ExternalEffect>? effects = null)
        {
            if (world.Map.CurrentPatch == null)
                throw new ArgumentException("world must have current patch set");

            var newSpecies = GetNewSpecies();

            foreach (var patch in world.Map.Patches.Values)
            {
                foreach (var species in patch.SpeciesInPatch.Keys)
                {
                    long globalPopulation = GetGlobalPopulation(species, true, true);

                    var previousGlobalPopulation = world.Map.GetSpeciesGlobalSimulationPopulation(species);

                    var unadjustedPopulation = GetPopulationInPatch(species, patch);
                    var finalPatchPopulation = unadjustedPopulation;
                    var previousPatchPopulation = patch.GetSpeciesSimulationPopulation(species);

                    finalPatchPopulation += CountSpeciesSpreadPopulation(species, patch);

                    if (results[species].SplitOffPatches?.Contains(patch) == true)
                    {
                        // All population splits off
                        finalPatchPopulation = 0;
                    }

                    if (globalPopulation <= 0)
                    {
                        // TODO: see https://github.com/Revolutionary-Games/Thrive/issues/2958
                        LogEventGloballyAndLocally(world, patch,
                            new LocalizedString("TIMELINE_SPECIES_EXTINCT", species.FormattedName),
                            species.PlayerSpecies, "extinction.png");

                        continue;
                    }

                    if (finalPatchPopulation > 0 && finalPatchPopulation != previousPatchPopulation)
                    {
                        if (finalPatchPopulation > previousPatchPopulation)
                        {
                            patch.LogEvent(new LocalizedString("TIMELINE_SPECIES_POPULATION_INCREASE",
                                    species.FormattedName, finalPatchPopulation),
                                species.PlayerSpecies, "popUp.png");
                        }
                        else
                        {
                            patch.LogEvent(new LocalizedString("TIMELINE_SPECIES_POPULATION_DECREASE",
                                    species.FormattedName, finalPatchPopulation),
                                species.PlayerSpecies, "popDown.png");
                        }
                    }
                    else
                    {
                        patch.LogEvent(new LocalizedString("TIMELINE_SPECIES_EXTINCT_LOCAL", species.FormattedName),
                            species.PlayerSpecies, "extinctionLocal.png");
                    }

                    if (globalPopulation != previousGlobalPopulation)
                    {
                        if (globalPopulation > previousGlobalPopulation)
                        {
                            world.LogEvent(new LocalizedString("TIMELINE_SPECIES_POPULATION_INCREASE",
                                    species.FormattedName, globalPopulation),
                                species.PlayerSpecies, "popUp.png");
                        }
                        else
                        {
                            world.LogEvent(new LocalizedString("TIMELINE_SPECIES_POPULATION_DECREASE",
                                    species.FormattedName, globalPopulation),
                                species.PlayerSpecies, "popDown.png");
                        }
                    }
                }

                foreach (var migration in GetMigrationsTo(patch))
                {
                    // Log to destination patch
                    // TODO: these events need to dynamically reveal their names in the event log once the player
                    // discovers them
                    patch.LogEvent(new LocalizedString("TIMELINE_SPECIES_MIGRATED_FROM", migration.Key.FormattedName,
                            migration.Value.From.VisibleName),
                        migration.Key.PlayerSpecies, "newSpecies.png");

                    // Log to game world
                    world.LogEvent(new LocalizedString("GLOBAL_TIMELINE_SPECIES_MIGRATED_TO",
                            migration.Key.FormattedName, migration.Value.To.VisibleName,
                            migration.Value.From.VisibleName),
                        migration.Key.PlayerSpecies, "newSpecies.png");

                    // Log to origin patch
                    migration.Value.From.LogEvent(new LocalizedString("TIMELINE_SPECIES_MIGRATED_TO",
                            migration.Key.FormattedName, migration.Value.To.VisibleName),
                        migration.Key.PlayerSpecies, "newSpecies.png");
                }

                foreach (var newSpeciesEntry in newSpecies)
                {
                    GetSpeciesPopulationsByPatch(newSpeciesEntry, true, true).TryGetValue(patch, out var population);

                    var speciesResult = results[newSpeciesEntry];

                    if (population > 0 && speciesResult.NewlyCreated != null)
                    {
                        if (speciesResult.SplitFrom == null)
                            throw new Exception("Split species doesn't have the species it split off stored");

                        switch (speciesResult.NewlyCreated.Value)
                        {
                            case NewSpeciesType.FillNiche:
                                LogEventGloballyAndLocally(world, patch, new LocalizedString("TIMELINE_NICHE_FILL",
                                        newSpeciesEntry.FormattedName, speciesResult.SplitFrom.FormattedName),
                                    false, "newSpecies.png");
                                break;
                            case NewSpeciesType.SplitDueToMutation:
                                LogEventGloballyAndLocally(world, patch, new LocalizedString(
                                        "TIMELINE_SELECTION_PRESSURE_SPLIT", newSpeciesEntry.FormattedName,
                                        speciesResult.SplitFrom.FormattedName),
                                    false, "newSpecies.png");
                                break;
                            default:
                                GD.PrintErr("Unhandled newly created species type: ", speciesResult.NewlyCreated.Value);
                                break;
                        }
                    }
                }
            }
        }

        /// <summary>
        ///   Call this only when auto-evo has finished. Calling at runtime will result in
        ///   incorrect result and random CollectionModifiedException.
        /// </summary>
        public IEnumerator GetEnumerator()
        {
            return results.GetEnumerator();
        }

        /// <summary>
        ///   Call this only when auto-evo has finished. Calling at runtime will result in
        ///   incorrect result and random CollectionModifiedException.
        /// </summary>
        IEnumerator<KeyValuePair<Species, SpeciesResult>> IEnumerable<KeyValuePair<Species, SpeciesResult>>.
            GetEnumerator()
        {
            return results.GetEnumerator();
        }

        /// <summary>
        ///   Returns the results for a given species for use by auto-evo internally
        /// </summary>
        /// <remarks>
        ///   <para>
        ///     Should be used very carefully and not at all by normal auto-evo steps. Used for more efficient auto-evo
        ///     run setups. Doesn't use any locking as this is not meant to be called before a run is started.
        ///   </para>
        /// </remarks>
        /// <param name="species">The species to get the results for</param>
        /// <returns>The species results for the species, modifications should be done very carefully</returns>
        internal SpeciesResult GetSpeciesResultForInternalUse(Species species)
        {
            if (results.TryGetValue(species, out var result))
                return result;

            result = new SpeciesResult(species);
            results[species] = result;

            return result;
        }

        /// <summary>
        ///   Logs an event description into game world and a patch. Use this if the event description in question
        ///   is exactly the same.
        /// </summary>
        private void LogEventGloballyAndLocally(GameWorld world, Patch patch, LocalizedString description,
            bool highlight = false, string? iconPath = null)
        {
            patch.LogEvent(description, highlight, iconPath);
            world.LogEvent(description, highlight, iconPath);
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

            if (!results.TryGetValue(species, out var speciesResult))
            {
                GD.PrintErr("RunResults: no species entry found for counting spread population");
                return -1;
            }

            foreach (var entry in speciesResult.SpreadToPatches)
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

        private bool HasSpeciesChanged(SpeciesResult result)
        {
            return result.MutatedProperties != null || result.SplitFrom != null || result.Species.PlayerSpecies;
        }

        public class SpeciesResult
        {
            public Species Species;

            /// <summary>
            ///   Dictionary of the new species population for relevant patches,
            ///   limited to natural increase/decrease and emergence.
            /// </summary>
            /// <remarks>
            ///   <para>
            ///     Does not consider migrations nor split-offs.
            ///   </para>
            /// </remarks>
            public Dictionary<Patch, long> NewPopulationInPatches = new();

            /// <summary>
            ///   null means no changes
            /// </summary>
            public Species? MutatedProperties;

            /// <summary>
            ///   List of patches this species has spread to
            /// </summary>
            public List<SpeciesMigration> SpreadToPatches = new();

            /// <summary>
            ///   If not null, this is a new species that was created
            /// </summary>
            public NewSpeciesType? NewlyCreated;

            /// <summary>
            ///   If set, the specified species split off from this species taking all the population listed in
            ///   <see cref="SplitOffPatches"/>
            /// </summary>
            public Species? SplitOff;

            /// <summary>
            ///   Patches that moved to the split off population
            /// </summary>
            public List<Patch>? SplitOffPatches;

            /// <summary>
            ///   Info on which species this split from. Not used for anything other than informational display
            /// </summary>
            public Species? SplitFrom;

            /// <summary>
            ///   If <see cref="SimulationConfiguration.CollectEnergyInformation"/> is set this collects energy
            ///   source and consumption info for this species per-patch where this was simulated
            /// </summary>
            public Dictionary<Patch, SpeciesPatchEnergyResults> EnergyResults = new();

            public SpeciesResult(Species species)
            {
                Species = species ?? throw new ArgumentException("species is null");
            }

            public SpeciesPatchEnergyResults GetEnergyResults(Patch patch)
            {
                if (patch == null)
                    throw new ArgumentException("can't get energy result for null patch", nameof(patch));

                if (EnergyResults.TryGetValue(patch, out var result))
                {
                    return result;
                }

                result = new SpeciesPatchEnergyResults();

                EnergyResults.Add(patch, result);
                return result;
            }
        }

        /// <summary>
        ///   Energy source and consumption information for a species in a patch
        /// </summary>
        public class SpeciesPatchEnergyResults
        {
            public readonly Dictionary<IFormattable, NicheInfo> PerNicheEnergy = new();

            public long UnadjustedPopulation;

            public float TotalEnergyGathered;

            public float IndividualCost;

            public class NicheInfo
            {
                public float CurrentSpeciesFitness;

                public float CurrentSpeciesEnergy;

                public float TotalFitness;

                public float TotalAvailableEnergy;
            }
        }
    }
}
