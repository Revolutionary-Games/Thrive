using System.Collections.Generic;
using System.Globalization;
using System.IO;
using Godot;

/// <summary>
///   A gallery of Thrive's assets, stage-specific concept arts, artworks and the likes
/// </summary>
public class Gallery : IRegistryType
{
    /// <summary>
    ///   Collection of assets within a category.
    /// </summary>
    public Dictionary<string, List<Asset>> Assets;

    public string InternalName { get; set; }

    public void Check(string name)
    {
        if (Assets == null || Assets.Count <= 0)
            throw new InvalidRegistryDataException(name, GetType().Name, "Missing gallery assets");

        foreach (var entry in Assets)
        {
            foreach (var art in entry.Value)
                art.Check();
        }
    }

    public void Resolve()
    {
        foreach (var entry in Assets)
        {
            foreach (var art in entry.Value)
                art.Resolve();
        }
    }

    public void ApplyTranslations()
    {
    }

    /// <summary>
    ///   A piece of artwork or any game asset to be included in gallery.
    /// </summary>
    public class Asset
    {
        /// <summary>
        ///   Path to a .tres file or any resource files.
        /// </summary>
        public string ResourcePath { get; set; }

        /// <summary>
        ///   The name of this artwork.
        /// </summary>
        public string Title { get; set; }

        /// <summary>
        ///   The name of the artist behind this art.
        /// </summary>
        public string Artist { get; set; }

        /// <summary>
        ///   Extended description of this artwork.
        /// </summary>
        public string Description { get; set; }

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
