using System.ComponentModel;
using System.Text.Json.Serialization;
using Saving.Serializers;
using SharedBase.Archive;
using ThriveScriptsShared;

/// <summary>
///   Define enzymes in the game. Enzyme is an "upgrade" that grants specific ability to microbes.
/// </summary>
[TypeConverter($"Saving.Serializers.{nameof(EnzymeStringConverter)}")]
public class Enzyme : RegistryType
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

    [JsonIgnore]
    public override ArchiveObjectType ArchiveObjectType => (ArchiveObjectType)ThriveArchiveObjectType.Enzyme;

    public static object ReadFromArchive(ISArchiveReader reader, ushort version, int referenceId)
    {
        return SimulationParameters.Instance.GetEnzyme(ReadInternalName(reader, version));
    }

    public override void Check(string name)
    {
        TranslationHelper.CopyTranslateTemplatesToTranslateSource(this);
    }

    public override void ApplyTranslations()
    {
        TranslationHelper.ApplyTranslations(this);
    }
}
