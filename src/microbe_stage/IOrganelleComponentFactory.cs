/// <summary>
///   Generic type for organelle factories
/// </summary>
public interface IOrganelleComponentFactory
{
    /// <summary>
    ///   Creates a new organelle component of the type that this factory makes
    /// </summary>
    /// <returns>The created component.</returns>
    public IOrganelleComponent Create();

    /// <summary>
    ///   Checks that values are valid. Throws InvalidRegistryData if not good.
    /// </summary>
    /// <param name="name">Name of the current object for easier reporting.</param>
    public void Check(string name);
}
