/// <summary>
///   Marks organelle as having some feature. This is a more lightweight system than full on components and is used for
///   feature markers that don't need the full-blown <see cref="IOrganelleComponent"/> functionality.
/// </summary>
public enum OrganelleFeatureTag
{
    SignalingAgent,
    Axon,
    BindingAgent,
    Myofibril,
    Nucleus,

    /// <summary>
    ///   Adds a stabby thing to the cell, positioned similarly to the flagellum
    /// </summary>
    Pilus,
}
