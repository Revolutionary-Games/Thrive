/// <summary>
///   Something that can show progress in a <see cref="BuildQueueItem"/>
/// </summary>
public interface IBuildQueueProgressItem
{
    public string ItemName { get; }

    /// <summary>
    ///   Progress of the items. In range of 0-1
    /// </summary>
    public float Progress { get; }
}
