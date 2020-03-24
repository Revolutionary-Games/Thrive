using System;

/// <summary>
///   Allows cell to store more stuff
/// </summary>
public class StorageComponent : IOrganelleComponent
{
    public StorageComponent(float capacity)
    {
        Capacity = capacity;
    }

    public float Capacity { get; }

    public void OnAttachToCell()
    {
    }

    public void OnDetachFromCell()
    {
    }

    public void Update(float elapsed)
    {
    }
}

public class StorageComponentFactory : IOrganelleComponentFactory
{
    public float Capacity;

    public IOrganelleComponent Create()
    {
        return new StorageComponent(Capacity);
    }

    public void Check(string name)
    {
        if (Capacity <= 0.0f)
        {
            throw new InvalidRegistryData(name, this.GetType().Name,
                "Storage component capactity must be > 0.0f");
        }
    }
}
