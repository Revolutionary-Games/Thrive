using System.Collections.Generic;
using Godot;
using Godot.Collections;

/// <summary>
///   Common helper operations for Controls
/// </summary>
public static class ControlHelpers
{
    /// <summary>
    ///   Shows the popup in the center of the screen and shrinks it to the minimum size,
    ///   alternative to PopupCentered.
    /// </summary>
    /// <remarks>
    ///   <para>
    ///     NOTE: This should be rarely used since for popups you should've already been using the ones
    ///     deriving from <see cref="CustomWindow"/> anyway. This is kept here as a backup.
    ///   </para>
    /// </remarks>
    public static void PopupCenteredShrink(this Popup popup, bool runSizeUnstuck = true)
    {
        popup.PopupCentered(popup.GetMinimumSize());

        // In case the popup sizing stuck (this happens sometimes)
        if (runSizeUnstuck)
        {
            Invoke.Instance.Queue(() =>
            {
                popup.MoveToCenter();

                // CustomRichTextLabel-based dialogs are especially vulnerable, thus do double unstucking
                Invoke.Instance.Queue(() => popup.MoveToCenter());
            });
        }
    }

    public static void MoveToCenter(this Control popup)
    {
        // "Refresh" control to correct its size
        popup.RectSize = Vector2.Zero;

        var parentRect = popup.GetViewportRect();

        // Center it
        popup.RectPosition = parentRect.Position + (parentRect.Size - popup.RectSize) / 2;
    }

    /// <summary>
    ///   Registers focus handlers for control so that it automatically is skipped over if it gets focused
    /// </summary>
    /// <remarks>
    ///   <para>
    ///     This is not usable in a situation where the control creates children dynamically or needs to in other cases
    ///     not always forward the focus to the next node.
    ///   </para>
    ///   <para>
    ///     TODO: test that this works as this didn't end up useful in the case this was made for due to the previous
    ///     point
    ///   </para>
    /// </remarks>
    /// <param name="control">The control to make transparently move the focus</param>
    /// <param name="adjustNextNodePreviousLinks">
    ///   If true the next node <see cref="control"/> points to will be updated to have the previous NodePaths in it
    ///   point behind the <see cref="control"/>
    /// </param>
    public static void BecomeFocusForwarder(this Control control, bool adjustNextNodePreviousLinks = true)
    {
        control.Connect("focus_entered", GUICommon.Instance, nameof(GUICommon.ProxyFocusForward), new Array(control));

        if (!adjustNextNodePreviousLinks)
            return;

        var next = control.GetNextControl();
        var previous = control.GetPreviousControl();

        if (next == null || previous == null)
        {
            GD.PrintErr(
                "Could not find next or previous node to link them properly together for a focus forwarder node");
            return;
        }

        var previousPath = previous.GetPath();

        var currentPath = control.GetPath();

        if (next.ResolveToAbsolutePath(next.FocusPrevious) == currentPath)
            next.FocusPrevious = previousPath;
        if (next.ResolveToAbsolutePath(next.FocusNeighbourLeft) == currentPath)
            next.FocusNeighbourLeft = previousPath;
        if (next.ResolveToAbsolutePath(next.FocusNeighbourRight) == currentPath)
            next.FocusNeighbourRight = previousPath;
        if (next.ResolveToAbsolutePath(next.FocusNeighbourBottom) == currentPath)
            next.FocusNeighbourBottom = previousPath;
    }

    /// <summary>
    ///   Moves focus to the next, bottom or right focus neighbour
    /// </summary>
    /// <param name="control">The node to read the next focus control from</param>
    public static void ForwardFocusToNext(this Control control)
    {
        var next = control.GetNextControl();
        next?.GrabFocus();
    }

    public static Control? GetNextControl(this Control control)
    {
        var path = control.FocusNext ?? control.FocusNeighbourBottom ?? control.FocusNeighbourRight;

        if (path == null)
        {
            GD.PrintErr($"No next Control found to focus after {control.GetPath()}");
            return null;
        }

        var result = control.GetNode<Control>(path);

        if (result == null)
            GD.PrintErr($"Failed to get control from NodePath: {path}");

        return result;
    }

    public static Control? GetPreviousControl(this Control control)
    {
        var path = control.FocusPrevious ?? control.FocusNeighbourTop ?? control.FocusNeighbourLeft;

        if (path == null)
        {
            GD.PrintErr($"No previous Control found to focus before {control.GetPath()}");
            return null;
        }

        var result = control.GetNode<Control>(path);

        if (result == null)
            GD.PrintErr($"Failed to get control from NodePath: {path}");

        return result;
    }

    public static void RegisterCustomFocusDrawer(this Control control)
    {
        control.Connect("draw", GUICommon.Instance, nameof(GUICommon.ProxyDrawFocus), new Array(control));
        control.Connect("focus_entered", control, "update");
        control.Connect("focus_exited", control, "update");
    }

