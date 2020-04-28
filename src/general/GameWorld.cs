using System;
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
public class GameWorld
{
    [JsonProperty]
    private uint speciesIdCounter = 0;

    [JsonProperty]
    private Mutations mutator;

    /// <summary>
    ///   This world's auto-evo run
    /// </summary>
    /// <remarks>
    ///   <para>
    ///     Once saving is implemented this probably shouldn't be attempted to be saved. But the list of external
    ///     population effects need to be saved.
    ///   </para>
    /// </remarks>
    private AutoEvoRun autoEvo;

    public GameWorld(WorldGenerationSettings settings)
    {
        mutator = new Mutations();
        PlayerSpecies = CreatePlayerSpecies();

        Map = PatchMapGenerator.Generate(settings, PlayerSpecies);

        if (!Map.Verify())
            throw new ArgumentException("generated patch map with settings is not valid");

        // Apply initial populations
        Map.UpdateGlobalPopulations();

        TimedEffects = new TimedWorldOperations();

        // Register glucose reduction
        TimedEffects.RegisterEffect("reduce_glucose", new WorldEffectLambda((elapsed, total) =>
        {
            foreach (var key in Map.Patches.Keys)
            {
                var patch = Map.Patches[key];

                foreach (var compound in patch.Biome.Compounds.Keys)
                {
                    if (compound == "glucose")
                    {
                        var data = patch.Biome.Compounds[compound];

                        // TODO: verify that this change is picked up by the patch manager
                        data.Density *= Constants.GLUCOSE_REDUCTION_RATE;
                    }
                }
            }
        }));
    }

    public Species PlayerSpecies { get; private set; }

    public PatchMap Map { get; private set; }

    public TimedWorldOperations TimedEffects { get; private set; }

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
    ///   Creates an empty species object
    /// </summary>
    public MicrobeSpecies NewMicrobeSpecies()
    {
        return new MicrobeSpecies(++speciesIdCounter);
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
        TimedEffects.OnTimePassed(timePassed);
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
        {
            CreateRunIfMissing();
            autoEvo.Start();
        }

        if (autoEvo == null)
            return false;

        return autoEvo.Finished;
    }

    public AutoEvoRun GetAutoEvoRun()
    {
        IsAutoEvoFinished(true);

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
    ///   Adds an external population effect to a species
    /// </summary>
    /// <param name="immediate">
    ///   If true applied immediately. Should only be used for the player dying
    /// </param>
    public void AlterSpeciesPopulation(Species species, int amount, string description,
        bool immediate = false)
    {
        if (amount == 0)
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
            species.ApplyImmediatePopulationChange(amount);
        }

        CreateRunIfMissing();

        autoEvo.AddExternalPopulationEffect(species, amount, description);
    }

    private void CreateRunIfMissing()
    {
        if (autoEvo != null)
            return;

        autoEvo = AutoEvo.AutoEvo.CreateRun(this);
    }
}
