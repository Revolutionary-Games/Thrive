﻿namespace ThriveScriptsShared;

using Newtonsoft.Json;

public class TranslationsInfo(Dictionary<string, float> translationProgress) : IRegistryType
{
    [JsonProperty]
    public Dictionary<string, float> TranslationProgress { get; private set; } = translationProgress;

    /// <summary>
    ///   Unused
    /// </summary>
    public string InternalName { get; set; } = null!;

    public virtual void Check(string name)
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
