using Newtonsoft.Json;

/// <summary>
///   Any kind of object that is tracked to be alive or dead, allows <see cref="EntityReference{T}"/> to work
/// </summary>
[JSONAlwaysDynamicType]
public interface IAliveTracked
{
    /// <summary>
    ///   Gets an alive marker associated with this entity (object). When this is Freed or QueueFreed this marker needs
    ///   to be set to non-alive state if this is a Godot entity. In other cases use the relevant destruction logic
    ///   for that object type.
    /// </summary>
    /// <remarks>
    ///   <para>
    ///     For now this is JSON ignored as all objects that refer to each other can easily re-grab the alive marker.
    ///   </para>
    /// </remarks>
    [JsonIgnore]
    public AliveMarker AliveMarker { get; }

    public void OnDestroyed();

    // TODO: have this implementation here (and also for AliveMarker) once Godot updates their dotnet runtime version
    // requirement, currently this doesn't compile if this default implementation is uncommented
    /*{
        AliveMarker.Alive = false;
    }*/
}
