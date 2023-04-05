/// <summary>
///   An abstract resource container that can hold some amount of resources
/// </summary>
public interface IResourceContainer
{
    /// <summary>
    ///   Adds some resources to this container
    /// </summary>
    /// <param name="resource">The resource to add</param>
    /// <param name="amount">The amount to add</param>
    /// <returns>
    ///   The amount that did NOT fit, if 0 then all the <see cref="amount"/> fit and is now in the container
    /// </returns>
    public float Add(WorldResource resource, float amount);

    /// <summary>
    ///   Gets the currently stored amount
    /// </summary>
    /// <param name="resource">Resource type to get</param>
    /// <returns>The amount of available resource of the specified type in the container</returns>
    public float GetAvailableAmount(WorldResource resource);
}
