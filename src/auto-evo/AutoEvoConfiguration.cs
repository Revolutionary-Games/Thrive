public class AutoEvoConfiguration : IRegistryType
{
    /// <summary>
    /// Unused
    /// </summary>
    /// <value>The name of the internal.</value>
    public string InternalName { get; set; }

    public int MUTATIONS_PER_SPECIES;
    public bool ALLOW_NO_MUTATION;
    public int MOVE_ATTEMPTS_PER_SPECIES;
    public bool ALLOW_NO_MIGRATION;

    public void Check(string name)
    {
        if (MUTATIONS_PER_SPECIES < 0)
        {
            throw new InvalidRegistryDataException("NameGenerator", GetType().Name,
                "Mutations per species must be positive");
        }

        if (MOVE_ATTEMPTS_PER_SPECIES < 0)
        {
            throw new InvalidRegistryDataException("NameGenerator", GetType().Name,
                "Move attempts per species must be positive");
        }
    }

    public void ApplyTranslations()
    {
    }
}
