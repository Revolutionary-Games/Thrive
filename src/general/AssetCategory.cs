using System.Collections.Generic;

public class AssetCategory : IRegistryType
{
#pragma warning disable 169,649 // Used through reflection
    private string? untranslatedName;
#pragma warning restore 169,649

    /// <summary>
    ///   The user readable name
    /// </summary>
    [TranslateFrom(nameof(untranslatedName))]
    public string Name { get; set; } = null!;

    public List<Asset> Assets { get; set; } = null!;

    public string InternalName { get; set; } = null!;

    public void Check(string name)
    {
        if (string.IsNullOrEmpty(Name))
            throw new InvalidRegistryDataException("Empty category name");

        if (Assets == null || Assets.Count <= 0)
            throw new InvalidRegistryDataException("Assets is null");

        if (string.IsNullOrEmpty(InternalName))
            throw new InvalidRegistryDataException("InternalName is not set");

        foreach (var asset in Assets)
            asset.Check();

        TranslationHelper.CopyTranslateTemplatesToTranslateSource(this);
    }

    public void Resolve()
    {
        foreach (var entry in Assets)
            entry.Resolve();
    }

    public void ApplyTranslations()
    {
        TranslationHelper.ApplyTranslations(this);
    }
}
