using System;
using System.Collections.Generic;
using System.Linq;
using AutoEvo;
using Godot;
using Newtonsoft.Json;
using Xoshiro.PRNG64;

/// <summary>
///   All data regarding the game world of a thrive playthrough
/// </summary>
/// <remarks>
///   <para>
///     In Leviathan this used to be the main class doing things,
///     but now this is just a collection of data regarding the world.
///   </para>
/// </remarks>
[JsonObject(IsReference = true)]
[UseThriveSerializer]
public class GameWorld : ISaveLoadable
{
    [JsonProperty]
    public UnlockProgress UnlockProgress = new();

    [JsonProperty]
    public WorldStatsTracker StatisticsTracker = new();

    [JsonProperty]
    public WorldGenerationSettings WorldSettings = new();

    /// <summary>
    ///   History of the world. Generations need to be inserted in order for this to work.
    /// </summary>
    [JsonProperty]
    public Dictionary<int, GenerationRecord> GenerationHistory = new();

    [JsonProperty]
    private uint speciesIdCounter;

    [JsonProperty]
    private Mutations mutator = new();

    [JsonProperty]
    private Dictionary<uint, Species> worldSpecies = new();

    [JsonProperty]
    private Dictionary<double, List<GameEventDescription>> eventsLog = new();

    /// <summary>
    ///   This world's auto-evo run
    /// </summary>
    /// <remarks>
    ///   <para>
    ///     This isn't saved as this can be restarted after loading a save. But the contained external effects in the
    ///     run are saved.
    ///   </para>
    /// </remarks>
    private AutoEvoRun? autoEvo;

    /// <summary>
    ///   Creates a new world
    /// </summary>
    /// <param name="settings">Settings to generate the world with</param>
    /// <param name="startingSpecies">Starting species for the player</param>
    public GameWorld(WorldGenerationSettings settings, Species? startingSpecies = null) : this()
    {
        WorldSettings = settings;
        LightCycle.ApplyWorldSettings(settings);

        if (startingSpecies == null)
        {
            PlayerSpecies = CreatePlayerSpecies();
        }
        else
        {
            // Species generation are forced to be 1 (the default value) in case it is different
            // when in a fossilisation file.
            startingSpecies.Generation = 1;

            startingSpecies.BecomePlayerSpecies();
            startingSpecies.OnEdited();

            // Need to update the species ID in case it was different in a previous game
            startingSpecies.OnBecomePartOfWorld(GetNextSpeciesID());
            worldSpecies[startingSpecies.ID] = startingSpecies;

            PlayerSpecies = startingSpecies;
        }

        if (!PlayerSpecies.PlayerSpecies)
            throw new Exception("PlayerSpecies flag for being player species is not set");

        Map = PatchMapGenerator.Generate(settings, PlayerSpecies);

        if (!Map.Verify())
            throw new ArgumentException("generated patch map with settings is not valid");

        // Apply initial populations
        Map.UpdateGlobalPopulations();

        // Create the initial generation by adding only the player species
        var initialSpeciesRecord = new SpeciesRecordLite((Species)PlayerSpecies.Clone(), PlayerSpecies.Population);
        GenerationHistory.Add(0, new GenerationRecord(0,
            new Dictionary<uint, SpeciesRecordLite> { { PlayerSpecies.ID, initialSpeciesRecord } }));

        UnlockProgress.UnlockAll = !settings.Difficulty.OrganelleUnlocksEnabled;
    }

    /// <summary>
    ///   Blank world creation, only for loading saves
    /// </summary>
    [JsonConstructor]
    public GameWorld()
    {
        // TODO: when loading a save this shouldn't be recreated as otherwise that happens all the time
        // Note that as the properties are applied from a save after the constructor, the save is correctly loaded
        // but these extra objects get created and garbage collected
        TimedEffects = new TimedWorldOperations();

        // Register glucose reduction
        TimedEffects.RegisterEffect("reduce_glucose", new GlucoseReductionEffect(this));
    }

    [JsonProperty]
    public Species PlayerSpecies { get; private set; } = null!;

    [JsonIgnore]
    public IReadOnlyDictionary<uint, Species> Species => worldSpecies;

    [JsonProperty]
    public PatchMap Map { get; private set; } = null!;

