using Godot;

/// <summary>
///   Interface for all custom tooltip nodes
/// </summary>
public interface ICustomToolTip
{
    Vector2 Position { get; set; }

    Vector2 Size { get; set; }

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

    /// <summary>
    ///   Display the tooltip with additional behavior if any
    /// </summary>
    void OnDisplay();

    /// <summary>
    ///   Hide the tooltip with additional behavior if any
    /// </summary>
    void OnHide();
}
