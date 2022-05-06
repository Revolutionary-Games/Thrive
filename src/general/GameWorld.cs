﻿using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using Newtonsoft.Json;

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
    public GameWorld(WorldGenerationSettings settings) : this()
    {
        PlayerSpecies = CreatePlayerSpecies();

        if (!PlayerSpecies.PlayerSpecies)
            throw new Exception("PlayerSpecies flag for being player species is not set");

        Map = PatchMapGenerator.Generate(settings, PlayerSpecies);

        if (!Map.Verify())
            throw new ArgumentException("generated patch map with settings is not valid");

        // Apply initial populations
        Map.UpdateGlobalPopulations();
    }

    /// <summary>
    ///   Blank world creation, only for loading saves
    /// </summary>
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

    public static void SetInitialSpeciesProperties(MicrobeSpecies species)
    {
        species.IsBacteria = true;
        species.SetInitialCompoundsForDefault();

        species.MembraneType = SimulationParameters.Instance.GetMembrane("single");

        species.Organelles.Add(new OrganelleTemplate(
            SimulationParameters.Instance.GetOrganelleType("cytoplasm"), new Hex(0, 0), 0));
    }

    /// <summary>
    ///   Creates an empty species object
    /// </summary>
    public MicrobeSpecies NewMicrobeSpecies(string genus, string epithet)
    {
        var species = new MicrobeSpecies(++speciesIdCounter, genus, epithet);

        worldSpecies[species.ID] = species;
        return species;
    }

    /// <summary>
    ///   Creates the initial (player) species
    /// </summary>
    public MicrobeSpecies CreatePlayerSpecies()
    {
        var species = NewMicrobeSpecies("Primum", "Thrivium");
        species.BecomePlayerSpecies();

        SetInitialSpeciesProperties(species);

        return species;
    }

    /// <summary>
    ///   Generates a few random species in all patches
    /// </summary>
    public void GenerateRandomSpeciesForFreeBuild()
    {
        var random = new Random();

        foreach (var entry in Map.Patches)
        {
            int speciesToAdd = random.Next(1, 4);

            for (int i = 0; i < speciesToAdd; ++i)
            {
                int population = Constants.INITIAL_SPECIES_POPULATION +
                    random.Next(Constants.INITIAL_FREEBUILD_POPULATION_VARIANCE_MIN,
                        Constants.INITIAL_FREEBUILD_POPULATION_VARIANCE_MAX + 1);

                entry.Value.AddSpecies(mutator.CreateRandomSpecies(NewMicrobeSpecies(string.Empty, string.Empty)),
                    population);
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
                // Mutator will mutate the name
                return mutator.CreateMutatedSpecies(s, NewMicrobeSpecies(species.Genus, species.Epithet));
            default:
                throw new ArgumentException("unhandled species type for CreateMutatedSpecies");
        }
    }

    /// <summary>
    ///   Registers a species created by auto-evo in this world. Updates the ID
    /// </summary>
    /// <param name="species">The species to register</param>
    public void RegisterAutoEvoCreatedSpecies(Species species)
    {
        if (worldSpecies.Any(p => p.Value == species))
            throw new ArgumentException("Species is already in this world");

        species.OnBecomePartOfWorld(++speciesIdCounter);
        worldSpecies[species.ID] = species;
        GD.Print("New species has become part of the world: ", species.FormattedIdentifier);
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
    ///   Makes the current auto-evo run run at full speed (all threads) until complete. If not active run does nothing
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
    ///   Adds an external population effect to a species
    /// </summary>
    /// <param name="species">Target species</param>
    /// <param name="constant">Change amount (constant part)</param>
    /// <param name="description">What caused the change</param>
    /// <param name="immediate">
    ///   If true applied immediately. Should only be used for the player dying
    /// </param>
    /// <param name="coefficient">Change amount (coefficient part)</param>
    public void AlterSpeciesPopulation(Species species, int constant, string description,
        bool immediate = false, float coefficient = 1)
    {
        if (constant == 0 || coefficient == 0)
            return;

        if (species == null)
            throw new ArgumentException("species is null");

        // Immediate is only allowed to use for the player dying
        if (immediate)
        {
            if (!species.PlayerSpecies)
                throw new ArgumentException("immediate effect is only for player dying");

            GD.Print("Applying immediate population effect " +
                "(should only be used for the player dying)");
            species.ApplyImmediatePopulationChange(constant, coefficient);
        }

        CreateRunIfMissing();

        autoEvo!.AddExternalPopulationEffect(species, constant, coefficient, description);
    }

    public void RemoveSpecies(Species species)
    {
        worldSpecies.Remove(species.ID);
    }

    public Species GetSpecies(uint id)
    {
        return worldSpecies[id];
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

        var stemCellType = new CellType(microbeSpecies);

        multicellularVersion.Cells.Add(new CellTemplate(stemCellType));
        multicellularVersion.CellTypes.Add(stemCellType);

        SwitchSpecies(species, multicellularVersion);
        return multicellularVersion;
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
        if (eventsLog[TotalPassedTime].Any(entry => entry.Description.Equals(description)))
        {
            return;
        }

        eventsLog[TotalPassedTime].Add(new GameEventDescription(description, iconPath, highlight));
    }

    public void FinishLoading(ISaveContext? context)
    {
        if (Map == null || PlayerSpecies == null)
            throw new InvalidOperationException("Map or player species was not loaded correctly for a saved world");
    }

    private void CreateRunIfMissing()
    {
        if (autoEvo != null)
            return;

        autoEvo = AutoEvo.AutoEvo.CreateRun(this);
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
}
