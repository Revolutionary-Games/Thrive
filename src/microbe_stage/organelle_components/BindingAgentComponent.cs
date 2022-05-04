/// <summary>
///   Used to detect if a binding agent is present
/// </summary>
public class BindingAgentComponent : EmptyOrganelleComponent
{
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
