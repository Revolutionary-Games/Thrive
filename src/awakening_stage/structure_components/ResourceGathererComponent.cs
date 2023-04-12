using Newtonsoft.Json;

/// <summary>
///   Base type for all resource gathering type components
/// </summary>
public abstract class ResourceGathererComponent : StructureComponent
{
    protected ResourceGathererComponent(PlacedStructure owningStructure) : base(owningStructure)
    {
    }

    public override void ProcessSociety(float delta, ISocietyStructureDataAccess dataAccess)
    {
        GatherResources(dataAccess.SocietyResources, delta);
    }

    protected abstract void GatherResources(IResourceContainer resourceReceiver, float delta);
}

/// <summary>
///   Simple single type resource gatherer with a speed
/// </summary>
public class SimpleResourceGatherer : ResourceGathererComponent
{
    private readonly WorldResource resource;
    private readonly float amount;

    public SimpleResourceGatherer(PlacedStructure owningStructure, WorldResource resource, float amount) : base(
        owningStructure)
    {
        this.resource = resource;
        this.amount = amount;
    }

    protected override void GatherResources(IResourceContainer resourceReceiver, float delta)
    {
        resourceReceiver.Add(resource, amount * delta);
    }
}

public abstract class SimpleGathererFactoryBase : IStructureComponentFactory
{
    /// <summary>
    ///   How fast the resource is gathered
    /// </summary>
    [JsonProperty]
    public float Amount { get; private set; } = 1;

    protected abstract string ResourceName { get; }

    public StructureComponent Create(PlacedStructure owningStructure)
    {
        return new SimpleResourceGatherer(owningStructure, GetResource(), Amount);
    }

    public void Check(string name)
    {
        if (Amount <= 0)
            throw new InvalidRegistryDataException(name, GetType().Name, "Gatherer component amount must be > 0.0f");

        // Ensure the resource type exists
        GetResource();
    }

    protected WorldResource GetResource()
    {
        return SimulationParameters.Instance.GetWorldResource(ResourceName);
    }
}

public class WoodGathererFactory : SimpleGathererFactoryBase
{
    protected override string ResourceName => "wood";
}

public class RockGathererFactory : SimpleGathererFactoryBase
{
    protected override string ResourceName => "rock";
}

public class FoodGathererFactory : SimpleGathererFactoryBase
{
    protected override string ResourceName => "food";
}
