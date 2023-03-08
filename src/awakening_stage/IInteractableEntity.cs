using Godot;

/// <summary>
///   A game entity the player can interact with
/// </summary>
/// <remarks>
///   <para>
///     This requires <see cref="IInventoryItem"/> as for now all interactables are assumed to be visible in the
///     inventory screen. If that is not the case in the future some different interface splitting will be needed.
///   </para>
/// </remarks>
public interface IInteractableEntity : IEntity, IPlayerReadableName, IInventoryItem
{
    /// <summary>
    ///   Offset added to the distance at which it is possible to interact with this
    /// </summary>
    public float InteractDistanceOffset { get; }

    /// <summary>
    ///   If not null this is added to the world position of this interactable when considering where the point to
    ///   interact with is located
    /// </summary>
    public Vector3? ExtraInteractOverlayOffset { get; }

    /// <summary>
    ///   Set to true when this interactable is disabled and nothing should be able to interact with this
    /// </summary>
    public bool InteractionDisabled { get; set; }

    // Interaction settings

    public bool CanBeCarried { get; }

    // TODO: add some kind of weight or size limit for carrying to limit how much stuff a creature can carry based
    // on its strength
}
