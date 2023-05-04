using System;
using Newtonsoft.Json;

/// <summary>
///   Definition for a screen effect
/// </summary>
/// <remarks>
///   <para>
///     Values for each screen effect
///   </para>
/// </remarks>
public class ScreenEffect : IRegistryType
{
    /// <summary>
    ///   User readable name
    /// </summary>
    [TranslateFrom(nameof(untranslatedName))]
    public string Name = null!;

#pragma warning disable 169,649 // Used through reflection
    private string? untranslatedName;
#pragma warning restore 169,649

    /// <summary>
    ///   Index for this screen effect in the effect menu
    /// </summary>
    [JsonProperty]
    public int Index { get; private set; }

    [JsonProperty]
    public string? ShaderPath { get; private set; }

    public string InternalName { get; set; } = null!;

    [JsonIgnore]
    public string UntranslatedName =>
        untranslatedName ?? throw new InvalidOperationException("Translations not initialized");

    public void Check(string name)
    {
        if (string.IsNullOrEmpty(Name))
            throw new InvalidRegistryDataException(name, GetType().Name, "Name is not set");

        TranslationHelper.CopyTranslateTemplatesToTranslateSource(this);

        if (Index < 0)
            throw new InvalidRegistryDataException(name, GetType().Name, "Index is negative");
    }

    public void ApplyTranslations()
    {
        TranslationHelper.ApplyTranslations(this);
    }

    public override string ToString()
    {
        return Name;
    }
}
