public enum ThreeDimensionalMovementMode
{
    /// <summary>
    ///   Left/right and forward/back are relative to the player's look direction (what the screen shows)
    /// </summary>
    ScreenRelative = 0,

    /// <summary>
    ///   Inputs are always related to the world (this is a bit of a troll option added solely because people have been
    ///   complaining about the default 2D movement scheme for a long time)
    /// </summary>
    WorldRelative,
}
