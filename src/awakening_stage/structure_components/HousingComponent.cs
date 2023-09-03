using Newtonsoft.Json;

/// <summary>
///   Provides living space
/// </summary>
public class HousingComponent : StructureComponent
{
    public HousingComponent(PlacedStructure owningStructure, int space) : base(owningStructure)
    {
        Space = space;
    }

    public int Space { get; }
}

public class HousingComponentFactory : IStructureComponentFactory
{
    [JsonProperty]
    public int Space { get; private set; } = 1;

    public StructureComponent Create(PlacedStructure owningStructure)
    {
        return new HousingComponent(owningStructure, Space);
    }

    public void Check(string name)
    {
        if (Space <= 0)
            throw new InvalidRegistryDataException(name, GetType().Name, "Housing component space must be >= 1");
    }
}
