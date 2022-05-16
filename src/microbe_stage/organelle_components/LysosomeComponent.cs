public class LysosomeComponent : EmptyOrganelleComponent
{
    // TODO: Animate lysosomes sticking onto phagosomes (if possible)
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
