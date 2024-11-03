﻿namespace AutoEvo;

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using Godot;
using Newtonsoft.Json;

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
    [JsonProperty]
    private readonly ConcurrentDictionary<Species, SpeciesResult> results = new();

    // The following variables are not shared as they are just temporary data during running, and not required
    // afterwards when loading from a save
    private readonly List<PossibleSpecies> modifiedSpecies = new();

    /// <summary>
    ///   Miche tree generated for this auto-evo run. This is partially saved (some subobjects are excluded) so that
    ///   food chain tab information can be displayed after loading a save.
    /// </summary>
    [JsonProperty]
    [JsonConverter(typeof(DictionaryWithJSONKeysConverter<Patch, Miche>))]
    private readonly Dictionary<Patch, Miche> micheByPatch = new();

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

    public void AddNewMicheForPatch(Patch patch, Miche miche)
    {
        micheByPatch[patch] = miche;
    }

    public void AddMutationResultForSpecies(Species species, Species? mutated,
        KeyValuePair<Patch, long> populationBoostInPatches)
    {
        MakeSureResultExistsForSpecies(species);

        results[species].MutatedProperties = mutated;

        results[species].NewPopulationInPatches.TryGetValue(populationBoostInPatches.Key, out var existing);
        results[species].NewPopulationInPatches[populationBoostInPatches.Key] =
            existing + populationBoostInPatches.Value;

        // This code is kept in case the population increase is changed to allow multiple items
        /*foreach (var population in populationBoostInPatches)
        {
            results[species].NewPopulationInPatches.TryGetValue(population.Key, out var existing);
            results[species].NewPopulationInPatches[population.Key] = existing + population.Value;
        }*/
    }

    public void AddPopulationResultForSpecies(Species species, Patch patch, long newPopulation)
    {
        MakeSureResultExistsForSpecies(species);

        results[species].NewPopulationInPatches[patch] = Math.Max(newPopulation, 0);
    }

    public void AddMigrationResultForSpecies(Species species, SpeciesMigration migration)
    {
        if (migration.Population < 0)
            throw new ArgumentException("Population to migrate cannot be negative");

        MakeSureResultExistsForSpecies(species);

        results[species].SpreadToPatches.Add(migration);
    }

    /// <summary>
    ///   Removes migrations specified for species for to/from patches where it split
    /// </summary>
    /// <param name="species">The species to check</param>
    public void RemoveMigrationsForSplitPatches(Species species)
    {
        SpeciesResult? result;

        lock (results)
        {
            if (!results.TryGetValue(species, out result))
                return;
        }

        if (result.SplitOffPatches == null)
            return;

        lock (result.SpreadToPatches)
        {
            result.SpreadToPatches.RemoveAll(s =>
                result.SplitOffPatches.Contains(s.From) || result.SplitOffPatches.Contains(s.To));
        }
    }

    /// <summary>
    ///   Makes sure that the species doesn't attempt to migrate to one patch from multiple source patches as this is
    ///   currently not supported by <see cref="GetMigrationsTo"/>
    /// </summary>
    public void RemoveDuplicateTargetPatchMigrations(Species species)
    {
        SpeciesResult? result;

        lock (results)
        {
            if (!results.TryGetValue(species, out result))
                return;
        }

        lock (result.SpreadToPatches)
        {
            var spreads = result.SpreadToPatches;
            int migrationCount = spreads.Count;

            for (int i = 1; i < migrationCount; ++i)
            {
                for (int j = 0; j < i; ++j)
                {
                    // Remove later migrations that target the same patch as an earlier one
                    if (spreads[i].To != spreads[j].To)
                        continue;

                    // Debugging code which can be enabled to track how much pruning happens
                    /*GD.Print($"Removed Patch migration to {spreads[i].To} from {spreads[j].From} for " +
                        $"species {species} as this is a duplicate migration target");*/

                    spreads.RemoveAt(i);

                    --i;
                    --migrationCount;
                    break;
                }
            }
        }
    }

    public List<PossibleSpecies> GetPossibleSpeciesList()
    {
        return modifiedSpecies;
    }

    public void AddPossibleMutation(Species species, KeyValuePair<Patch, long> initialPopulationInPatches,
        NewSpeciesType addType, Species parentSpecies)
    {
        modifiedSpecies.Add(new PossibleSpecies(species, initialPopulationInPatches, addType, parentSpecies));
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

    public void AddNewSpecies(Species species, KeyValuePair<Patch, long> initialPopulationInPatch,
        NewSpeciesType addType, Species parentSpecies)
    {
        MakeSureResultExistsForSpecies(species);

        results[species].NewlyCreated = addType;
        results[species].SplitFrom = parentSpecies;

        results[species].NewPopulationInPatches[initialPopulationInPatch.Key] =
            Math.Max(initialPopulationInPatch.Value, 0);
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

    public void AddTrackedEnergyForSpecies(Species species, Patch patch, SelectionPressure pressure,
        float speciesFitness, float totalFitness, float speciesEnergy)
    {
        MakeSureResultExistsForSpecies(species);

        var dataReceiver = results[species].GetEnergyResults(patch);

        var nicheDescription = pressure.GetDescription();
        dataReceiver.PerNicheEnergy[nicheDescription] = new SpeciesPatchEnergyResults.NicheInfo
        {
            CurrentSpeciesFitness = speciesFitness,
            CurrentSpeciesEnergy = speciesEnergy,
            TotalFitness = totalFitness,
            TotalAvailableEnergy = pressure.GetEnergy(patch),
        };
    }

    public void AddTrackedEnergyConsumptionForSpecies(Species species, Patch patch, float totalEnergy,
        float individualCost)
    {
        MakeSureResultExistsForSpecies(species);

        var dataReceiver = results[species].GetEnergyResults(patch);

        dataReceiver.TotalEnergyGathered = totalEnergy;
        dataReceiver.IndividualCost = individualCost;
    }

    /// <summary>
    ///   Checks if species has results. Species doesn't have results if it was extinct or was not part of the run.
    ///   All species *should* have results for them that were part of the world at the start of the auto-evo run.
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

                        if (!patch.AddSpecies(entry.Key, populationEntry.Value))
                        {
                            GD.PrintErr("RunResults has new species that already exists in patch");
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

    public Miche GetMicheForPatch(Patch patch)
    {
        if (!micheByPatch.TryGetValue(patch, out var miche))
            throw new ArgumentException("Miche not found for " + patch.Name + " in MicheByPatch");

        return miche;
    }

    /// <summary>
    ///   Returns the miche by patch dictionary
    /// </summary>
    /// <remarks>
    ///   <para>
    ///     This should not be used in AutoEvo code, prefer <see cref="GetMicheForPatch"/>.
    ///   </para>
    /// </remarks>
    public Dictionary<Patch, Miche> InspectPatchMicheData()
    {
        return micheByPatch;
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
                {
                    // TODO: should this support the species migrating from multiple patches to the target patch
                    if (!migrationsToPatch.TryAdd(resultEntry.Key, migration))
                    {
                        GD.PrintErr("Species tried to migrate to a target patch from multiple patches at once, " +
                            "this is currently unsupported, losing this migration");
                    }
                }
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
    ///   Stores previous populations for species in this results object
    /// </summary>
    /// <param name="gameWorldMap">Map to read old populations from</param>
    public void StorePreviousPopulations(PatchMap gameWorldMap)
    {
        if (gameWorldMap.CurrentPatch == null)
            GD.PrintErr("Store previous populations for auto-evo given a map with no current patch set");

        foreach (var result in results)
        {
            foreach (var patch in gameWorldMap.Patches.Values)
            {
                var population = patch.GetSpeciesSimulationPopulation(result.Key);
                if (population > 0)
                    result.Value.OldPopulationInPatches[patch] = population;
            }
        }
    }

    /// <summary>
    ///   Calculates the final new population for a species in the given patch while taking migrations and splits into
    ///   account.
    /// </summary>
    /// <returns>New population or 0</returns>
    public long GetNewSpeciesPopulationInPatch(SpeciesResult entry, Patch forPatch, bool resolveMigrations = true,
        bool resolveSplits = true)
    {
        entry.NewPopulationInPatches.TryGetValue(forPatch, out var newPopulation);

        if (resolveMigrations)
        {
            newPopulation += CountSpeciesSpreadPopulation(entry.Species, forPatch);

            // It is valid to add migrations that try to move more population than there is in total in the
            // patch. The actual results apply clamp the values to real populations, but that haven't been done
            // here so we need to clamp things to not be negative here as it would look pretty wierd.
            if (newPopulation < 0)
                newPopulation = 0;
        }

        if (resolveSplits)
        {
            if (entry.SplitOffPatches?.Contains(forPatch) == true)
            {
                // All population splits off
                newPopulation = 0;
            }

            if (entry.SplitFrom != null)
            {
                var splitFrom = results[entry.SplitFrom];

                // Get population from where this split-off
                if (splitFrom.SplitOff == entry.Species)
                {
                    if (splitFrom.SplitOffPatches == null)
                        throw new Exception("Split off patches is null for a split species");

                    foreach (var patchPopulation in splitFrom.SplitOffPatches)
                    {
                        if (patchPopulation == forPatch)
                        {
                            newPopulation += splitFrom.NewPopulationInPatches[patchPopulation];
                            break;
                        }
                    }
                }
            }
        }

        return newPopulation;
    }

    /// <summary>
    ///   Makes summary text. To show previous populations <see cref="StorePreviousPopulations"/> must be called
    /// </summary>
    /// <param name="playerReadable">If true ids are removed from the output</param>
    /// <remarks>
    ///   <para>
    ///     This method no longer takes external effects as parameters as it is assumed that
    ///     <see cref="AutoEvoRun.CalculateAndApplyFinalExternalEffectSizes"/> has been called.
    ///   </para>
    /// </remarks>
    /// <returns>The generated summary text</returns>
    public LocalizedStringBuilder MakeSummary(bool playerReadable = false)
    {
        // Splits have to be calculated here as this uses the results population numbers which aren't adjusted when
        // applying the results. So if this didn't want to have to resolve those, we'd need to adjust the result data
        // when applying the results to have the most up-to-date data available directly here.
        const bool resolveMigrations = true;
        const bool resolveSplits = true;

        var builder = new LocalizedStringBuilder(500);

        // TODO: the new old populations code uses patch references directly rather than using IDs to compare them
        // so this is now only partially anymore setup to deal with patch object inequality with the results. Probably
        // should remove the ID comparisons entirely as the old species population needs custom dictionary looping to
        // lookup things by ID.

        LocalizedStringBuilder PatchString(Patch patch)
        {
            // TODO: avoid this temporary string builder creation here
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

            builder.Append(' ');
            builder.Append(new LocalizedString("PREVIOUS_COLON"));
            builder.Append(' ');
            results[species].OldPopulationInPatches.TryGetValue(patch, out var oldPopulation);
            builder.Append(oldPopulation);

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
                long adjustedPopulation =
                    GetNewSpeciesPopulationInPatch(entry, patchPopulation.Key, resolveMigrations, resolveSplits);

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
                    if (entry.OldPopulationInPatches.TryGetValue(patchPopulation.Key, out var old) && old > 0)
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
                        OutputPopulationForPatch(entry.Species, to, CountSpeciesSpreadPopulation(entry.Species, to));
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

    /// <summary>
    ///   Makes a graphical variant of the summary report for a single patch (and also optionally global results)
    /// </summary>
    /// <param name="guiTarget">
    ///   Where to put the graphical controls (TODO: could reuse instances where possible)
    /// </param>
    /// <param name="forPatch">The patch the results are for</param>
    /// <param name="showGlobalResults">If true then global results are added after the patch results</param>
    /// <param name="speciesResultScene">
    ///   Scene to display the results with, has to be <see cref="SpeciesResultButton"/>
    /// </param>
    /// <param name="titleFonts">Font settings for the titles between sections, if null no titles are added</param>
    /// <param name="selectionCallback">
    ///   Callback that takes a single <c>uint SpeciesID</c> parameter for when a species button is clicked
    /// </param>
    public void MakeGraphicalSummary(Container guiTarget, Patch forPatch, bool showGlobalResults,
        PackedScene speciesResultScene, LabelSettings? titleFonts, Callable? selectionCallback)
    {
        // As this reads data from the results and not an up-to-date map, this doesn't have all population effects
        // resolved by default, so we need to do those here
        const bool resolveMigrations = true;
        const bool resolveSplits = true;

        bool IsRelevantForResults(Patch patch, SpeciesResult result)
        {
            if (result.NewPopulationInPatches.TryGetValue(patch, out var population) && population > 0)
                return true;

            if (result.OldPopulationInPatches.TryGetValue(patch, out population) && population > 0)
                return true;

            if (resolveMigrations)
            {
                foreach (var spreadEntry in result.SpreadToPatches)
                {
                    if (spreadEntry.To == forPatch && spreadEntry.Population != 0)
                        return true;
                }
            }

            return false;
        }

        // Patch specific results
        if (titleFonts != null)
        {
            var patchHeading = new HBoxContainer();
            patchHeading.AddChild(new HSeparator
            {
                SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
            });

            // Applying the translation here means  that this method needs to be re-called when translations change,
            // which should be set up currently
            patchHeading.AddChild(new Label
            {
                Text = Localization.Translate("AUTO_EVO_RESULTS_PATCH_TITLE"),
                LabelSettings = titleFonts,
            });

            patchHeading.AddChild(new HSeparator
            {
                SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
            });

            guiTarget.AddChild(patchHeading);
        }

        // Results after the heading are contained in a HFlow to automatically split into lines
        var container = new HFlowContainer();

        foreach (var entry in
                 results.Values.OrderByDescending(s => s.Species.PlayerSpecies)
                     .ThenBy(s => s.Species.FormattedName))
        {
            if (!IsRelevantForResults(forPatch, entry))
                continue;

            var resultDisplay = speciesResultScene.Instantiate<SpeciesResultButton>();

            resultDisplay.DisplaySpecies(entry, false);

            entry.OldPopulationInPatches.TryGetValue(forPatch, out var oldPopulation);

            var newPopulation = GetNewSpeciesPopulationInPatch(entry, forPatch, resolveMigrations, resolveSplits);

            resultDisplay.DisplayPopulation(newPopulation, oldPopulation, true);

            // TODO: add this if desired
            // resultDisplay.DisplayGlobalPopulation()
            resultDisplay.HideGlobalPopulation();

            if (selectionCallback != null)
                resultDisplay.Connect(SpeciesResultButton.SignalName.SpeciesSelected, selectionCallback.Value);

            container.AddChild(resultDisplay);
        }

        guiTarget.AddChild(container);

        // Global results
        if (!showGlobalResults)
            return;

        // Heading again
        if (titleFonts != null)
        {
            var patchHeading = new HBoxContainer();
            patchHeading.AddChild(new HSeparator
            {
                SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
            });

            patchHeading.AddChild(new Label
            {
                Text = Localization.Translate("AUTO_EVO_RESULTS_GLOBAL_TITLE"),
                LabelSettings = titleFonts,
            });

            patchHeading.AddChild(new HSeparator
            {
                SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
            });

            guiTarget.AddChild(patchHeading);
        }

        // And then the results
        container = new HFlowContainer();

        foreach (var entry in
                 results.Values.OrderByDescending(s => s.Species.PlayerSpecies)
                     .ThenBy(s => s.Species.FormattedName))
        {
            // If the global populations are shown by the above loop, then this should skip the reverse of
            // IsRelevantForResults

            var resultDisplay = speciesResultScene.Instantiate<SpeciesResultButton>();

            resultDisplay.DisplaySpecies(entry, true);

            // Calculate global old and new populations to show
            long totalOldPopulation = 0;
            long totalNewPopulation = 0;

            foreach (var populationEntry in entry.OldPopulationInPatches)
            {
                if (populationEntry.Value > 0)
                    totalOldPopulation += populationEntry.Value;
            }

            foreach (var populationEntry in entry.NewPopulationInPatches)
            {
                if (populationEntry.Value > 0)
                    totalNewPopulation += populationEntry.Value;
            }

            // For global populations migrations just move stuff around, so it doesn't matter here

            // But splits do matter
            if (resolveSplits)
            {
                if (entry.SplitOffPatches != null)
                {
                    foreach (var splitOffPatch in entry.SplitOffPatches)
                    {
                        if (entry.NewPopulationInPatches.TryGetValue(splitOffPatch, out var adjustment))
                            totalNewPopulation -= adjustment;
                    }
                }

                if (entry.SplitFrom != null)
                {
                    var splitFrom = results[entry.SplitFrom];

                    // Get population from where this split-off
                    if (splitFrom.SplitOff == entry.Species)
                    {
                        if (splitFrom.SplitOffPatches == null)
                            throw new Exception("Split off patches is null for a split species");

                        foreach (var patchPopulation in splitFrom.SplitOffPatches)
                        {
                            totalNewPopulation += splitFrom.NewPopulationInPatches[patchPopulation];
                        }
                    }
                }
            }

            resultDisplay.DisplayPopulation(totalNewPopulation, totalOldPopulation, false);

            // Definitely don't want to show the extra global population line here
            resultDisplay.HideGlobalPopulation();

            if (selectionCallback != null)
                resultDisplay.Connect(SpeciesResultButton.SignalName.SpeciesSelected, selectionCallback.Value);

            container.AddChild(resultDisplay);
        }

        guiTarget.AddChild(container);
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
                        new LocalizedString("TIMELINE_SPECIES_EXTINCT", species.FormattedNameBbCodeUnstyled),
                        species.PlayerSpecies, false, "extinction.png");

                    continue;
                }

                if (finalPatchPopulation > 0 && finalPatchPopulation != previousPatchPopulation)
                {
                    if (finalPatchPopulation > previousPatchPopulation)
                    {
                        patch.LogEvent(new LocalizedString("TIMELINE_SPECIES_POPULATION_INCREASE",
                                species.FormattedNameBbCodeUnstyled, finalPatchPopulation),
                            species.PlayerSpecies, false, "popUp.png");
                    }
                    else
                    {
                        patch.LogEvent(new LocalizedString("TIMELINE_SPECIES_POPULATION_DECREASE",
                                species.FormattedNameBbCodeUnstyled, finalPatchPopulation),
                            species.PlayerSpecies, false, "popDown.png");
                    }
                }
                else
                {
                    patch.LogEvent(new LocalizedString("TIMELINE_SPECIES_EXTINCT_LOCAL",
                        species.FormattedNameBbCodeUnstyled), species.PlayerSpecies, false, "extinctionLocal.png");
                }

                if (globalPopulation != previousGlobalPopulation)
                {
                    if (globalPopulation > previousGlobalPopulation)
                    {
                        world.LogEvent(new LocalizedString("TIMELINE_SPECIES_POPULATION_INCREASE",
                                species.FormattedNameBbCodeUnstyled, globalPopulation),
                            species.PlayerSpecies, false, "popUp.png");
                    }
                    else
                    {
                        world.LogEvent(new LocalizedString("TIMELINE_SPECIES_POPULATION_DECREASE",
                                species.FormattedNameBbCodeUnstyled, globalPopulation),
                            species.PlayerSpecies, false, "popDown.png");
                    }
                }
            }

            foreach (var migration in GetMigrationsTo(patch))
            {
                // Log to destination patch
                // TODO: these events need to dynamically reveal their names in the event log once the player
                // discovers them
                patch.LogEvent(new LocalizedString("TIMELINE_SPECIES_MIGRATED_FROM",
                        migration.Key.FormattedNameBbCodeUnstyled, migration.Value.From.VisibleName),
                    migration.Key.PlayerSpecies, false, "newSpecies.png");

                // Log to game world
                world.LogEvent(new LocalizedString("GLOBAL_TIMELINE_SPECIES_MIGRATED_TO",
                        migration.Key.FormattedNameBbCodeUnstyled, migration.Value.To.VisibleName,
                        migration.Value.From.VisibleName),
                    migration.Key.PlayerSpecies, false, "newSpecies.png");

                // Log to origin patch
                migration.Value.From.LogEvent(new LocalizedString("TIMELINE_SPECIES_MIGRATED_TO",
                        migration.Key.FormattedNameBbCodeUnstyled, migration.Value.To.VisibleName),
                    migration.Key.PlayerSpecies, false, "newSpecies.png");
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
                                newSpeciesEntry.FormattedNameBbCodeUnstyled,
                                speciesResult.SplitFrom.FormattedNameBbCodeUnstyled), false, false, "newSpecies.png");
                            break;
                        case NewSpeciesType.SplitDueToMutation:
                            LogEventGloballyAndLocally(world, patch, new LocalizedString(
                                    "TIMELINE_SELECTION_PRESSURE_SPLIT", newSpeciesEntry.FormattedNameBbCodeUnstyled,
                                    speciesResult.SplitFrom.FormattedNameBbCodeUnstyled),
                                false, false, "newSpecies.png");
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
        bool highlight = false, bool showInReport = false, string? iconPath = null)
    {
        patch.LogEvent(description, highlight, showInReport, iconPath);
        world.LogEvent(description, highlight, showInReport, iconPath);
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

    private long CountSpeciesSpreadPopulation(Species species, Patch targetPatch)
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

    /// <summary>
    ///   A species that may come into existence due to auto-evo simulation, but it isn't guaranteed yet. These are
    ///   resolved by <see cref="RegisterNewSpecies"/>
    /// </summary>
    public record struct PossibleSpecies(Species Species, KeyValuePair<Patch, long>
        InitialPopulationInPatches, NewSpeciesType AddType, Species ParentSpecies);

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
        ///   Previous population numbers that are stored for more advanced result views
        /// </summary>
        public Dictionary<Patch, long> OldPopulationInPatches = new();

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

        // TODO: NEW AUTO-EVO NEEDS TO BE FIXED TO USE THE FOLLOWING TO VARIABLES (check, might already work):
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
        [JsonConverter(typeof(DictionaryWithJSONKeysConverter<LocalizedString, NicheInfo>))]
        public readonly Dictionary<LocalizedString, NicheInfo> PerNicheEnergy = new();

        public float TotalEnergyGathered;

        public float IndividualCost;

        /// <summary>
        ///   Unadjusted population based the energy sources. Doesn't take
        ///   <see cref="Constants.AUTO_EVO_MINIMUM_VIABLE_POPULATION"/> into account.
        /// </summary>
        [JsonIgnore]
        public long UnadjustedPopulation => (long)MathF.Floor(TotalEnergyGathered / IndividualCost);

        public class NicheInfo
        {
            public float CurrentSpeciesFitness;

            public float CurrentSpeciesEnergy;

            public float TotalFitness;

            public float TotalAvailableEnergy;
        }
    }
}
