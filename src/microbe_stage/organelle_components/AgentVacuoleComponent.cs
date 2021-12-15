using Godot;

/// <summary>
///   Adds toxin shooting capability
/// </summary>
public class AgentVacuoleComponent : IOrganelleComponent
{
    public string Compound;

    public AgentVacuoleComponent(string compound)
    {
        Compound = compound;
    }

    public void OnAttachToCell(PlacedOrganelle organelle)
    {
    }

    public void OnDetachFromCell(PlacedOrganelle organelle)
    {
    }

    public void Update(float elapsed)
    {
    }

    public void OnShapeParentChanged(Microbe newShapeParent, Vector2 offset)
    {
    }
}

public class AgentVacuoleComponentFactory : IOrganelleComponentFactory
{
    public string Compound;
    public string Process;

    public IOrganelleComponent Create()
    {
        return new AgentVacuoleComponent(Compound);
    }

    public void Check(string name)
    {
        if (string.IsNullOrEmpty(Compound))
        {
            throw new InvalidRegistryDataException(name, GetType().Name,
                "Agent compound needs to be set");
        }

        if (string.IsNullOrEmpty(Process))
        {
            throw new InvalidRegistryDataException(name, GetType().Name,
                "Agent process needs to be set");
        }
    }
}
