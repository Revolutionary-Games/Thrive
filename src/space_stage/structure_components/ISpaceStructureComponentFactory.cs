/// <summary>
///   Space structure version of this, see: <see cref="IStructureComponentFactory"/>
/// </summary>
public interface ISpaceStructureComponentFactory
{
    public SpaceStructureComponent Create(PlacedSpaceStructure owningStructure);
    public void Check(string name);
}
