/// <summary>
///   Generic type for organelle factories
/// </summary>
public interface IOrganelleComponentFactory
{
    /// <summary>
    ///   Creates a new organelle component of the type that this factory makes
    /// </summary>
    /// <returns>The created component.</returns>
    /// <remarks>
    ///   <para>
    ///     TODO: refactor this to take in <see cref="PlacedOrganelle"/> to allow more easily initializing the
    ///     component state in the constructor rather than using null overrides so much.
    ///   </para>
    /// </remarks>
    public IOrganelleComponent Create();

    /// <summary>
    ///   Checks that values are valid. Throws InvalidRegistryData if not good.
    /// </summary>
    /// <param name="name">Name of the current object for easier reporting.</param>
    public void Check(string name);
}
