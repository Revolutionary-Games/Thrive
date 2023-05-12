using System.ComponentModel;

/// <summary>
///   Controls how difficult researching some technology is (restricts technologies based on how advanced researching
///   tools are)
/// </summary>
public enum ResearchLevel
{
    /// <summary>
    ///   Level for awakening stage technologies
    /// </summary>
    [Description("TECHNOLOGY_LEVEL_PRE_SOCIETY")]
    PreSociety,

    /// <summary>
    ///   Technology level at the start of society stage
    /// </summary>
    [Description("TECHNOLOGY_LEVEL_PRIMITIVE")]
    Primitive,

    /// <summary>
    ///   Start of industrial revolution technology level
    /// </summary>
    [Description("TECHNOLOGY_LEVEL_INDUSTRIAL")]
    Industrial,

    /// <summary>
    ///   Technology level that humans currently have
    /// </summary>
    [Description("TECHNOLOGY_LEVEL_SPACE_AGE")]
    SpaceAge,

    /// <summary>
    ///   Near future / conceivable technology
    /// </summary>
    [Description("TECHNOLOGY_LEVEL_ADVANCED_SPACE")]
    AdvancedSpace,

    /// <summary>
    ///   Full on science fiction technology level like FTL and ascension
    /// </summary>
    [Description("TECHNOLOGY_LEVEL_SCIFI")]
    Scifi,
}
