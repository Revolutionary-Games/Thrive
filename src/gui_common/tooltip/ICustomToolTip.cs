using Godot;

/// <summary>
///   Methods of where a tooltip should be positioned on display.
/// </summary>
public enum ToolTipPositioning
{
    /// <summary>
    ///   Tooltip positioned at the last cursor position after entering a tooltip enabled area.
    /// </summary>
    LastMousePosition,

    /// <summary>
    ///   Tooltip constantly positioned at the same position as the cursor.
    /// </summary>
    FollowMousePosition,

    /// <summary>
    ///   Tooltip is positioned relative to the bottom right edge of a Control rect. Useful for tooltips
    ///   pertaining to items in a grid-based layout.
    /// </summary>
    ControlBottomRightEdge,
}

/// <summary>
///   Methods of how a tooltip should transition on becoming visible and on being hidden.
/// </summary>
public enum ToolTipTransitioning
{
    /// <summary>
    ///   Immediately display and hide the tooltip without animation.
    /// </summary>
    Immediate,

    /// <summary>
    ///   Use fading to display and hide the tooltip.
    /// </summary>
    Fade,
}

/// <summary>
///   Interface for all custom tooltip Controls. Benefits from being highly-customizable
///   than the default built-in tooltips.
/// </summary>
/// <remarks>
///   <para>
///     NOTE: if the tooltip is simple enough (just a single line of text), it's better to use
///     a Control's HintTooltip property for displaying it as using a custom tooltip for that
///     will just be unnecessarily complicated.
///   </para>
/// </remarks>
public interface ICustomToolTip
{
    /// <summary>
    ///   Used as the human readable name for this tooltip, as opposed to the Node name
    ///   which usually functions as the "InternalName".
    /// </summary>
    string DisplayName { get; set; }

    string Description { get; set; }

    /// <summary>
    ///   Used to delay how long it takes for this tooltip to appear. Set this to zero for no delay.
    /// </summary>
    float DisplayDelay { get; set; }

    /// <summary>
    ///   Where a tooltip should be positioned on display.
    /// </summary>
    ToolTipPositioning Positioning { get; set; }

    /// <summary>
    ///   How a tooltip should transition on becoming visible and on being hidden.
    /// </summary>
    ToolTipTransitioning TransitionType { get; set; }

    bool HideOnMousePress { get; set; }

    /// <summary>
    ///   Control node of this tooltip
    /// </summary>
    Control ToolTipNode { get; }
}
