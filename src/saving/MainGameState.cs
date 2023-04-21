/// <summary>
///   The top level active game state (matches which Godot scene is active).
///   Menu doesn't have its own state as saving is not possible in the menu
/// </summary>
/// <remarks>
///   <para>
///     When adding new values only add them at the end to not break existing saves
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
    MicrobeStage,

    /// <summary>
    ///   Microbe editor
    /// </summary>
    MicrobeEditor,

    EarlyMulticellularEditor,

    /// <summary>
    ///   The late multicellular environment that is 3D
    /// </summary>
    MulticellularStage,

    LateMulticellularEditor,

    SocietyStage,

    IndustrialStage,
}
