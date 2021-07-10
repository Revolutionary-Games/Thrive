using Godot;

/// <summary>
///   How a tooltip should be positioned on display.
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
}

/// <summary>
///   Interface for all custom tooltip Control nodes. Benefits from being highly-customizable
///   than the default built-in tooltips.
/// </summary>
/// <remarks>
///   <para>
///     NOTE: if the tooltip is simple enough (just a single line of text), it's better to use
///     a Control's HintTooltip property for displaying it as using a custom tooltip will just
///     be unnecessarily complicated.
///   </para>
/// </remarks>
public interface ICustomToolTip
{
    Vector2 Position { get; set; }

    Vector2 Size { get; set; }

    /// <summary>
    ///   Used as the human readable name for the tooltip, as opposed to the Node name
    ///   which usually functions as the "InternalName".
    /// </summary>
    string DisplayName { get; set; }

    string Description { get; set; }

    /// <summary>
    ///   Used to delay how long it takes for the tooltip to appear
    /// </summary>
    float DisplayDelay { get; set; }

    /// <summary>
    ///   If true the tooltip is shown
    /// </summary>
    bool ToolTipVisible { get; set; }

    ToolTipPositioning Positioning { get; set; }

    bool HideOnMousePress { get; set; }

    /// <summary>
    ///   Node of the tooltip
    /// </summary>
    Node ToolTipNode { get; }

    /// <summary>
    ///   Display the tooltip in a customized way (like fade in or scale tweening)
    /// </summary>
    void OnDisplay();

    /// <summary>
    ///   Hide the tooltip
    /// </summary>
    void OnHide();
}
