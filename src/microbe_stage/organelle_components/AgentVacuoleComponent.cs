using System;

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

    public void OnAttachToCell()
    {
        throw new NotImplementedException();
    }

    public void OnDetachFromCell()
    {
        throw new NotImplementedException();
    }

    public void Update(float elapsed)
    {
        throw new NotImplementedException();
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
        if (Compound == string.Empty)
        {
            throw new InvalidRegistryData(name, this.GetType().Name,
                "Agent compound needs to be set");
        }

        if (Process == string.Empty)
        {
            throw new InvalidRegistryData(name, this.GetType().Name,
                "Agent process needs to be set");
        }
    }
}
