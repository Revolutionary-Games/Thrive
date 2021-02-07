using Godot;

/// <summary>
///   Common helper operations for Controls
/// </summary>
public static class ControlHelpers
{
    /// <summary>
    ///   Shows the popup in the center of the screen and shrinks it to the minimum size,
    ///   alternative to PopupCentered.
    /// </summary>
    public static void PopupCenteredShrink(this Popup popup, bool runSizeUnstuck = true)
    {
        popup.PopupCentered(popup.GetMinimumSize());

        // In case the popup sizing stuck (this happens sometimes)
        if (runSizeUnstuck)
        {
            Invoke.Instance.Queue(() =>
            {
                // "Refresh" the popup to correct its size
                popup.RectSize = Vector2.Zero;

                // Re-center it
                popup.PopupCentered();
            });
        }
    }
}
