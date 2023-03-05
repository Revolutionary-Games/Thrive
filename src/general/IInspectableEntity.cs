/// <summary>
///   A game entity the player can inspect (with mouse), for querying information and displayed on GUI
/// </summary>
public interface IInspectableEntity : IGraphicalEntity
{
    public string InspectableName { get; }

    /// <summary>
    ///   Called when a raycast hits this entity.
    /// </summary>
    /// <param name="raycastResult">The raycast data.</param>
    public void OnMouseEnter(RaycastResult raycastResult);

    /// <summary>
    ///   Called when a raycast no longer hits this entity.
    /// </summary>
    /// <param name="raycastResult">The data of the last intersecting raycast.</param>
    public void OnMouseExit(RaycastResult raycastResult);
}
