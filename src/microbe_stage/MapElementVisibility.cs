/// <summary>
///   The visibility of an element in the patch map.
/// </summary>
public enum MapElementVisibility
{
    /// <summary>
    ///   Invisible to the player
    /// </summary>
    Undiscovered,

    /// <summary>
    ///   Visible to the player but details hidden
    /// </summary>
    Unexplored,

    /// <summary>
    ///   Visible to the player and details shown
    /// </summary>
    Explored,
}
