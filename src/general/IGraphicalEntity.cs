using Godot;
using Newtonsoft.Json;

/// <summary>
///   All graphical game entities implement this interface to provide support for needed operations regarding them
/// </summary>
public interface IGraphicalEntity : IEntity
{
    /// <summary>
    ///   This entity's visual instance.
    /// </summary>
    [JsonIgnore]
    public GeometryInstance EntityGraphics { get; }

    /// <summary>
    ///   The shader material that this entity's geometric instance owns.
    /// </summary>
    [JsonIgnore]
    public Material EntityMaterial { get; }
}
