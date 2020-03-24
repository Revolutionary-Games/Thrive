using System;

/// <summary>
///   Just adds some nucleus extra graphics
/// </summary>
public class NucleusComponent : IOrganelleComponent
{
    public NucleusComponent()
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
    }
}

public class NucleusComponentFactory : IOrganelleComponentFactory
{
    public IOrganelleComponent Create()
    {
        return new NucleusComponent();
    }

    public void Check(string name)
    {
    }
}
