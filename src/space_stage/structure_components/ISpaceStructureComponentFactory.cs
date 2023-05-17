/// <summary>
///   Space structure version of this, see: <see cref="IStructureComponentFactory"/>
/// </summary>
public interface ISpaceStructureComponentFactory
{
    public SpaceStructureComponent Create();
    public void Check(string name);
}
