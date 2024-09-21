using System.Collections.Generic;
using Godot;
using ThriveScriptsShared;

public class TranslationInfoLocaleChecking(Dictionary<string, float> translationProgress)
    : TranslationsInfo(translationProgress)
{
    public override void Check(string name)
    {
        base.Check(name);

        foreach (string locale in TranslationServer.GetLoadedLocales())
        {
            if (!TranslationProgress.ContainsKey(locale))
            {
                throw new InvalidRegistryDataException("TranslationsInfo", GetType().Name,
                    $"translation progress does not contain {locale}");
            }
        }
    }
}
