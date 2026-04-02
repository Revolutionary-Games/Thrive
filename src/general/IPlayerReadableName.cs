/// <summary>
///   Something that has a player-readable name
/// </summary>
public interface IPlayerReadableName
{
    /// <summary>
    ///   Primary user-readable name of this thing (should be translated)
    /// </summary>
    public string ReadableName { get; }

    /// <summary>
    ///   A more exact identifier that includes technical details like coordinate location (potentially). Defaults to
    ///   the same as <see cref="ReadableName"/> but some types provide more exact information with this.
    /// </summary>
    public string ReadableExactIdentifier => ReadableName;
}
