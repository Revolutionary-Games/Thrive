/// <summary>
///   Controls how difficult researching some technology is (restricts technologies based on how advanced researching
///   tools are)
/// </summary>
public enum ResearchLevel
{
    /// <summary>
    ///   Level for awakening stage technologies
    /// </summary>
    PreSociety,

    /// <summary>
    ///   Technology level at the start of society stage
    /// </summary>
    Primitive,

    /// <summary>
    ///   Start of industrial revolution technology level
    /// </summary>
    Industrial,

    /// <summary>
    ///   Technology level that humans currently have
    /// </summary>
    SpaceAge,

    /// <summary>
    ///   Near future / conceivable technology
    /// </summary>
    AdvancedSpace,

    /// <summary>
    ///   Full on science fiction technology level like FTL and ascension
    /// </summary>
    Scifi,
}
