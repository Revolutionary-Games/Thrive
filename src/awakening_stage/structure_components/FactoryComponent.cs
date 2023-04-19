/// <summary>
///   Structure is really good at building stuff (it is a factory)
/// </summary>
public class FactoryComponent : StructureComponent
{
    public FactoryComponent(PlacedStructure owningStructure) : base(owningStructure)
    {
    }
}

public class FactoryComponentFactory : IStructureComponentFactory
{
    public StructureComponent Create(PlacedStructure owningStructure)
    {
        return new FactoryComponent(owningStructure);
    }

    public void Check(string name)
    {
    }
}
