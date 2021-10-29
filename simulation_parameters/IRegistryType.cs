public interface IRegistryType
{
    /// <summary>
    ///   The name referred to this registry object in json
    /// </summary>
    string InternalName { get; set; }

    /// <summary>
    ///   Checks that values are valid. Throws InvalidRegistryData if not good.
    /// </summary>
    /// <param name="name">Name of the current object for easier reporting.</param>
    /// <remarks> Some registry types also process their initial data and create derived data here </remarks>
    void Check(string name);

    /// <summary>
    ///   Fetch translations (if needed) for this object
    /// </summary>
    void ApplyTranslations();
}
