public class SignalingAgentComponent : EmptyOrganelleComponent
{
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