    /// <summary>
    ///   This probably needs to be changed to a huge precision number
    ///   depending on what timespans we'll end up using.
    /// </summary>
    [JsonProperty]
    public double TotalPassedTime { get; private set; }

    [JsonProperty]
    public TimedWorldOperations TimedEffects { get; private set; }

    [JsonProperty]
    public DayNightCycle LightCycle { get; private set; } = new();

    /// <summary>
    ///   The current external effects for the current auto-evo run. This is here to allow saving to work for them.
    ///   Don't add new effects through this, instead go through the run instead
    /// </summary>
    public List<ExternalEffect>? CurrentExternalEffects
    {
        get
        {
            if (autoEvo == null)
                return new List<ExternalEffect>();

            return autoEvo.ExternalEffects;
        }
        set
        {
            // Make sure there is an existing run, as that isn't saved, so when loading we need to create the run to
            // store things in it. Creating the run here doesn't interfere with it being started

            // We skip starting a run if the list of external effects would be empty anyway, as is the case
            // when loading a save made in the editor
            if (value == null || value.Count < 1)
            {
                autoEvo?.ExternalEffects.Clear();
                return;
            }

            CreateRunIfMissing();

            var effects = autoEvo!.ExternalEffects;

            effects.Clear();

            effects.AddRange(value);
        }
    }

    /// <summary>
    ///   Returns log of game events pertaining to the whole world.
    /// </summary>
    public IReadOnlyDictionary<double, List<GameEventDescription>> EventsLog => eventsLog;

    public static void SetInitialSpeciesProperties(MicrobeSpecies species, List<Hex> workMemory1, List<Hex> workMemory2)
    {
        species.IsBacteria = true;

        species.MembraneType = SimulationParameters.Instance.GetMembrane("single");

        species.Organelles.AddFast(new OrganelleTemplate(SimulationParameters.Instance.GetOrganelleType("cytoplasm"),
            new Hex(0, 0), 0), workMemory1, workMemory2);

        species.OnEdited();
    }

    /// <summary>
    ///   Takes care of processing everything in the world.
    /// </summary>
    public void Process(float delta)
    {
        LightCycle.Process(delta);
    }

    /// <summary>
    ///   Adds data for the current generation to a list of generation records.
    /// </summary>
    public void AddCurrentGenerationToHistory()
    {
        if (autoEvo?.Results == null)
        {
            GD.PrintErr("Auto-evo run not finished for adding to generation history");
            return;
        }

        var generation = PlayerSpecies.Generation - 1;
        GenerationHistory.Add(generation, new GenerationRecord(TotalPassedTime,
            autoEvo.Results.GetSpeciesRecords()));
    }

    /// <summary>
    ///   Creates an empty species object
    /// </summary>
    public MicrobeSpecies NewMicrobeSpecies(string genus, string epithet)
    {
        var species = new MicrobeSpecies(GetNextSpeciesID(), genus, epithet);

        worldSpecies[species.ID] = species;
        return species;
    }

    /// <summary>
    ///   Creates the initial (player) species
    /// </summary>
    public MicrobeSpecies CreatePlayerSpecies()
    {
        var species = NewMicrobeSpecies("Primum", "thrivium");
        species.BecomePlayerSpecies();

        var workMemory1 = new List<Hex>();
        var workMemory2 = new List<Hex>();

        SetInitialSpeciesProperties(species, workMemory1, workMemory2);

        return species;
    }

    /// <summary>
    ///   Removes player status from <see cref="PlayerSpecies"/> and makes the given species the new player. Marks the
    ///   old species as obsolete (and deletes all populations).
    /// </summary>
    /// <param name="species">New player species</param>
    /// <param name="deleteOldPlayer">If false doesn't make the old player species unable to be used</param>
    /// <exception cref="ArgumentException">If the given species is currently the player species</exception>
    public void ReplacePlayerSpeciesWith(Species species, bool deleteOldPlayer = true)
    {
        if (ReferenceEquals(PlayerSpecies, species) || PlayerSpecies.ID == species.ID)
        {
            throw new ArgumentException("New and old player species are the same");
        }

        if (species.Obsolete)
            throw new ArgumentException("Cannot switch to an obsolete species");

        bool found = false;

        foreach (var existingSpecies in worldSpecies)
        {
            if (ReferenceEquals(species, existingSpecies.Value))
            {
                found = true;
                break;
            }
        }

        if (!found)
            GD.PrintErr("Player swapping to species object not present in the current world");

        var oldPlayer = PlayerSpecies;

        if (oldPlayer.Obsolete)
        {
            GD.PrintErr("Player species is already obsolete on switch");
        }

        GD.Print($"Switching player species from {oldPlayer.FormattedIdentifier} to {species.FormattedIdentifier}");

        if (deleteOldPlayer)
        {
            oldPlayer.Obsolete = true;

            // Remove all populations of the old species to make sure it is extinct
            oldPlayer.Population = 0;
            Map.RemoveAllPopulationsOf(oldPlayer);
        }

        species.BecomePlayerSpecies();
        PlayerSpecies = species;

        // Update the species generation to continue the game timeline from where it left off
        species.Generation = oldPlayer.Generation;
    }

