using Godot;

/// <summary>
///   Base interface that all organelle components need to implement
/// </summary>
public interface IOrganelleComponent
{
    void OnAttachToCell(PlacedOrganelle organelle);
    void OnDetachFromCell(PlacedOrganelle organelle);
    void Update(float elapsed);
    void OnShapeParentChanged(Microbe newShapeParent, Vector3 offset, Vector3 masterRotation, Vector3 parentRotation);
}
