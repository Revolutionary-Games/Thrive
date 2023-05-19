using Newtonsoft.Json;

public class InterplanetaryEnergyComponent : SpaceStructureComponent
{
    private readonly WorldResource energyResource;

    public InterplanetaryEnergyComponent(float energy)
    {
        Energy = energy;

        energyResource = SimulationParameters.Instance.GetWorldResource("energy");
    }

    public float Energy { get; }

    public override void ProcessSpace(float delta, ISocietyStructureDataAccess dataAccess)
    {
        dataAccess.SocietyResources.Add(energyResource, delta * Energy);
    }
}

public class InterplanetaryEnergyComponentFactory : ISpaceStructureComponentFactory
{
    [JsonProperty]
    public float Amount { get; private set; }

    public SpaceStructureComponent Create()
    {
        return new InterplanetaryEnergyComponent(Amount);
    }

    public void Check(string name)
    {
        if (Amount <= 0)
        {
            throw new InvalidRegistryDataException(name, GetType().Name,
                "Interplanetary energy component must produce energy");
        }
    }
}
