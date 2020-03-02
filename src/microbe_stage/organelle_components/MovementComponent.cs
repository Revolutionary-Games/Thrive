using System;

/// <summary>
///   Flagellum for making cells move faster
/// </summary>
public class MovementComponent : IOrganelleComponent
{
    public float Momentum;
    public float Torque;

    public MovementComponent(float momentum, float torque)
    {
        Momentum = momentum;
        Torque = torque;
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

public class MovementComponentFactory : IOrganelleComponentFactory
{
    public float Momentum;
    public float Torque;

    public IOrganelleComponent Create()
    {
        return new MovementComponent(Momentum, Torque);
    }

    public void Check(string name)
    {
        if (Momentum <= 0.0f)
        {
            throw new InvalidRegistryData(name, this.GetType().Name,
                "Momentum needs to be > 0.0f");
        }

        if (Torque <= 0.0f)
        {
            throw new InvalidRegistryData(name, this.GetType().Name,
                "Torque needs to be > 0.0f");
        }
    }
}
