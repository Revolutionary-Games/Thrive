/// <summary>
///   Specifies at which point in time retrieved compounds are sampled (or if the average value should be returned)
/// </summary>
/// <remarks>
///   <para>
///     This is used to implement compound amounts changing during a single in-game day
///   </para>
/// </remarks>
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
    ///   Ambient compound amount given in the biome conditions for a patch. Is not affected by current game time.
    /// </summary>
    Biome,

    /// <summary>
    ///   Ambient compound amount given in the patch template definition.
    /// </summary>
    Template,
}
