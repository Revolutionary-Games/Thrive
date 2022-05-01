/// <summary>
///   Literally does nothing anymore. If this isn't used as PlacedOrganelle.HasComponent type
///   This serves no purpose anymore.
/// </summary>
public class NucleusComponent : EmptyOrganelleComponent
{
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
