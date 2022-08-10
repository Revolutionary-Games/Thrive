/// <summary>
///   A game resource loadable through <see cref="ResourceManager"/>
/// </summary>
public interface IResource
{
    public bool RequiresSyncLoad { get; }

    public bool UsesPostProcessing { get; }

    public bool RequiresSyncPostProcess { get; }

    /// <summary>
    ///   Should estimate roughly how long loading this resource takes in the usual case. This is used to skip more
    ///   loading work if there isn't that much time budget remaining in a frame. This should be very cheap to ask
    ///   and if not then this should be cached by the resource type.
    /// </summary>
    public float EstimatedTimeRequired { get; }

    /// <summary>
    ///   Set to true once <see cref="PrepareLoading"/> is called. Set by <see cref="ResourceManager"/>
    /// </summary>
    public bool LoadingPrepared { get; set; }

    /// <summary>
    ///   Set to true once the resource is loaded (either in <see cref="Load"/> or after post processing)
    /// </summary>
    public bool Loaded { get; }

    /// <summary>
    ///   Used to uniquely identify what resource this is
    /// </summary>
    public string Identifier { get; }

    /// <summary>
    ///   Loading is prepared in a background operation
    /// </summary>
    public void PrepareLoading();

    /// <summary>
    ///   The actual resource load happens. If <see cref="RequiresSyncLoad"/> is true this is called on the main thread.
    /// </summary>
    public void Load();

    /// <summary>
    ///   Post processing of the resource. Only called if <see cref="UsesPostProcessing"/> is true.
    ///   If <see cref="RequiresSyncPostProcess"/> is true this is called on the main thread.
    /// </summary>
    public void PerformPostProcessing();
}
