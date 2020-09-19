using Godot;

/// <summary>
///   Interface for all custom tooltip nodes
/// </summary>
public interface ICustomTooltip
{
    Vector2 Position { get; set; }

    Vector2 Size { get; set; }

    string TooltipName { get; set; }

    string TooltipDescription { get; set; }

    /// <summary>
    ///   Used to delay how long it takes for this tooltip to appear
    /// </summary>
    float DisplayDelay { get; }

    /// <summary>
    ///   If true the tooltip is currently displayed
    /// </summary>
    bool TooltipVisible { get; set; }

    /// <summary>
    ///   Node of this tooltip
    /// </summary>
    Node TooltipNode { get; }
}
