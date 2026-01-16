/// <summary>
///   The top level active game state (matches which Godot scene is active).
///   Menu doesn't have its own state as saving is not possible in the menu
/// </summary>
/// <remarks>
///   <para>
///     When adding new values, only add them at the end to not break existing saves
///   </para>
/// </remarks>
public enum MainGameState
{
    /// <summary>
    ///   Invalid value
    /// </summary>
    Invalid,

    /// <summary>
    ///   Microbe stage
    /// </summary>
    [Alias("microbe")]
    MicrobeStage,

    /// <summary>
    ///   Microbe editor
    /// </summary>
    MicrobeEditor,

    [Alias("multicellular")]
    MulticellularEditor,

    /// <summary>
    ///   The macroscopic environment that is 3D
    /// </summary>
    [Alias("macroscopic")]
    MacroscopicStage,

    MacroscopicEditor,

    [Alias("society")]
    SocietyStage,

    [Alias("industrial")]
    IndustrialStage,

    [Alias("space")]
    SpaceStage,

    /// <summary>
    ///   The cutscene where the player gets to ascension
    /// </summary>
    [Alias("ascension")]
    AscensionCeremony,
}
