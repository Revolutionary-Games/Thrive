using Godot;
using Newtonsoft.Json;

/// <summary>
///   All game entities implement this interface to provide support for needed operations regarding them
/// </summary>
public interface IEntity
{
    /// <summary>
    ///   Gets an alive marker associated with this entity. When this is Freed or QueueFreed this marker needs to be
    ///   set to non-alive state.
    /// </summary>
    /// <remarks>
    ///   <para>
    ///     For now this is JSON ignored as all objects that refer to each other can easily re-grab the alive marker.
    ///   </para>
    /// </remarks>
    [JsonIgnore]
    public AliveMarker AliveMarker { get; }

    /// <summary>
    ///   The Node that this entity is in the game world as
    /// </summary>
    [JsonIgnore]
    public Spatial EntityNode { get; }

    public void OnDestroyed();

    // TODO: have this implementation here (and also for AliveMarker) once Godot updates their dotnet runtime version
    // requirement, currently this doesn't compile if this default implementation is uncommented
    /*{
        AliveMarker.Alive = false;
    }*/
}
