public enum TwoDimensionalMovementMode
{
    /// <summary>
    ///   Automatically detect based on if controller or keyboard is used (Thrive movement is traditionally player
    ///   relative)
    /// </summary>
    Automatic = 0,

    /// <summary>
    ///   Left and right are relative to the player's orientation
    /// </summary>
    PlayerRelative,

    /// <summary>
    ///   Inputs are always related to the screen
    /// </summary>
    ScreenRelative,
}
