namespace AutoEvo;

using Newtonsoft.Json;

/// <summary>
///   Species mutation and population data from a single generation.
/// </summary>
public abstract class SpeciesRecord
{
    protected SpeciesRecord(long population, uint? mutatedPropertiesID, uint? splitFromID)
    {
        Population = population;
        MutatedPropertiesID = mutatedPropertiesID;
        SplitFromID = splitFromID;
    }

    /// <summary>
    ///   Species population for this generation.
    /// </summary>
    [JsonProperty]
    public long Population { get; private set; }

    /// <summary>
    ///   ID of the species this species mutated from. If null, this species did not mutate this generation.
    /// </summary>
    [JsonProperty]
    public uint? MutatedPropertiesID { get; private set; }

    /// <summary>
    ///   ID of the species this species speciated from. If null, this species did not appear this generation.
    /// </summary>
    [JsonProperty]
    public uint? SplitFromID { get; private set; }
}
