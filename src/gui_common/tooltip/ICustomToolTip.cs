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
    ///   Used to delay how long it takes for this tooltip to appear
    /// </summary>
    float DisplayDelay { get; set; }

    /// <summary>
    ///   If true tooltip is shown
    /// </summary>
    bool ToolTipVisible { get; set; }

    /// <summary>
    ///   Node of this tooltip
    /// </summary>
    Node ToolTipNode { get; }

    /// <summary>
    ///   Displays tooltip with additional behavior
    /// </summary>
    void OnDisplay();

    /// <summary>
    ///   Hides tooltip with additional behavior
    /// </summary>
    void OnHide();
}
