public interface IRegistryType
{
    /// <summary>
    /// Checks that values are valid. Throws InvalidRegistryData if not good.
    /// </summary>
    /// <param name="name">Name of the current object for easier reporting.</param>
    void Check(string name);
}
