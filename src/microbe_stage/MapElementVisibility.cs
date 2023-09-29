/// <summary>
///   The visibility of an element in the patch map.
/// </summary>
public enum MapElementVisibility
{
    /// <summary>
    ///   Invisible to the player
    /// </summary>
    Undiscovered = 0,

    /// <summary>
    ///   Visible to the player but details hidden
    /// </summary>
    Unexplored = 1,

    /// <summary>
    ///   Visible to the player and details shown
    /// </summary>
    Explored = 2,
}