    /// <summary>
    ///   Generates a few random species in all patches
    /// </summary>
    public void GenerateRandomSpeciesForFreeBuild(long seed = 0)
    {
        Random random;

        if (seed == 0)
        {
            random = new XoShiRo256starstar();
        }
        else
        {
            random = new XoShiRo256starstar(seed);
        }

        var workMemory = new MutationWorkMemory();

        foreach (var entry in Map.Patches)
        {
            int speciesToAdd = random.Next(1, 4);

            for (int i = 0; i < speciesToAdd; ++i)
            {
                int population = Constants.INITIAL_SPECIES_POPULATION +
                    random.Next(Constants.INITIAL_FREEBUILD_POPULATION_VARIANCE_MIN,
                        Constants.INITIAL_FREEBUILD_POPULATION_VARIANCE_MAX + 1);

                var randomSpecies = mutator.CreateRandomSpecies(NewMicrobeSpecies(string.Empty, string.Empty),
                    WorldSettings.AIMutationMultiplier, WorldSettings.LAWK, workMemory);

                GenerationHistory[0].AllSpeciesData
                    .Add(randomSpecies.ID, new SpeciesRecordLite(randomSpecies, population));

                entry.Value.AddSpecies(randomSpecies, population);
            }
        }
    }

    /// <summary>
    ///   Simulate long term world time passing
    /// </summary>
    public void OnTimePassed(double timePassed)
    {
        TotalPassedTime += timePassed * Constants.EDITOR_TIME_JUMP_MILLION_YEARS * 1000000;

        TimedEffects.OnTimePassed(timePassed, TotalPassedTime);
    }

    /// <summary>
    ///   Creates a mutated copy of a species
    /// </summary>
    public Species CreateMutatedSpecies(Species species)
    {
        switch (species)
        {
            case MicrobeSpecies s:
            {
                var workMemory = new MutationWorkMemory();

                // Mutator will mutate the name
                return mutator.CreateMutatedSpecies(s, NewMicrobeSpecies(species.Genus, species.Epithet),
                    WorldSettings.AIMutationMultiplier, WorldSettings.LAWK, workMemory);
            }

            default:
                throw new ArgumentException("unhandled species type for CreateMutatedSpecies");
        }
    }

    /// <inheritdoc cref="RegisterAutoEvoCreatedSpecies"/>
    public void RegisterAutoEvoCreatedSpeciesIfNotAlready(Species species)
    {
        if (worldSpecies.Any(p => p.Value == species))
            return;

        RegisterAutoEvoCreatedSpecies(species);
    }

    /// <summary>
    ///   Registers a species created by auto-evo in this world. Updates the ID to ensure it is unique and valid for
    ///   this world.
    /// </summary>
    /// <param name="species">The species to register</param>
    public void RegisterAutoEvoCreatedSpecies(Species species)
    {
        if (worldSpecies.Any(p => p.Value == species))
            throw new ArgumentException("Species is already in this world");

        species.OnBecomePartOfWorld(GetNextSpeciesID());
        worldSpecies[species.ID] = species;
    }

    /// <summary>
    ///   Checks if an auto-evo run for this world is finished, optionally starting one if not in-progress already
    /// </summary>
    public bool IsAutoEvoFinished(bool autostart = true)
    {
        if (autoEvo == null && autostart)
            CreateRunIfMissing();

        if (autoEvo != null && !autoEvo.Running && autostart)
            autoEvo.Start();

        if (autoEvo == null)
            return false;

        return autoEvo.Finished;
    }

