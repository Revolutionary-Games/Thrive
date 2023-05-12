/// <summary>
///   Which level a tab control is at on a screen, controls which set of tab switching hotkeys are used
/// </summary>
public enum TabLevel
{
    Primary,
    Secondary,

    /// <summary>
    ///   If there's too many levels, some tab controls need to be put on the uncontrollable level, which disables
    ///   the hotkeys for changing the tabs
    /// </summary>
    Uncontrollable,
}
