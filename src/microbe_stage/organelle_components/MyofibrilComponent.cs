public class MyofibrilComponent : EmptyOrganelleComponent
{
}

public class MyofibrilComponentFactory : IOrganelleComponentFactory
{
    public IOrganelleComponent Create()
    {
        return new MyofibrilComponent();
    }

    public void Check(string name)
    {
    }
}
