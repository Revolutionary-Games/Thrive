/// <summary>
///   Factory interface that are contained in <see cref="StructureDefinition"/> and used to instantiate the actual
///   components for <see cref="PlacedStructure"/>. The architecture here is modeled after
///   <see cref="IOrganelleComponentFactory"/>.
/// </summary>
public interface IStructureComponentFactory
{
    public StructureComponent Create(PlacedStructure owningStructure);
    public void Check(string name);
}
