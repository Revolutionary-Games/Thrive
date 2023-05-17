public class AscensionComponent : SpaceStructureComponent
{
}

public class AscensionComponentFactory : ISpaceStructureComponentFactory
{
    public SpaceStructureComponent Create()
    {
        return new AscensionComponent();
    }

    public void Check(string name)
    {
    }
}
