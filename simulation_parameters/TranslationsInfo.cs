using System.Collections.Generic;
using Godot;
using Newtonsoft.Json;

public class TranslationsInfo : IRegistryType
{
    [JsonProperty]
    public Dictionary<string, float> TranslationProgress { get; private set; }

    /// <summary>
    ///   Unused
    /// </summary>
    public string InternalName { get; set; }

    public void Check(string name)
    {
        if (TranslationProgress.Count < 1)
        {
            throw new InvalidRegistryDataException("TranslationsInfo", GetType().Name,
                "translation progress is empty");
        }

        foreach (string locale in TranslationServer.GetLoadedLocales())
        {
            if (!TranslationProgress.ContainsKey(locale))
            {
                throw new InvalidRegistryDataException("TranslationsInfo", GetType().Name,
                    $"translation progress does not contain {locale}");
            }
        }
    }

    public void ApplyTranslations()
    {
    }
}
