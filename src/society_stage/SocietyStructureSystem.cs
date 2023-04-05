using Godot;
using Newtonsoft.Json;

/// <summary>
///   Handles structure update logic in the society stage
/// </summary>
public class SocietyStructureSystem
{
    private Node worldRoot;

    [JsonProperty]
    private float elapsed = 1;

    public SocietyStructureSystem(Node root)
    {
        worldRoot = root;
    }

    /// <summary>
    ///   The total amount of storage in all structures. Updated only once <see cref="Process"/> has been called
    /// </summary>
    [JsonIgnore]
    public float CachedTotalStorage { get; private set; }

    /// <summary>
    ///   Initializes this system. Bit of a superfluous one but this mainly exists as placeholder for
    ///   <see cref="SocietyStage"/> class to have a placeholder where systems are initialized.
    /// </summary>
    public void Init()
    {
    }

    public void Process(float delta, ISocietyStructureDataAccess societyData)
    {
        elapsed += delta;

        if (elapsed < Constants.SOCIETY_STAGE_BUILDING_PROCESS_INTERVAL)
            return;

        float storage = 0;

        foreach (var structure in worldRoot.GetChildrenToProcess<PlacedStructure>(Constants.STRUCTURE_ENTITY_GROUP))
        {
            var storageComponent = structure.GetComponent<StructureStorageComponent>();
            if (storageComponent != null)
                storage += storageComponent.Capacity;

            structure.ProcessSociety(elapsed, societyData);
        }

        elapsed = 0;
        CachedTotalStorage = storage;
    }

    /// <summary>
    ///   Immediately calculates total storage
    /// </summary>
    /// <returns>The total storage</returns>
    public float CalculateTotalStorage()
    {
        float storage = 0;

        foreach (var structure in worldRoot.GetChildrenToProcess<PlacedStructure>(Constants.STRUCTURE_ENTITY_GROUP))
        {
            var storageComponent = structure.GetComponent<StructureStorageComponent>();
            if (storageComponent != null)
                storage += storageComponent.Capacity;
        }

        CachedTotalStorage = storage;
        return storage;
    }
}
