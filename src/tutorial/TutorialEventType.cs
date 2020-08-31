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
    ///   Player entered the microbe stage
    /// </summary>
    EnteredMicrobeStage,

    /// <summary>
    ///   Player entered the microbe editor
    /// </summary>
    EnteredMicrobeEditor,
}
