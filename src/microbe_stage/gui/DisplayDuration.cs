/// <summary>
///   How long something is shown to the player
/// </summary>
public enum DisplayDuration
{
    Short,
    Normal,
    Long,

    /// <summary>
    ///   Used for messages in prototypes that should really be popups in the final game, but are still important to
    ///   read so they stay on screen for a long time
    /// </summary>
    ExtraLong,
}
