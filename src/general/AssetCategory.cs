using System.Collections.Generic;
using System.Globalization;
using System.IO;
using Godot;
    
public class AssetCategory : IRegistryType
{
#pragma warning disable 169 // Used through reflection
    private string? untranslatedName;
#pragma warning restore 169

    /// <summary>
    ///   The user readable name
    /// </summary>
    [TranslateFrom("untranslatedName")]
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

    /// <summary>
    ///   A piece of artwork or any game asset to be included in gallery.
    /// </summary>
    public class Asset
    {
        public enum AssetType
        {
            Visual,
            Auditory,
        }

        /// <summary>
        ///   Path to a .tres file or any resource files.
        /// </summary>
        public string ResourcePath { get; set; } = null!;

        public AssetType Type { get; set; } = AssetType.Visual;

        /// <summary>
        ///   The name of this artwork.
        /// </summary>
        public string? Title { get; set; }

        /// <summary>
        ///   The name of the artist behind this art.
        /// </summary>
        public string? Artist { get; set; }

        /// <summary>
        ///   Extended description of this artwork.
        /// </summary>
        public string? Description { get; set; }

        /// <summary>
        ///   Combines artwork title, artist name and extended description into one structured string.
        /// </summary>
        /// <param name="extended">
        ///     Includes the extended description if true (and it's not empty).
        /// </param>
        public string BuildDescription(bool extended)
        {
            var result = string.Empty;

            if (!string.IsNullOrEmpty(Title) && !string.IsNullOrEmpty(Artist))
            {
                result += string.Format(
                    CultureInfo.CurrentCulture, TranslationServer.Translate("ARTWORK_TITLE"), Title, Artist);
            }
            else if (string.IsNullOrEmpty(Title) && !string.IsNullOrEmpty(Artist))
            {
                result += string.Format(CultureInfo.CurrentCulture, TranslationServer.Translate("ART_BY"), Artist);
            }
            else if (!string.IsNullOrEmpty(Title) && string.IsNullOrEmpty(Artist))
            {
                result += Title;
            }

            if (extended && !string.IsNullOrEmpty(Description))
                result += $"\n{Description}";

            return result;
        }

        public void Check()
        {
            if (string.IsNullOrEmpty(ResourcePath))
            {
                throw new InvalidRegistryDataException(
                    "artwork", GetType().Name, "ResourcePath missing");
            }
        }

        public void Resolve()
        {
            // When exported only the .import files exist, so this check is done accordingly
            if (!FileHelpers.Exists(ResourcePath + ".import"))
                throw new FileNotFoundException("The given image file in ResourcePath doesn't exist");
        }
    }
}
