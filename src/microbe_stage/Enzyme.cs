/// <summary>
///   Define enzymes in the game. Enzyme is an "upgrade" that grants specific ability to microbes.
/// </summary>
public class Enzyme : IRegistryType
{
    /// <summary>
    ///   User visible pretty name
    /// </summary>
    [TranslateFrom("untranslatedName")]
    public string Name = null!;

    /// <summary>
    ///   The effect of this enzyme.
    /// </summary>
    public string Property = null!;

#pragma warning disable 169 // Used through reflection
    private string? untranslatedName;
#pragma warning restore 169

    public string InternalName { get; set; } = null!;

    public void Check(string name)
    {
        if (string.IsNullOrEmpty(Property))
            throw new InvalidRegistryDataException(name, GetType().Name, "Enzyme property is not specified");

        TranslationHelper.CopyTranslateTemplatesToTranslateSource(this);
    }

    public void ApplyTranslations()
    {
        TranslationHelper.ApplyTranslations(this);
    }
}
