using Newtonsoft.Json;

public class AutoEvoConfiguration : IRegistryType
{
    [JsonProperty]
    public int MutationsPerSpecies { get; private set; }

    [JsonProperty]
    public bool AllowNoMutation { get; private set; }

    [JsonProperty]
    public int MoveAttemptsPerSpecies { get; private set; }

    [JsonProperty]
    public bool AllowNoMigration { get; private set; }

    /// <summary>
    /// Unused
    /// </summary>
    /// <value>The name of the internal.</value>
    public string InternalName { get; set; }

    public void Check(string name)
    {
        if (MutationsPerSpecies < 0)
        {
            throw new InvalidRegistryDataException("AutoEvoConfiguration", GetType().Name,
                "Mutations per species must be positive");
        }

        if (MoveAttemptsPerSpecies < 0)
        {
            throw new InvalidRegistryDataException("AutoEvoConfiguration", GetType().Name,
                "Move attempts per species must be positive");
        }
    }

    public void ApplyTranslations()
    {
    }
}
