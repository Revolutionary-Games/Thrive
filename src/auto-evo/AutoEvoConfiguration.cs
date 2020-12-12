using Newtonsoft.Json;

public class AutoEvoConfiguration : IRegistryType
{
#pragma warning disable 169
    [JsonProperty]
    private int mutationsPerSpecies;

    [JsonProperty]
    private bool allowNoMutation;

    [JsonProperty]
    private int moveAttemptsPerSpecies;

    [JsonProperty]
    private bool allowNoMigration;
#pragma warning restore 169

    public int MutationsPerSpecies => mutationsPerSpecies;

    public bool AllowNoMutation => allowNoMutation;

    public int MoveAttemptsPerSpecies => moveAttemptsPerSpecies;

    public bool AllowNoMigration => allowNoMigration;

    /// <summary>
    /// Unused
    /// </summary>
    /// <value>The name of the internal.</value>
    public string InternalName { get; set; }

    public void Check(string name)
    {
        if (mutationsPerSpecies < 0)
        {
            throw new InvalidRegistryDataException("AutoEvoConfiguration", GetType().Name,
                "Mutations per species must be positive");
        }

        if (moveAttemptsPerSpecies < 0)
        {
            throw new InvalidRegistryDataException("AutoEvoConfiguration", GetType().Name,
                "Move attempts per species must be positive");
        }
    }

    public void ApplyTranslations()
    {
    }
}
