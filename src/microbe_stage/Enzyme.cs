using System.ComponentModel;
using Saving.Serializers;

/// <summary>
///   Define enzymes in the game. Enzyme is an "upgrade" that grants specific ability to microbes.
/// </summary>
[TypeConverter($"Saving.Serializers.{nameof(EnzymeStringConverter)}")]
public class Enzyme : IRegistryType
{
    /// <summary>
    ///   User visible pretty name
    /// </summary>
    [TranslateFrom(nameof(untranslatedName))]
    public string Name = null!;

    [TranslateFrom(nameof(untranslatedDescription))]
    public string Description = null!;

    /// <summary>
    ///   What this enzyme does.
    /// </summary>
    public EnzymeProperty Property = EnzymeProperty.Hydrolytic;

#pragma warning disable 169,649 // Used through reflection
    private string? untranslatedName;
    private string? untranslatedDescription;
#pragma warning restore 169,649

    public enum EnzymeProperty
    {
        Hydrolytic,
        Oxidizer,
    }

    public string InternalName { get; set; } = null!;

    public void Check(string name)
    {
        TranslationHelper.CopyTranslateTemplatesToTranslateSource(this);
    }

    public void ApplyTranslations()
    {
        TranslationHelper.ApplyTranslations(this);
    }
}
