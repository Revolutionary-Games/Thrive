using System.Collections.Generic;
using Godot;
using Newtonsoft.Json;

/// <summary>
///   Collection of stage-specific concept arts and artworks
/// </summary>
public class Gallery : IRegistryType
{
    public List<ConceptArt> ConceptArts;

    public string InternalName { get; set; }

    public void Check(string name)
    {
        foreach (var entry in ConceptArts)
        {
            entry.Check();
        }
    }

    public void Resolve()
    {
        foreach (var entry in ConceptArts)
        {
            entry.Resolve();
        }
    }

    public void ApplyTranslations()
    {
    }

    public class ConceptArt
    {
        [JsonIgnore]
        public Texture LoadedImage;

        public string ResourcePath { get; set; }

        public string Title { get; set; }

        public string Artist { get; set; }

        public string Description { get; set; }

        /// <summary>
        ///   Builds the description string for this artwork
        /// </summary>
        /// <param name="extended">
        ///     Includes the extended description if true (and it's not empty).
        /// </param>
        public string BuildDescription(bool extended)
        {
            var result = string.Empty;

            if (!string.IsNullOrEmpty(Title) && !string.IsNullOrEmpty(Artist))
            {
                result += $"\"{Title}\" - {Artist}";
            }
            else if (string.IsNullOrEmpty(Title) && !string.IsNullOrEmpty(Artist))
            {
                result += $"Art by {Artist}";
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
                    "conceptArt", GetType().Name, "ResourcePath missing for art texture");
            }
        }

        public void Resolve()
        {
            LoadedImage = GD.Load<Texture>(ResourcePath);
        }
    }
}
