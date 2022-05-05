public class LysosomeComponent : EmptyOrganelleComponent
{
}

public class LysosomeComponentFactory : IOrganelleComponentFactory
{
    public IOrganelleComponent Create()
    {
        return new LysosomeComponent();
    }

    public void Check(string name)
    {
    }
}
