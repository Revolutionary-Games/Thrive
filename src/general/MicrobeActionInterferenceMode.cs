/// <summary>
///   Describes how two microbe actions interference with each other.
/// </summary>
/// <remarks>
///   <para>
///     Used for MP calculation
///   </para>
/// </remarks>
public enum MicrobeActionInterferenceMode
{
    /// <summary>
    ///   The two actions are completely independent
    /// </summary>
    NoInterference,

    /// <summary>
    ///   The other action replaces the this one
    /// </summary>
    ReplacesOther,

    /// <summary>
    ///   The two actions cancel out each other
    /// </summary>
    CancelsOut,

    /// <summary>
    ///   The two actions can be combined to a whole different action.
    ///   Call <see cref="CombinableActionData.Combine"/> to get this action.
    /// </summary>
    Combinable,
}
