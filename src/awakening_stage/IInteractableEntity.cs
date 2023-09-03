using System.Collections.Generic;
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
    public Vector3? ExtraInteractionCenterOffset { get; }

    /// <summary>
    ///   If not null, the interaction popup will have this extra text in it. For now this doesn't support rich text.
    /// </summary>
    public string? ExtraInteractionPopupDescription { get; }

    /// <summary>
    ///   Set to true when this interactable is disabled and nothing should be able to interact with this
    /// </summary>
    public bool InteractionDisabled { get; set; }

    // Interaction settings

    public bool CanBeCarried { get; }

    // TODO: add some kind of weight or size limit for carrying to limit how much stuff a creature can carry based
    // on its strength

    /// <summary>
    ///   Returns the harvesting data for this entity if this can be harvested
    /// </summary>
    /// <returns>Null if can't be harvested or the harvesting related info if can be</returns>
    public IHarvestAction? GetHarvestingInfo();

    /// <summary>
    ///   Returns custom actions supported by this entity
    /// </summary>
    /// <returns>
    ///   List of tuples (or null if nothing is supported) where the tuple contains the type and then an alternative
    ///   text to show to the user, if the action is disabled. So if the string is not null, then the action must be
    ///   handled as not available.
    /// </returns>
    public IEnumerable<(InteractionType Type, string? DisabledAlternativeText)>? GetExtraAvailableActions();

    /// <summary>
    ///   Performs an action returned by <see cref="GetExtraAvailableActions"/>
    /// </summary>
    /// <param name="interactionType">The type of interaction to perform</param>
    /// <returns>True when the action succeeds, false if it failed for some reason</returns>
    public bool PerformExtraAction(InteractionType interactionType);
}

public static class InteractableEntityHelpers
{
    /// <summary>
    ///   Rotated to world space interaction offset. This is most of the time more useful than the raw value
    /// </summary>
    /// <param name="entity">The entity to get the interaction offset for</param>
    /// <returns>The adjusted offset or null if it doesn't exist for the entity</returns>
    public static Vector3? RotatedExtraInteractionOffset(this IInteractableEntity entity)
    {
        var offset = entity.ExtraInteractionCenterOffset;

        if (offset == null)
            return null;

        return RotateExtraInteractionOffset(offset.Value, entity.EntityNode.GlobalTransform.basis);
    }

    /// <summary>
    ///   Corrects an interaction offset for the entity's actual rotation
    /// </summary>
    /// <param name="offset">Raw offset value</param>
    /// <param name="rotation">Rotation of the entity</param>
    /// <returns>The corrected offset</returns>
    public static Vector3 RotateExtraInteractionOffset(Vector3 offset, Basis rotation)
    {
        return rotation.Xform(offset);
    }
}
