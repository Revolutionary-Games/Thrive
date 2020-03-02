using System;

/// <summary>
///   Adds a stabby thing to the cell, positioned similarly to the flagellum
/// </summary>
public class PilusComponent : IOrganelleComponent
{
    public PilusComponent()
    {
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

public class PilusComponentFactory : IOrganelleComponentFactory
{
    public IOrganelleComponent Create()
    {
        return new PilusComponent();
    }

    public void Check(string name)
    {
    }
}
