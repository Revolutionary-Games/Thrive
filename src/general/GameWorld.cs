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

    public GameWorld(WorldGenerationSettings settings)
    {
        Map = PatchMapGenerator.Generate(settings);
    }

    public PatchMap Map { get; private set; }

    public MicrobeSpecies NewMicrobeSpecies()
    {
        return new MicrobeSpecies(++speciesIdCounter);
    }
}
