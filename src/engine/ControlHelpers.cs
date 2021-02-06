using Godot;

/// <summary>
///   Common helper operations for Controls
/// </summary>
public static class ControlHelpers
{
    /// <summary>
    ///   Shows the popup and shrinks it to the minimum size, alternative to PopupCentered.
    /// </summary>
    public static void PopupCenteredShrink(this Popup popup)
    {
        popup.PopupCentered(popup.GetMinimumSize());
    }
}
