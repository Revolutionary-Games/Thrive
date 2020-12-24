using Godot;

/// <summary>
///   Interface for all custom tooltip Control nodes
/// </summary>
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

    /// <summary>
    ///   Node of the tooltip
    /// </summary>
    Node ToolTipNode { get; }

    ToolTipPositioning Positioning { get; }

    bool HideOnMousePress { get; }

    /// <summary>
    ///   Display the tooltip in a customized way (like fade in or scale tweening)
    /// </summary>
    void OnDisplay();

    /// <summary>
    ///   Hide the tooltip
    /// </summary>
    void OnHide();
}

/// <summary>
///   How the tooltip should be positioned on display.
/// </summary>
public enum ToolTipPositioning
{
    /// <summary>
    ///   Tooltip positioned at the last cursor position after entering a tooltipable area.
    /// </summary>
    LastMousePosition,

    /// <summary>
    ///   Tooltip constantly positioned at the same position as the cursor.
    /// </summary>
    FollowMousePosition,
}
