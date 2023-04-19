/// <summary>
///   Access to data that structures in society stage need to function
/// </summary>
public interface ISocietyStructureDataAccess
{
    public IResourceContainer SocietyResources { get; }

    public TechnologyProgress? CurrentlyResearchedTechnology { get; }

    void AddActiveResearchContribution(object researchSource, float researchPoints);
    void RemoveActiveResearchContribution(object researchSource);
}
