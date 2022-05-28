using System.Globalization;
using System.IO;
using Godot;

/// <summary>
///   A general game asset such as models, sounds etc.
/// </summary>
public class Asset
{
    /// <summary>
    ///   Path to a .tres file or any resource files.
    /// </summary>
    public string ResourcePath { get; set; } = null!;

    public AssetType Type { get; set; } = AssetType.Texture;

    public string? MeshNodePath { get; set; }

    /// <summary>
    ///   The name of this asset.
    /// </summary>
    public string Title { get; set; } = null!;

    /// <summary>
    ///   The name of the artist behind this asset.
    /// </summary>
    public string? Artist { get; set; }

    /// <summary>
    ///   Extended description of this asset.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    ///   Combines asset title, artist name and extended description into one structured string.
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
                "asset", GetType().Name, "ResourcePath missing");
        }

        if (!string.IsNullOrEmpty(MeshNodePath) && Type != AssetType.ModelScene)
        {
            throw new InvalidRegistryDataException(
                "asset", GetType().Name, "MeshNodePath is specified but asset type is not ModelScene");
        }

        if (!string.IsNullOrEmpty(MeshNodePath) && ResourcePath.Extension() != "tscn")
        {
            throw new InvalidRegistryDataException(
                "asset", GetType().Name, "MeshNodePath is specified but resource is not a PackedScene");
        }
    }

    public void Resolve()
    {
        if (string.IsNullOrEmpty(Title))
        {
            // File name as the default title
            Title = ResourcePath.GetFile().BaseName();
        }

        if (string.IsNullOrEmpty(MeshNodePath) && Type == AssetType.ModelScene)
        {
            // This sets the visual node path to the root node itself
            MeshNodePath = ".";
        }

        if (Type is AssetType.ModelScene or AssetType.VideoPlayback && !FileHelpers.Exists(ResourcePath))
            throw new FileNotFoundException("The given scene file in ResourcePath doesn't exist");

        // When exported only the .import files exist, so this check is done accordingly
        if (Type is AssetType.Texture or AssetType.AudioPlayback && !FileHelpers.Exists(ResourcePath + ".import"))
            throw new FileNotFoundException("The given image file in ResourcePath doesn't exist");
    }
}
