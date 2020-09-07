/// <summary>
///   Types of tutorial events sent to the tutorial system
/// </summary>
public enum TutorialEventType
{
    /// <summary>
    ///   Player object was created, args is MicrobeEventArgs
    /// </summary>
    MicrobePlayerSpawned,

    /// <summary>
    ///   Rotation of the player in the microbe stage, args is RotationEventArgs
    /// </summary>
    MicrobePlayerOrientation,

    /// <summary>
    ///   There are tutorial relevant compounds near the player, args is CompoundPositionEventArgs
    /// </summary>
    MicrobeCompoundsNearPlayer,

    /// <summary>
    ///   Reports the player compound amounts while they are alive, args is CompoundBagEventArgs
    /// </summary>
    MicrobePlayerCompounds,

    /// <summary>
    ///   Reports total compounds the player has absorbed, args is CompoundEventArgs
    /// </summary>
    MicrobePlayerTotalCollected,

    /// <summary>
    ///   Player has died
    /// </summary>
    MicrobePlayerDied,

    /// <summary>
    ///   Player is ready to reproduce
    /// </summary>
    MicrobePlayerReadyToEdit,

    /// <summary>
    ///   Player entered the microbe stage
    /// </summary>
    EnteredMicrobeStage,

    /// <summary>
    ///   Player entered the microbe editor
    /// </summary>
    EnteredMicrobeEditor,
}
