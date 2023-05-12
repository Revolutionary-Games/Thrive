/// <summary>
///   The type of a controller, used to determine which button icon set to use. Only add new values to the end as
///   these are saved as ints.
/// </summary>
public enum ControllerType
{
    /// <summary>
    ///   This value exists to allow putting this in settings. The lower level controller icon methods do not accept
    ///   this value, instead use the <see cref="KeyPromptHelper.ActiveControllerType"/> value.
    /// </summary>
    Automatic,

    Xbox360 = 1,
    XboxOne,
    XboxSeriesX,
    PlayStation3,
    PlayStation4,
    PlayStation5,

    // TODO: add these
    // Switch,
    // SteamDeck,
}
