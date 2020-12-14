using System;
using Newtonsoft.Json;

/// <summary>
///   A godot input action name with an associated user readable name
/// </summary>
public class NamedInputAction : IRegistryType
{
#pragma warning disable 169 // Used through reflection
    private string untranslatedName;
#pragma warning restore 169

    public string InputName { get; set; }

    /// <summary>
    ///   The user readable name
    /// </summary>
    [TranslateFrom("untranslatedName")]
    public string Name { get; set; }

    [JsonIgnore]
    public string InternalName { get => InputName; set => throw new NotSupportedException(); }

    public void Check(string name)
    {
        TranslationHelper.CopyTranslateTemplatesToTranslateSource(this);
    }

    public void ApplyTranslations()
    {
        TranslationHelper.ApplyTranslations(this);
    }
}
