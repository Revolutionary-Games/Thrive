using Newtonsoft.Json;

/// <summary>
///   Type of a cell in a multicellular species. There can be multiple instances of a cell type placed at once
/// </summary>
public class CellType : ICellProperties
{
    public CellType(OrganelleLayout<OrganelleTemplate> organelles)
    {
        Organelles = organelles;
    }

    public CellType()
    {
        Organelles = new OrganelleLayout<OrganelleTemplate>();
    }

    [JsonProperty]
    public OrganelleLayout<OrganelleTemplate> Organelles { get; private set; }
}
