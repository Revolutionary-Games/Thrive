/// <summary>
///   A game entity the player can inspect (with mouse), for querying information and displayed on GUI
/// </summary>
/// <remarks>
///   <para>
///     NOTE: Entity must have collision shapes thus be a collision object to be detected.
///   </para>
/// </remarks>
public interface IInspectableEntity : IEntity, IPlayerReadableName
{
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
