using Newtonsoft.Json;

public class InterplanetaryEnergyComponent : SpaceStructureComponent
{
    public InterplanetaryEnergyComponent(float energy)
    {
        Energy = energy;
    }

    public float Energy { get; }
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
