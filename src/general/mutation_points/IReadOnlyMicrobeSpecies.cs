/// <summary>
///   Readonly species with microbe properties on top
/// </summary>
public interface IReadOnlyMicrobeSpecies : IReadOnlySpecies, IReadOnlyCellTypeDefinition
{
    public MicrobeSpecies Clone(bool cloneOrganelles);
}
