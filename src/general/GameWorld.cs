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

        species.IsBacteria = true;
        species.InitialCompounds.Add("atp", 30);
        species.InitialCompounds.Add("glucose", 10);
        species.Genus = "Primum";
        species.Epithet = "Thrivium";

        species.MembraneType = SimulationParameters.Instance.GetMembrane("single");

        species.Organelles.Add(new OrganelleTemplate(
            SimulationParameters.Instance.GetOrganelleType("cytoplasm"), new Hex(0, 0), 0));

        return species;
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
}
