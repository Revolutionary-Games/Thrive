/// <summary>
///   Something that has a player readable name
/// </summary>
public interface IPlayerReadableName
{
    public string ReadableName { get; }

    // TODO: remove this if not needed
    // /// <summary>
    // ///   Access to the internal name, needed when copying data and the ability to translate later needs
    // ///   to be preserved
    // /// </summary>
    // public string InternalName { get; }
}
