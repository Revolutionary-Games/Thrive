using Newtonsoft.Json;

/// <summary>
///   Resource storage capacity in a structure (as compared to storage in a cell)
/// </summary>
public class StructureStorageComponent : StructureComponent
{
    public StructureStorageComponent(PlacedStructure owningStructure, float capacity) : base(owningStructure)
    {
        Capacity = capacity;
    }

    public float Capacity { get; }
}

public class StructureStorageComponentFactory : IStructureComponentFactory
{
    [JsonProperty]
    public float Capacity { get; private set; }

    public StructureComponent Create(PlacedStructure owningStructure)
    {
        return new StructureStorageComponent(owningStructure, Capacity);
    }

    public void Check(string name)
    {
        if (Capacity <= 0)
            throw new InvalidRegistryDataException(name, GetType().Name, "Storage component capacity must be > 0.0f");
    }
}
