/// <summary>
///   Readonly species with microbe properties on top
/// </summary>
public interface IReadOnlyMicrobeSpecies : IReadOnlySpecies, IReadOnlyCellDefinition
{
    public MicrobeSpecies Clone(bool cloneOrganelles);
}
