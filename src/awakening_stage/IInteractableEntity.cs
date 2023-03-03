using Godot;

/// <summary>
///   A game entity the player can interact with
/// </summary>
public interface IInteractableEntity : IEntity
{
    public float InteractDistanceOffset { get; }

    public Vector3? ExtraInteractOverlayOffset { get; }
}
