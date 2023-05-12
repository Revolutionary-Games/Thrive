using Godot;

/// <summary>
///   Interface for all tutorial GUI classes
/// </summary>
public interface ITutorialGUI
{
    /// <summary>
    ///   What game state this tutorial GUI is associated with
    /// </summary>
    public MainGameState AssociatedGameState { get; }

    /// <summary>
    ///   Which object receives events from this tutorial
    /// </summary>
    public ITutorialInput? EventReceiver { get; set; }

    /// <summary>
    ///   Used to ignore reporting closing back to whoever is setting the visible properties
    /// </summary>
    public bool IsClosingAutomatically { get; set; }

    /// <summary>
    ///   True when the tutorial selected boxes have been left untouched (on).
    ///   Child classes should by default set this to true.
    /// </summary>
    /// <remarks>
    ///   <para>
    ///     There's a small bug where if the tutorials are turned back on and then displayed, that leaves the checkboxes
    ///     unchecked, when things become visible all the enable tutorials check boxes would need to read this value
    ///     to fix this
    ///   </para>
    /// </remarks>
    public bool TutorialEnabledSelected { get; }

    /// <summary>
    ///   The main GUI node
    /// </summary>
    public Node GUINode { get; }

    /// <summary>
    ///   A button that closes all tutorials was pressed by the user
    /// </summary>
    public void OnClickedCloseAll();

    /// <summary>
    ///   A button for closing specific tutorial was pressed by the user
    /// </summary>
    /// <param name="closedThing">Name of the tutorials that should be closed</param>
    public void OnSpecificCloseClicked(string closedThing);

    /// <summary>
    ///   Value for tutorials being on was toggled by the user, and should be applied when the current tutorial is
    ///   closed
    /// </summary>
    /// <param name="value">Whether tutorials should be on</param>
    public void OnTutorialEnabledValueChanged(bool value);
}
