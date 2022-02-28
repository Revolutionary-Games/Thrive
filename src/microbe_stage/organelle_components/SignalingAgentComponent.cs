using Godot;

public class SignalingAgentComponent : IOrganelleComponent
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

    public void OnShapeParentChanged(Microbe newShapeParent, Vector3 offset)
    {
    }
}

public class SignalingAgentComponentFactory : IOrganelleComponentFactory
{
    public IOrganelleComponent Create()
    {
        return new SignalingAgentComponent();
    }

    public void Check(string name)
    {
    }
}
