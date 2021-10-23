using Godot;

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

    public void OnAttachToCell(PlacedOrganelle organelle)
    {
    }

    public void OnDetachFromCell(PlacedOrganelle organelle)
    {
    }

    public void Update(float elapsed)
    {
    }

    public void OnShapeParentChanged(Microbe newShapeParent, Vector3 offset, Vector3 masterRotation,
        Vector3 parentRotation)
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
            throw new InvalidRegistryDataException(name, GetType().Name,
                "Storage component capacity must be > 0.0f");
        }
    }
}