    /// <summary>
    ///   Returns the given control or the first child (depth first) that is focusable. Doesn't traverse over non
    ///   <see cref="Control"/> nodes.
    /// </summary>
    /// <param name="control">The control to start checking from</param>
    /// <returns>The found focusable control or null if nothing is focusable</returns>
    public static Control? FirstFocusableControl(this Control control)
    {
        if (control.FocusMode != Control.FocusModeEnum.None)
            return control;

        int count = control.GetChildCount();

        for (int i = 0; i < count; ++i)
        {
            var child = control.GetChild(i);

            if (child is Control childAsControl)
            {
                var childResult = FirstFocusableControl(childAsControl);

                if (childResult != null)
                    return childResult;
            }
        }

        return null;
    }

    /// <summary>
    ///   Maps each given control to its first child (depth first) that is focusable. If something has no focusable
    ///   children it is not output at all.
    /// </summary>
    /// <param name="controlsToMap">The list of controls to find the first focusable child of</param>
    /// <returns>The focusable children</returns>
    public static IEnumerable<Control> SelectFirstFocusableChild(this IEnumerable<Control> controlsToMap)
    {
        foreach (var control in controlsToMap)
        {
            var mapped = control.FirstFocusableControl();

            if (mapped != null)
                yield return mapped;
        }
    }

    public static void DrawCustomFocusBorderIfFocused(this Control control)
    {
        control._Draw();

        if (!control.HasFocus())
            return;

        // var rect = control.GetRect();
        var size = control.GetRect().Size;

        int cornerRadius = Constants.CUSTOM_FOCUS_DRAWER_RADIUS;
        float quarterCircle = (float)(MathUtils.FULL_CIRCLE * 0.25f);

        // Lines
        // Top line
        control.DrawLine(new Vector2(cornerRadius, 0),
            new Vector2(size.x - cornerRadius, 0),
            Constants.CustomFocusDrawerColour, Constants.CUSTOM_FOCUS_DRAWER_WIDTH,
            Constants.CUSTOM_FOCUS_DRAWER_ANTIALIAS);

        // Bottom line
        control.DrawLine(new Vector2(cornerRadius, size.y),
            new Vector2(size.x - cornerRadius, size.y),
            Constants.CustomFocusDrawerColour, Constants.CUSTOM_FOCUS_DRAWER_WIDTH,
            Constants.CUSTOM_FOCUS_DRAWER_ANTIALIAS);

        // Left
        control.DrawLine(new Vector2(0, cornerRadius),
            new Vector2(0, size.y - cornerRadius),
            Constants.CustomFocusDrawerColour, Constants.CUSTOM_FOCUS_DRAWER_WIDTH,
            Constants.CUSTOM_FOCUS_DRAWER_ANTIALIAS);

        // Right
        control.DrawLine(new Vector2(size.x, cornerRadius),
            new Vector2(size.x, size.y - cornerRadius),
            Constants.CustomFocusDrawerColour, Constants.CUSTOM_FOCUS_DRAWER_WIDTH,
            Constants.CUSTOM_FOCUS_DRAWER_ANTIALIAS);

        // Corners
        // Top left corner
        var arcWidth = Constants.CUSTOM_FOCUS_DRAWER_WIDTH;

        control.DrawArc(new Vector2(cornerRadius, cornerRadius), cornerRadius,
            quarterCircle * 2, quarterCircle * 3,
            Constants.CUSTOM_FOCUS_DRAWER_RADIUS_POINTS, Constants.CustomFocusDrawerColour,
            arcWidth, Constants.CUSTOM_FOCUS_DRAWER_ANTIALIAS);

        // Top right
        control.DrawArc(new Vector2(size.x - cornerRadius, cornerRadius), cornerRadius,
            quarterCircle * 3, quarterCircle * 4,
            Constants.CUSTOM_FOCUS_DRAWER_RADIUS_POINTS, Constants.CustomFocusDrawerColour,
            arcWidth, Constants.CUSTOM_FOCUS_DRAWER_ANTIALIAS);

        // Bottom right
        control.DrawArc(new Vector2(size.x - cornerRadius, size.y - cornerRadius), cornerRadius,
            0, quarterCircle,
            Constants.CUSTOM_FOCUS_DRAWER_RADIUS_POINTS, Constants.CustomFocusDrawerColour,
            arcWidth, Constants.CUSTOM_FOCUS_DRAWER_ANTIALIAS);

        // Bottom left
        control.DrawArc(new Vector2(cornerRadius, size.y - cornerRadius), cornerRadius,
            quarterCircle, quarterCircle * 2,
            Constants.CUSTOM_FOCUS_DRAWER_RADIUS_POINTS, Constants.CustomFocusDrawerColour,
            arcWidth, Constants.CUSTOM_FOCUS_DRAWER_ANTIALIAS);
    }
}
