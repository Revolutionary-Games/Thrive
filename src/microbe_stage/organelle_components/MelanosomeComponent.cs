using Godot;

public class MelanosomeComponent : IOrganelleComponent
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

public class MelanosomeComponentFactory : IOrganelleComponentFactory
{
    public IOrganelleComponent Create()
    {
        return new MelanosomeComponent();
    }

    public void Check(string name)
    {
    }
}
