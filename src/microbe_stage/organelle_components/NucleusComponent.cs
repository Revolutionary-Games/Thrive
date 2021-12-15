using Godot;

/// <summary>
///   Literally does nothing anymore. If this isn't used as PlacedOrganelle.HasComponent type
///   This serves no purpose anymore.
/// </summary>
public class NucleusComponent : IOrganelleComponent
{
    public void OnAttachToCell(PlacedOrganelle organelle)
    {
    }

    public void OnDetachFromCell(PlacedOrganelle organelle)
    {
    }

    public void Update(float elapsed)
    {
    }

    public void OnShapeParentChanged(Microbe newShapeParent, Vector2 offset)
    {
    }
}

public class NucleusComponentFactory : IOrganelleComponentFactory
{
    public IOrganelleComponent Create()
    {
        return new NucleusComponent();
    }

    public void Check(string name)
    {
    }
}
