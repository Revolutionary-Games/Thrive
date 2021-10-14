using System;
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
public class GameWorld
{
    [JsonProperty]
    private uint speciesIdCounter;

    [JsonProperty]
    private Mutations mutator = new Mutations();

    [JsonProperty]
    private Dictionary<uint, Species> worldSpecies = new Dictionary<uint, Species>();

    /// <summary>
    ///   This world's auto-evo run
    /// </summary>
    /// <remarks>
    ///   <para>
    ///     TODO: Once saving is implemented this probably shouldn't be attempted to be saved. But the list of external
    ///     population effects need to be saved.
    ///   </para>
    /// </remarks>
    private AutoEvoRun autoEvo;

    /// <summary>
    ///   Creates a new world
    /// </summary>
    /// <param name="settings">Settings to generate the world with</param>
    public GameWorld(WorldGenerationSettings settings) : this()
    {
        PlayerSpecies = CreatePlayerSpecies();

        Map = PatchMapGenerator.Generate(settings, PlayerSpecies);

        if (!Map.Verify())
            throw new ArgumentException("generated patch map with settings is not valid");
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
    public Species PlayerSpecies { get; private set; }

    [JsonProperty]
    public PatchMap Map { get; private set; }

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
    public List<ExternalEffect> CurrentExternalEffects
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
            CreateRunIfMissing();

            var effects = autoEvo.ExternalEffects;

            effects.Clear();

            if (value == null)
                return;

            effects.AddRange(value);
        }
    }

    public static void SetInitialSpeciesProperties(MicrobeSpecies species)
    {
        species.IsBacteria = true;
        species.SetInitialCompoundsForDefault();
        species.Genus = "Primum";
        species.Epithet = "Thrivium";

        species.MembraneType = SimulationParameters.Instance.GetMembrane("single");

        species.Organelles.Add(new OrganelleTemplate(
            SimulationParameters.Instance.GetOrganelleType("cytoplasm"), new Hex(0, 0), 0));
    }

    /// <summary>
    ///   Gets the global population of a species.
    /// </summary>
    /// <returns>Returns the sum of the populations in all patches</returns>
    public long GetGlobalSpeciesPopulation(Species species)
    {
        return Map.Patches.Sum(p => GetSpeciesPopulationInPatch(species, p.Value));
    }

    /// <summary>
    ///   Gets the local population of a species.
    /// </summary>
    /// <returns>Returns the population of the species in a patch.</returns>
    public long GetSpeciesPopulationInPatch(Species species, Patch patch)
    {
        return patch.SpeciesInPatch.ContainsKey(species) ? patch.SpeciesInPatch[species] : 0;
    }

    /// <summary>
    ///   Immediately sets the population of a species in a patch
    /// </summary>
    public void SetSpeciesPopulationInPatch(Species species, Patch patch, long value)
    {
        if (value <= 0)
        {
            patch.SpeciesInPatch.Remove(species);
            return;
        }

        if (patch.SpeciesInPatch.ContainsKey(species))
            patch.SpeciesInPatch[species] = value;
        else
            patch.SpeciesInPatch.Add(species, value);
    }

    /// <summary>
    ///   Immediate population change (from the player dying)
    /// </summary>
    public void ApplyImmediatePopulationChange(Species species, Patch patch, long constant, float coefficient)
    {
        var newPopulation = (long)(GetSpeciesPopulationInPatch(species, patch) * coefficient);
        newPopulation += constant;

        SetSpeciesPopulationInPatch(species, patch, newPopulation);
    }

    public bool IsPlayerExtinct()
    {
        return IsSpeciesExtinct(PlayerSpecies);
    }

    public bool IsSpeciesExtinct(Species species)
    {
        return GetGlobalSpeciesPopulation(species) <= 0;
    }

    public bool IsSpeciesExtinctInPatch(Species species, Patch patch)
    {
        return GetSpeciesPopulationInPatch(species, patch) <= 0;
    }

    /// <summary>
    ///   Creates an empty species object
    /// </summary>
    public MicrobeSpecies NewMicrobeSpecies()
    {
        var species = new MicrobeSpecies(++speciesIdCounter);

        worldSpecies[species.ID] = species;
        return species;
    }

    /// <summary>
    ///   Creates the initial (player) species
    /// </summary>
    public MicrobeSpecies CreatePlayerSpecies()
    {
        var species = NewMicrobeSpecies();
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

                entry.Value.AddSpecies(mutator.CreateRandomSpecies(NewMicrobeSpecies()), population);
            }
        }
    }

    /// <summary>
    ///   Simulate long term world time passing
    /// </summary>
    public void OnTimePassed(double timePassed)
    {
        TotalPassedTime += timePassed * 100000000;

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
                return mutator.CreateMutatedSpecies(s, NewMicrobeSpecies());
            default:
                throw new ArgumentException("unhandled species type for CreateMutatedSpecies");
        }
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

    public AutoEvoRun GetAutoEvoRun()
    {
        IsAutoEvoFinished();

        return autoEvo;
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
    /// <param name="immediate">
    ///   If true applied immediately. Should only be used for the player dying
    /// </param>
    /// <param name="coefficient">Change amount (coefficient part)</param>
    /// <param name="patch">The patch where this happened. Null for the current patch.</param>
    public void AlterSpeciesPopulation(Species species, int constant, string description,
        bool immediate = false, float coefficient = 1, Patch patch = null)
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
            ApplyImmediatePopulationChange(species, patch ?? Map.CurrentPatch, constant, coefficient);
        }

        CreateRunIfMissing();

        autoEvo.AddExternalPopulationEffect(species, constant, coefficient, description, patch ?? Map.CurrentPatch, immediate);
    }

    /// <summary>
    ///   Adds an external population effect to a species in all patches where this species is present
    /// </summary>
    public void AlterSpeciesPopulationEverywhere(Species species, int constant, string description,
        bool immediate = false, float coefficient = 1)
    {
        foreach (var patch in Map.Patches.Values)
        {
            if (patch.GetSpeciesPopulation(species) <= 0)
                continue;

            AlterSpeciesPopulation(species, constant, description, immediate, coefficient, patch);
        }
    }

    public void RemoveSpecies(Species species)
    {
        worldSpecies.Remove(species.ID);
    }

    public Species GetSpecies(uint id)
    {
        return worldSpecies[id];
    }

    private void CreateRunIfMissing()
    {
        if (autoEvo != null)
            return;

        autoEvo = AutoEvo.AutoEvo.CreateRun(this);
    }
}
