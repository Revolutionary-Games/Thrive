using System.Collections.Generic;
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
    }

    public void ApplyTranslations()
    {
    }
}
