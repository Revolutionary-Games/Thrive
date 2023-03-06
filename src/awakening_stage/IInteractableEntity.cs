using Godot;

/// <summary>
///   A game entity the player can interact with
/// </summary>
public interface IInteractableEntity : IEntity, IPlayerReadableName
{
    public float InteractDistanceOffset { get; }

    public Vector3? ExtraInteractOverlayOffset { get; }

    // Interaction settings

    public bool CanBeCarried { get; }

    // TODO: add some kind of weight or size limit for carrying to limit how much stuff a creature can carry based
    // on its strength
}
