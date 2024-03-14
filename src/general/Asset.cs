using System.IO;
using System.Text;
using Godot;
using Newtonsoft.Json;

/// <summary>
///   A general game asset such as models, sounds etc.
/// </summary>
public class Asset
{
    /// <summary>
    ///   Path to a .tres file or any resource files.
    /// </summary>
    public string ResourcePath { get; set; } = null!;

    [JsonIgnore]
    public string FileName { get; private set; } = null!;

    public AssetType Type { get; set; } = AssetType.Texture2D;

    public string? MeshNodePath { get; set; }

    /// <summary>
    ///   The name of this asset.
    /// </summary>
    public string? Title { get; set; }

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
    ///   Includes the extended description if true (and it's not empty).
    /// </param>
    public string BuildDescription(bool extended)
    {
        var result = new StringBuilder(50);

        if (!string.IsNullOrEmpty(Title) && !string.IsNullOrEmpty(Artist))
        {
            result.Append(Localization.Translate("ARTWORK_TITLE").FormatSafe(Title, Artist));
        }
        else if (string.IsNullOrEmpty(Title) && !string.IsNullOrEmpty(Artist))
        {
            result.Append(Localization.Translate("ART_BY").FormatSafe(Artist));
        }
        else if (!string.IsNullOrEmpty(Title) && string.IsNullOrEmpty(Artist))
        {
            result.Append(Title);
        }

        if (extended && !string.IsNullOrEmpty(Description))
            result.Append($"\n{Description}");

        return result.ToString();
    }

    public void Check()
    {
        if (string.IsNullOrEmpty(ResourcePath))
        {
            throw new InvalidRegistryDataException("asset", GetType().Name, "ResourcePath missing");
        }

        if (!string.IsNullOrEmpty(MeshNodePath) && Type != AssetType.ModelScene)
        {
            throw new InvalidRegistryDataException("asset", GetType().Name,
                "MeshNodePath is specified but asset type is not ModelScene");
        }

        if (!string.IsNullOrEmpty(MeshNodePath) && ResourcePath.GetExtension() != "tscn")
        {
            throw new InvalidRegistryDataException("asset", GetType().Name,
                "MeshNodePath is specified but resource is not a PackedScene");
        }
    }

    public void Resolve()
    {
        if (string.IsNullOrEmpty(MeshNodePath) && Type == AssetType.ModelScene)
        {
            // This sets the visual node path to the root node itself
            MeshNodePath = ".";
        }

        if (Type is AssetType.ModelScene or AssetType.VideoPlayback && !FileHelpers.Exists(ResourcePath))
            throw new FileNotFoundException("The given scene or video file in ResourcePath doesn't exist");

        // When exported only the .import files exist, so this check is done accordingly
        if (Type is AssetType.Texture2D or AssetType.AudioPlayback && !FileHelpers.Exists(ResourcePath + ".import"))
            throw new FileNotFoundException("The given image or audio file in ResourcePath doesn't exist");

        FileName = ResourcePath.GetFile().GetBaseName();
    }
}
