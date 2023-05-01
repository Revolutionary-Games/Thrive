/// <summary>
///   Access to data that structures in society stage need to function
/// </summary>
public interface ISocietyStructureDataAccess
{
    public IResourceContainer SocietyResources { get; }

    public TechnologyProgress? CurrentlyResearchedTechnology { get; }

    public void AddActiveResearchContribution(object researchSource, float researchPoints);
    public void RemoveActiveResearchContribution(object researchSource);
}
