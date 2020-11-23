public class BindingAgentComponent : IOrganelleComponent
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
}

public class BindingAgentComponentFactory : IOrganelleComponentFactory
{
    public IOrganelleComponent Create()
    {
        return new BindingAgentComponent();
    }

    public void Check(string name)
    {
    }
}
