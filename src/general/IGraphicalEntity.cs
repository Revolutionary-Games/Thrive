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

    [JsonIgnore]
    public int RenderPriority { get; set; }
}
