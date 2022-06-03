using Godot;

/// <summary>
///   An organelle component that doesn't do anything
/// </summary>
public abstract class EmptyOrganelleComponent : IOrganelleComponent
{
    public void OnAttachToCell(PlacedOrganelle organelle)
    {
    }

    public void OnDetachFromCell(PlacedOrganelle organelle)
    {
    }

    public void UpdateAsync(float delta)
    {
    }

    public void UpdateSync()
    {
    }

    public void OnShapeParentChanged(Microbe newShapeParent, Vector3 offset)
    {
    }
}
