using System.ComponentModel;

/// <summary>
///   Contains all the game stages. Used for the Thriveopedia.
///   Includes <b>only</b> stages, not editors.
/// </summary>
public enum Stage
{
    [Description("MICROBE_STAGE")]
    MicrobeStage = 0,

    [Description("MULTICELLULAR_STAGE")]
    MulticellularStage = 1,

    [Description("AWARE_STAGE")]
    AwareStage = 2,

    [Description("AWAKENING_STAGE")]
    AwakeningStage = 3,

    [Description("SOCIETY_STAGE")]
    SocietyStage = 4,

    [Description("INDUSTRIAL_STAGE")]
    IndustrialStage = 5,

    [Description("SPACE_STAGE")]
    SpaceStage = 6,

    [Description("ASCENSION_STAGE")]
    AscensionStage = 7,
}
