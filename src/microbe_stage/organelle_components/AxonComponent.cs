using Godot;

public class AxonComponent : ExternallyPositionedComponent
{
    protected override void OnPositionChanged(Quat rotation, float angle, Vector3 membraneCoords)
    {
        organelle!.OrganelleGraphics!.Transform = new Transform(rotation, membraneCoords);
    }
}

public class AxonComponentFactory : IOrganelleComponentFactory
{
    public IOrganelleComponent Create()
    {
        return new AxonComponent();
    }

    public void Check(string name)
    {
    }
}