    /// <summary>
    ///   Makes the current auto-evo run to run at full speed (all threads) until complete. If not active run does
    ///   nothing
    /// </summary>
    public void FinishAutoEvoRunAtFullSpeed()
    {
        if (autoEvo?.FullSpeed == false)
            autoEvo.FullSpeed = true;
    }

    public AutoEvoRun GetAutoEvoRun()
    {
        IsAutoEvoFinished();

        return autoEvo ?? throw new Exception("Auto evo run starting did not work");
    }

    /// <summary>
    ///   Stops and removes any auto-evo runs for this world
    /// </summary>
    public void ResetAutoEvoRun()
    {
        if (autoEvo != null)
        {
            autoEvo.Abort();
            autoEvo = null;
        }
    }

    /// <summary>
    ///   Adds an external population effect to a species in a specific patch
    /// </summary>
    /// <param name="species">Target species</param>
    /// <param name="constant">Change amount (constant part)</param>
    /// <param name="description">What caused the change</param>
    /// <param name="patch">The patch this effect affects.</param>
    /// <param name="immediate">
    ///   If true applied immediately. Should only be used for the player dying
    /// </param>
    /// <param name="coefficient">Change amount (coefficient part)</param>
    public void AlterSpeciesPopulation(Species species, int constant, string description, Patch patch,
        bool immediate = false, float coefficient = 1)
    {
        // It sort of makes sense to allow 0 coefficient to force population to 0, that's why this check is here
        // now if this effect would do nothing, then it is skipped
        if (constant == 0 && Math.Abs(coefficient - 1) < MathUtils.EPSILON)
            return;

        if (species == null)
            throw new ArgumentException("species is null");

        if (coefficient < 0)
            throw new ArgumentException("coefficient may not be negative");

        if (string.IsNullOrEmpty(description))
            throw new ArgumentException("May not be empty or null", nameof(description));

        // Immediate is only allowed to use for the player dying
        if (immediate)
        {
            if (!species.PlayerSpecies)
                throw new ArgumentException("immediate effect is only for player dying");

            GD.Print($"Applying immediate population effect to {species.FormattedIdentifier}, constant: " +
                $"{constant}, coefficient: {coefficient}, reason: {description}");

            species.ApplyImmediatePopulationChange(constant, coefficient, patch);
        }

        CreateRunIfMissing();

        autoEvo!.AddExternalPopulationEffect(species, constant, coefficient, description, patch);
    }

    /// <summary>
    ///   Adds an external population effect to a species in the current patch
    /// </summary>
    /// <param name="species">Target species</param>
    /// <param name="constant">Change amount (constant part)</param>
    /// <param name="description">What caused the change</param>
    /// <param name="immediate">
    ///   If true applied immediately. Should only be used for the player dying
    /// </param>
    /// <param name="coefficient">Change amount (coefficient part)</param>
    public void AlterSpeciesPopulationInCurrentPatch(Species species, int constant, string description,
        bool immediate = false, float coefficient = 1)
    {
        if (Map.CurrentPatch == null)
            throw new InvalidOperationException("No current patch set in map");

        AlterSpeciesPopulation(species, constant, description, Map.CurrentPatch, immediate, coefficient);
    }

    public void RemoveSpecies(Species species)
    {
        worldSpecies.Remove(species.ID);
    }

    public Species GetSpecies(uint id)
    {
        return worldSpecies[id];
    }

    public bool TryGetSpecies(uint id, out Species? species)
    {
        return worldSpecies.TryGetValue(id, out species);
    }

