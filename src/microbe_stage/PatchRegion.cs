using System.Collections.Generic;
using Godot;
using Newtonsoft.Json;

/// <summary>
///   A region is a something like a continent/ocean that contains multiple patches.
/// </summary>
[UseThriveSerializer]
public class PatchRegion
{
    // TODO: Move these to Constants.cs

    [JsonIgnore]
    public readonly float RegionLineWidth = 4.0f;

    [JsonIgnore]
    public readonly float PatchMargin = 4.0f;

    [JsonConstructor]
    public PatchRegion(int id, LocalizedString name, RegionType type, Vector2 screenCoordinates,
        float height, float width, List<Patch> patches, bool isForDrawingOnly)
    {
        ID = id;
        Name = name;
        Type = type;
        Patches = patches;
        ScreenCoordinates = screenCoordinates;
        Height = height;
        Width = width;
        IsForDrawingOnly = isForDrawingOnly;
    }

    public PatchRegion(int id, LocalizedString name, RegionType regionType, Vector2 screenCoordinates)
    {
        ID = id;
        Patches = new List<Patch>();
        Name = name;
        Height = 0;
        Width = 0;
        Type = regionType;
        ScreenCoordinates = screenCoordinates;
    }

    public enum RegionType
    {
        Predefined,
        Sea,
        Ocean,
        Continent,
        Vent,
        Cave,
    }

    [JsonProperty]
    public int ID { get; }

    /// <summary>
    ///   Regions this is next to
    /// </summary>
    /// <remarks>
    ///   <para>
    ///     TODO: this may contain non-simulation regions (drawing only) which must be refactored out of here
    ///   </para>
    /// </remarks>
    [JsonIgnore]
    public ISet<PatchRegion> Adjacent { get; } = new HashSet<PatchRegion>();

    [JsonProperty]
    public RegionType Type { get; }

    [JsonProperty]
    public float Height { get; set; }

    [JsonProperty]
    public float Width { get; set; }

    [JsonIgnore]
    public Vector2 Size
    {
        get => new(Width, Height);
        set
        {
            Width = value.x;
            Height = value.y;
        }
    }

    [JsonProperty]
    public List<Patch> Patches { get; set; }

    [JsonProperty]
    public LocalizedString Name { get; private set; }

    /// <summary>
    ///   Coordinates this region is to be displayed at in the GUI
    /// </summary>
    [JsonProperty]
    public Vector2 ScreenCoordinates { get; set; }

    [JsonProperty]
    public bool IsForDrawingOnly { get; set; }

    /// <summary>
    ///   Adds a connection to region
    /// </summary>
    /// <returns>True if this was new, false if already added</returns>
    public bool AddNeighbour(PatchRegion region)
    {
        return Adjacent.Add(region);
    }
}
