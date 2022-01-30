using System.Collections.Generic;
using System.Globalization;
using System.IO;
using Godot;

/// <summary>
///   Collection of stage-specific concept arts and artworks
/// </summary>
public class Gallery : IRegistryType
{
    public List<Artwork> Artworks = null!;

    public string InternalName { get; set; } = null!;

    public void Check(string name)
    {
        foreach (var entry in Artworks)
        {
            entry.Check();
        }
    }

    public void Resolve()
    {
        foreach (var entry in Artworks)
        {
            entry.Resolve();
        }
    }

    public void ApplyTranslations()
    {
    }

    /// <summary>
    ///   A piece of concept art or any artworks for Thrive.
    /// </summary>
    public class Artwork
    {
        public string ResourcePath { get; set; } = null!;

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
                    "artwork", GetType().Name, "ResourcePath missing for art texture");
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
