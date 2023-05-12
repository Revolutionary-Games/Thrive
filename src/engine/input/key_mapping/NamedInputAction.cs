using System;
using Newtonsoft.Json;

/// <summary>
///   A godot input action name with an associated user readable name
/// </summary>
public class NamedInputAction : IRegistryType
{
#pragma warning disable 169,649 // Used through reflection
    private string? untranslatedName;
#pragma warning restore 169,649

    public string InputName { get; set; } = null!;

    /// <summary>
    ///   The user readable name
    /// </summary>
    [TranslateFrom(nameof(untranslatedName))]
    public string Name { get; set; } = null!;

    [JsonIgnore]
    public string InternalName { get => InputName; set => throw new NotSupportedException(); }

    public void Check(string name)
    {
        if (string.IsNullOrEmpty(Name))
        {
            throw new InvalidRegistryDataException(name, GetType().Name,
                "NamedInputAction has no name");
        }

        if (string.IsNullOrEmpty(InputName))
        {
            throw new InvalidRegistryDataException(name, GetType().Name,
                "NamedInputAction has no input name");
        }

        TranslationHelper.CopyTranslateTemplatesToTranslateSource(this);
    }

    public void ApplyTranslations()
    {
        TranslationHelper.ApplyTranslations(this);
    }
}