    /// <summary>
    ///   Finds the species that is the closest relative to the given species
    /// </summary>
    /// <param name="relatedTo">This is the species to which the closet relative is searched</param>
    /// <param name="onlyNonExtinct">If true this method will only return non-extinct species</param>
    /// <param name="filter">Optional extra filter to discard results if this returns false</param>
    /// <returns>The closest related species or null</returns>
    public Species? GetClosestRelatedSpecies(Species relatedTo, bool onlyNonExtinct, Func<Species, bool>? filter)
    {
        // How closely related species are to the target
        var speciesDistance = new Dictionary<Species, int>();

        var relatedId = relatedTo.ID;

        int nextGeneration = 0;

        foreach (var generation in GenerationHistory)
        {
            if (nextGeneration != generation.Key)
            {
                GD.PrintErr(
                    $"World history is out of order, expected generation {nextGeneration} but got {generation.Key}");
            }

            ++nextGeneration;

            foreach (var recordSpecies in generation.Value.AllSpeciesData)
            {
                // Skip already handled species
                bool handled = false;

                foreach (var alreadyHandled in speciesDistance)
                {
                    if (alreadyHandled.Key.ID == recordSpecies.Key)
                    {
                        handled = true;
                        break;
                    }
                }

                if (handled)
                    continue;

                var currentRecordValue = recordSpecies.Value;

                // Directly split species
                if (currentRecordValue.MutatedPropertiesID == relatedId || currentRecordValue.SplitFromID == relatedId)
                {
                    // This cannot use historical species records as those record the historical populations and not
                    // current ones. And the Species objects wouldn't be in this world.
                    if (TryGetSpecies(recordSpecies.Key, out var candidateSpecies))
                    {
                        speciesDistance[candidateSpecies!] = 1;
                    }
                }

                // Indirectly split species
                foreach (var existingDistance in speciesDistance)
                {
                    var existingId = existingDistance.Key.ID;

                    if (existingId != currentRecordValue.MutatedPropertiesID &&
                        existingId != currentRecordValue.SplitFromID)
                    {
                        continue;
                    }

                    if (TryGetSpecies(recordSpecies.Key, out var candidateSpecies))
                    {
                        // Distance is one more than the existing calculated distance that this current species is
                        // related to
                        speciesDistance[candidateSpecies!] = existingDistance.Value + 1;
                    }

                    break;
                }
            }
        }

        // Find the best usable species with the minimum distance to the related species
        Species? result = null;
        int minDistance = int.MaxValue;
        int minGeneration = int.MaxValue;

        foreach (var entry in speciesDistance)
        {
            if (entry.Value > minDistance)
                continue;

            if ((onlyNonExtinct && entry.Key.IsExtinct) || entry.Key.Obsolete)
                continue;

            if (filter != null && !filter.Invoke(entry.Key))
                continue;

            // The entry also needs to be as little mutated compared to the player as possible (this is estimated based
            // on the generation of the species)
            if (minGeneration < entry.Key.Generation)
                continue;

            // If same as min distance population needs to be higher
            if (entry.Value == minDistance && result != null && entry.Key.Population <= result.Population)
                continue;

            minDistance = entry.Value;
            minGeneration = entry.Key.Generation;
            result = entry.Key;
        }

        return result;
    }

    /// <summary>
    ///   Moves a species to the multicellular stage
    /// </summary>
    /// <param name="species">
    ///   The species to convert to an early multicellular one. No checks are done to make sure the species is
    ///   actually a valid multicellular one.
    /// </param>
    public EarlyMulticellularSpecies ChangeSpeciesToMulticellular(Species species)
    {
        var microbeSpecies = (MicrobeSpecies)species;

        if (microbeSpecies.IsBacteria)
            throw new ArgumentException("bacteria can't turn multicellular");

        var multicellularVersion = new EarlyMulticellularSpecies(species.ID, species.Genus, species.Epithet);
        species.CopyDataToConvertedSpecies(multicellularVersion);

        var workMemory1 = new List<Hex>();
        var workMemory2 = new List<Hex>();

        var stemCellType = new CellType(microbeSpecies, workMemory1, workMemory2);

        multicellularVersion.Cells.AddFast(new CellTemplate(stemCellType), workMemory1, workMemory2);
        multicellularVersion.CellTypes.Add(stemCellType);

        multicellularVersion.OnEdited();
        SwitchSpecies(species, multicellularVersion);
        return multicellularVersion;
    }

