using System;
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

    public GameWorld(WorldGenerationSettings settings)
    {
        mutator = new Mutations();
        PlayerSpecies = CreatePlayerSpecies();

        Map = PatchMapGenerator.Generate(settings, PlayerSpecies);

        if (!Map.Verify())
            throw new ArgumentException("generated patch map with settings is not valid");

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
                        data.Density *= 0.8f;
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
        throw new NotImplementedException();

        // auto@ patches = map.getPatches();
        //
        // for (uint i = 0; i < patches.length(); ++i)
        // {
        //
        //     const int species = GetEngine().GetRandom().GetNumber(1, 4);
        //     for (int count = 0; count < species; ++count)
        //     {
        //
        //         patches[i].addSpecies(createRandomSpecies(),
        //             GetEngine().GetRandom().GetNumber(INITIAL_SPLIT_POPULATION_MIN,
        //                 INITIAL_SPLIT_POPULATION_MAX));
        //     }
        // }
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
    ///   Adds an external population effect to a species
    /// </summary>
    /// <param name="immediate">
    ///   If true applied immediately. Should only be used for the player dying
    /// </param>
    public void AlterSpeciesPopulation(Species species, int amount, string description,
        bool immediate = false)
    {
        // TODO: fix
        throw new NotImplementedException();
    }
}
