using System.ComponentModel;

/// <summary>
///   Contains all the game stages. Used for the Thriveopedia. Includes <b>only</b> stages, not editors.
/// </summary>
/// <remarks>
///   <para>
///     In the Thrive project there is also MainGameState. Reordering values here will cause issues.
///   </para>
/// </remarks>
public enum Stage
{
    [Description("MICROBE_STAGE")]
    MicrobeStage = 0,

    [Description("MULTICELLULAR_STAGE")]
    MulticellularStage = 1,

    [Description("MACROSCOPIC_STAGE")]
    MacroscopicStage = 2,

    [Description("AWARE_STAGE")]
    AwareStage = 3,

    [Description("AWAKENING_STAGE")]
    AwakeningStage = 4,

    [Description("SOCIETY_STAGE")]
    SocietyStage = 5,

    [Description("INDUSTRIAL_STAGE")]
    IndustrialStage = 6,

    [Description("SPACE_STAGE")]
    SpaceStage = 7,

    [Description("ASCENSION_STAGE")]
    AscensionStage = 8,
}
