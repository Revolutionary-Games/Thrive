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

                var parentRect = popup.GetViewport().GetVisibleRect();

                // Re-center it
                popup.RectPosition = parentRect.Position + (parentRect.Size - popup.RectSize) / 2;
            });
        }
    }

    /// <summary>
    ///   Shows the control in the center of the screen and shrinks it to the minimum size.
    /// </summary>
    public static void ControlCenteredShrink(this Control control, bool runSizeUnstuck = true)
    {
        var parentRect = control.GetViewport().GetVisibleRect();

        var rectSize = control.GetMinimumSize();
        if (rectSize == default(Vector2))
        {
            foreach (Control child in control.GetChildren())
            {
                if (child != null)
                {
                    var pos = child.RectPosition;
                    var min = child.GetCombinedMinimumSize();

                    rectSize.x = Mathf.Max(pos.x + min.x, rectSize.x);
                    rectSize.y = Mathf.Max(pos.y + min.y, rectSize.y);
                }
            }
        }

        var rectPos = parentRect.Position + (parentRect.Size - rectSize) / 2;

        control.SetPosition(rectPos);
        control.SetSize(rectSize);

        control.Visible = true;

        // In case the popup sizing stuck (this happens sometimes)
        if (runSizeUnstuck)
        {
            Invoke.Instance.Queue(() =>
            {
                // "Refresh" the popup to correct its size
                control.RectSize = Vector2.Zero;

                parentRect = control.GetViewport().GetVisibleRect();

                // Re-center it
                control.RectPosition = parentRect.Position + (parentRect.Size - control.RectSize) / 2;
            });
        }
    }
}
