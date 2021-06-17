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
    AliveMarker AliveMarker { get; }

    void OnDestroyed();

    // TODO: have this implementation here (and also for AliveMarker) once
    /*{
        AliveMarker.Alive = false;
    }*/
}
