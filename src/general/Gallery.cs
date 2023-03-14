using System.Collections.Generic;

/// <summary>
///   A specific collection of Thrive's assets, such as concept art, musics, models.
/// </summary>
public class Gallery : IRegistryType
{
#pragma warning disable 169,649 // Used through reflection
    private string? untranslatedName;
#pragma warning restore 169,649

    /// <summary>
    ///   The user readable name
    /// </summary>
    [TranslateFrom(nameof(untranslatedName))]
    public string Name { get; set; } = null!;

    public Dictionary<string, AssetCategory> AssetCategories { get; set; } = null!;

    public string InternalName { get; set; } = null!;

    public void Check(string name)
    {
        if (AssetCategories == null || AssetCategories.Count <= 0)
            throw new InvalidRegistryDataException(name, GetType().Name, "Missing gallery asset categories");

        foreach (var entry in AssetCategories)
        {
            entry.Value.InternalName = entry.Key;
            entry.Value.Check(name);
        }

        TranslationHelper.CopyTranslateTemplatesToTranslateSource(this);
    }

    public void Resolve()
    {
        foreach (var entry in AssetCategories)
            entry.Value.Resolve();
    }

    public void ApplyTranslations()
    {
        TranslationHelper.ApplyTranslations(this);

        foreach (var entry in AssetCategories)
            entry.Value.ApplyTranslations();
    }
}
