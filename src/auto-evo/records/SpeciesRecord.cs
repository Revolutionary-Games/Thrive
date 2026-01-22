namespace AutoEvo;

using Newtonsoft.Json;
using SharedBase.Archive;

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
    ///   ID of the species this species speciated from. If null, this species did not appear in this generation.
    /// </summary>
    [JsonProperty]
    public uint? SplitFromID { get; private set; }

    public virtual void WriteToArchive(ISArchiveWriter writer)
    {
        writer.Write(Population);

        if (MutatedPropertiesID != null)
        {
            writer.WriteAnyRegisteredValueAsObject(MutatedPropertiesID.Value);
        }
        else
        {
            writer.WriteNullObject();
        }

        if (SplitFromID != null)
        {
            writer.WriteAnyRegisteredValueAsObject(SplitFromID.Value);
        }
        else
        {
            writer.WriteNullObject();
        }
    }
}
