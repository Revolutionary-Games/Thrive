using System.Collections.Generic;
using Godot;
using Newtonsoft.Json;

/// <summary>
///   A region is a something like a continent/ocean that contains multiple patches.
/// </summary>
[UseThriveSerializer]
[JsonObject(IsReference = true)]
public class PatchRegion
{
    // TODO: Move these to Constants.cs

    [JsonIgnore]
    public readonly float RegionLineWidth = 4.0f;

    [JsonIgnore]
    public readonly float PatchMargin = 4.0f;

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

    [JsonConstructor]
    public PatchRegion(int id, LocalizedString name, RegionType type, Vector2 screenCoordinates,
        float height, float width, bool usesSpecialLinking)
    {
        ID = id;
        Name = name;
        Type = type;
        ScreenCoordinates = screenCoordinates;
        Height = height;
        Width = width;
        UsesSpecialLinking = usesSpecialLinking;
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
    public RegionType Type { get; }

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
    public LocalizedString Name { get; private set; }

    /// <summary>
    ///   Coordinates this region is to be displayed at in the GUI
    /// </summary>
    [JsonProperty]
    public Vector2 ScreenCoordinates { get; set; }

    /// <summary>
    ///   When <c>true</c> this region is marked as having special line connection and drawing logic
    /// </summary>
    [JsonProperty]
    public bool UsesSpecialLinking { get; set; }

    /// <summary>
    ///   The patches in this region. This is last because other constructor params need to be loaded from JSON first
    ///   and also this can't be a JSON constructor parameter because the patches refer to this so we couldn't
    ///   construct anything to begin with.
    /// </summary>
    [JsonProperty]
    public List<Patch> Patches { get; private set; } = null!;

    /// <summary>
    ///   Adds a connection to region
    /// </summary>
    /// <returns>True if this was new, false if already added</returns>
    public bool AddNeighbour(PatchRegion region)
    {
        return Adjacent.Add(region);
    }
}