    /// <summary>
    ///   Moves a species to the late multicellular stage
    /// </summary>
    /// <param name="species">
    ///   The species to convert to a late multicellular one. No checks are done to make sure the species is
    ///   actually a valid multicellular one.
    /// </param>
    public LateMulticellularSpecies ChangeSpeciesToLateMulticellular(Species species)
    {
        var earlySpecies = species as EarlyMulticellularSpecies;

        if (earlySpecies == null)
            throw new ArgumentException("Only early multicellular species can become late multicellular species");

        var lateVersion = new LateMulticellularSpecies(species.ID, species.Genus, species.Epithet);
        species.CopyDataToConvertedSpecies(lateVersion);

        // Copy all the cell types, even ones that are unused so the player doesn't lose any when moving stages
        // in case they want to place them later
        lateVersion.CellTypes.AddRange(earlySpecies.CellTypes);

        // Create initial metaball layout from the cell layout
        // TODO: improve this algorithm

        // Create metaballs for everything first
        var metaballs = new List<MulticellularMetaball>();

        foreach (var cellTemplate in earlySpecies.Cells)
        {
            var metaball = new MulticellularMetaball(cellTemplate.CellType)
            {
                Position = Hex.AxialToCartesian(cellTemplate.Position),
                Size = 1,
            };

            metaballs.Add(metaball);
        }

        // Then create the parent structure
        // Root is the closest metaball to the origin
        var rootMetaball = metaballs.OrderBy(m => m.Position.LengthSquared()).First();

        foreach (var metaball in metaballs)
        {
            if (ReferenceEquals(metaball, rootMetaball))
                continue;

            if (metaball.Parent != null)
                throw new Exception("Logic error in metaball initial parent calculation");

            // For now just pick the closest (and in case of ties, the closer to origin) metaball as the parent
            // Also avoid accidentally making short parent loops
            var potentialParents = metaballs
                .Where(m => !ReferenceEquals(m, metaball) && !ReferenceEquals(m.Parent, metaball))
                .OrderBy(m => m.Position.DistanceSquaredTo(metaball.Position)).ThenBy(m => m.Position.LengthSquared());

            bool foundSuitableParent = false;

            foreach (var parentCandidate in potentialParents)
            {
                // Prevent causing parent loops
                if (parentCandidate.HasAncestor(metaball))
                    continue;

                metaball.Parent = parentCandidate;
                foundSuitableParent = true;
                break;
            }

            if (!foundSuitableParent)
                throw new Exception("Could not find a suitable parent for metaball");
        }

        // Fix root to be at 0,0
        rootMetaball.Position = Vector3.Zero;

        // And finally move the metaballs to touch each other
        // Do this from the root down to not need to process metaballs multiple times
        // TODO: should this logic be in OnEdited for general use?

        var metaballsToPosition = new List<MulticellularMetaball> { Capacity = metaballs.Count };

        // First build a good order to update the metaballs in
        foreach (var metaball in metaballs.OrderBy(m => m.Position.LengthSquared()))
        {
            RecursivelyAddBallsToList(metaballsToPosition, metaball);
        }

        // Next, calculate the direction vectors to parents
        var metaballParentVectors = new List<Vector3> { Capacity = metaballsToPosition.Count };

        foreach (var metaball in metaballsToPosition)
        {
            // Add dummy value for the root to not need to worry about that in the next loop
            metaballParentVectors.Add(metaball.DirectionToParent() ?? Vector3.Zero);
        }

        // And finally apply the positioning
        for (int i = 0; i < metaballsToPosition.Count; ++i)
        {
            var metaball = metaballsToPosition[i];

            // Don't position the root metaball here
            if (metaball.Parent == null)
                continue;

            metaball.AdjustPositionToTouchParent(metaballParentVectors[i]);
        }

        // Finish off by adding the metaballs to the layout in an order where all parents are added before the other
        // ones
        foreach (var metaball in metaballsToPosition)
            lateVersion.BodyLayout.Add(metaball);

        lateVersion.BodyLayout.VerifyMetaballsAreTouching();

        lateVersion.OnEdited();
        SwitchSpecies(species, lateVersion);
        return lateVersion;
    }

    /// <summary>
    ///   Should be called after a batch of species stage changes are done, for example after calling
    ///   <see cref="ChangeSpeciesToMulticellular"/>
    /// </summary>
    public void NotifySpeciesChangedStages()
    {
        if (autoEvo != null)
        {
            GD.Print("Canceling and restarting auto-evo to have stage changed species versions in it");
            ResetAutoEvoRun();
            IsAutoEvoFinished();
        }
    }

