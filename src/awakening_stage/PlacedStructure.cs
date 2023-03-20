using Godot;

public class PlacedStructure : Spatial, IInteractableEntity
{
    public bool Completed { get; private set; }

    public override void _Ready()
    {
    }
}
