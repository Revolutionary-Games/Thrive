/// <summary>
///   Options for retrieving compound amounts in a patch for complex compound types.
/// </summary>
public enum CompoundAmountType
{
    /// <summary>
    ///   Ambient compound amount at the present moment.
    /// </summary>
    Current,

    /// <summary>
    ///   Maximum possible ambient compound amount.
    /// </summary>
    Maximum,

    /// <summary>
    ///   Average ambient compound amount over the course of a day.
    /// </summary>
    Average,

    /// <summary>
    ///   Ambient compound amount given in the patch template definition.
    /// </summary>
    Template,
}