    /// <summary>
    ///   Stores a description of a global event into the game world records.
    /// </summary>
    /// <param name="description">The event's description</param>
    /// <param name="highlight">If true, the event will be highlighted in the timeline UI</param>
    /// <param name="iconPath">Resource path to the icon of the event</param>
    public void LogEvent(LocalizedString description, bool highlight = false, string? iconPath = null)
    {
        if (eventsLog.Count > Constants.GLOBAL_EVENT_LOG_CAP)
        {
            var oldestKey = eventsLog.Keys.Min();
            eventsLog.Remove(oldestKey);
        }

        if (!eventsLog.ContainsKey(TotalPassedTime))
            eventsLog.Add(TotalPassedTime, new List<GameEventDescription>());

        // Event already logged in timeline
        if (eventsLog[TotalPassedTime].Any(e => e.Description.Equals(description)))
        {
            return;
        }

        eventsLog[TotalPassedTime].Add(new GameEventDescription(description, iconPath, highlight));
    }

    /// <summary>
    ///   Updates the light level in all patches according to <see cref="LightCycle"/> data.
    /// </summary>
    public void UpdateGlobalLightLevels()
    {
        foreach (var patch in Map.Patches.Values)
        {
            patch.UpdateCurrentSunlight(LightCycle.DayLightFraction);
        }
    }

    public void FinishLoading(ISaveContext? context)
    {
        if (Map == null || PlayerSpecies == null)
            throw new InvalidOperationException("Map or player species was not loaded correctly for a saved world");

        LightCycle.CalculateDependentLightData(WorldSettings);
    }

    public void BuildEvolutionaryTree(EvolutionaryTree tree)
    {
        // Building the tree relies on the existence of a full history of generations stored in the current game. Since
        // we only started adding these in 0.6.0, it's impossible to build a tree in older saves.
        // TODO: avoid an ugly try/catch block by actually checking the original save version?
        if (GenerationHistory.Count < 1)
        {
            throw new InvalidOperationException("Generation history is empty");
        }

        tree.Clear();
        int nextGeneration = 0;

        foreach (var generation in GenerationHistory)
        {
            if (nextGeneration != generation.Key)
            {
                GD.PrintErr(
                    $"World history is out of order, expected generation {nextGeneration} but got {generation.Key}");
            }

            ++nextGeneration;

            var record = generation.Value;

            if (generation.Key == 0)
            {
                var initialSpecies = GenerationHistory[0].AllSpeciesData.Values.Select(s => s.Species).WhereNotNull();
                tree.Init(initialSpecies, PlayerSpecies.ID, PlayerSpecies.FormattedName);
                continue;
            }

            // Recover all omitted species data for this generation, so we can fill the tree
            var updatedSpeciesData = record.AllSpeciesData.ToDictionary(s => s.Key,
                s => GenerationRecord.GetFullSpeciesRecord(s.Key, generation.Key, GenerationHistory));

            tree.Update(updatedSpeciesData, generation.Key, record.TimeElapsed, PlayerSpecies.ID);
        }
    }

    private void CreateRunIfMissing()
    {
        if (autoEvo != null)
            return;

        autoEvo = AutoEvo.AutoEvo.CreateRun(this);
    }

    private uint GetNextSpeciesID()
    {
        var speciesId = ++speciesIdCounter;

        // Guard against running out of IDs
        if (speciesId == uint.MaxValue)
        {
            // TODO: implement species ID sequence reset (need to find unused ranges of species ID numbers for this
            // world
            throw new NotImplementedException(
                "World ran out of species ID numbers and restarting sequence is not implemented");
        }

        return speciesId;
    }

    private void SwitchSpecies(Species old, Species newSpecies)
    {
        GD.Print("Moving species ", old.FormattedIdentifier, " from ", old.GetType().Name, " to ",
            newSpecies.GetType().Name);

        RemoveSpecies(old);
        worldSpecies.Add(old.ID, newSpecies);

        Map.ReplaceSpecies(old, newSpecies);

        if (PlayerSpecies == old)
            PlayerSpecies = newSpecies;
    }

    private void RecursivelyAddBallsToList(ICollection<MulticellularMetaball> list, MulticellularMetaball metaball)
    {
        if (list.Contains(metaball))
            return;

        if (metaball.Parent != null)
        {
            // Need to recursively add parents first to the list, this is absolutely required for the step where
            // these are added to the layout ultimately
            RecursivelyAddBallsToList(list, (MulticellularMetaball)metaball.Parent);
        }

        list.Add(metaball);
    }
}
