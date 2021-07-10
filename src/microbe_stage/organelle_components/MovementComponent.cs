using Godot;

/// <summary>
///   Organelles for making cells move faster
/// </summary>
public abstract class MovementComponent : ExternallyPositionedComponent
{
    public float Momentum;
    public float Torque;

    protected readonly Compound atp = SimulationParameters.Instance.GetCompound("atp");

    protected Vector3 force;

    protected AnimationPlayer animation;

    public MovementComponent(float momentum, float torque)
    {
        Momentum = momentum;
        Torque = torque;
    }

    public override void Update(float elapsed)
    {
        // Visual positioning code
        base.Update(elapsed);

        // Movement force
        var microbe = organelle.ParentMicrobe;

        var movement = CalculateMovementForce(microbe, elapsed);

        if (movement != new Vector3(0, 0, 0))
            microbe.AddMovementForce(movement);
    }

    /// <summary>
    ///   Calculate the momentum of the movement organelle based on
    ///   angle towards middle of cell
    /// </summary>
    protected static Vector3 CalculateForce(Hex pos, float momentum)
    {
        Vector3 organelle = Hex.AxialToCartesian(pos);
        Vector3 middle = Hex.AxialToCartesian(new Hex(0, 0));
        var delta = middle - organelle;
        return delta.Normalized() * momentum;
    }

    protected override void CustomAttach()
    {
        force = CalculateForce(organelle.Position, Momentum);

        animation = organelle.OrganelleAnimation;

        if (animation == null)
        {
            GD.PrintErr("MovementComponent's organelle has no animation player set");
        }

        SetSpeedFactor(0.25f);
    }

    protected override void OnPositionChanged(Quat rotation, float angle,
        Vector3 membraneCoords)
    {
        organelle.OrganelleGraphics.Transform = new Transform(rotation, membraneCoords);
    }

    protected void SetSpeedFactor(float speed)
    {
        if (animation != null)
        {
            animation.PlaybackSpeed = speed;
        }
    }

    /// <summary>
    ///   The final calculated force is multiplied by elapsed before
    ///   applying. So we don't have to do that. But we need to take
    ///   the right amount of atp.
    /// </summary>
    protected abstract Vector3 CalculateMovementForce(Microbe microbe, float elapsed);
}

public abstract class MovementComponentFactory : IOrganelleComponentFactory
{
    public float Momentum;
    public float Torque;

    public abstract IOrganelleComponent Create();

    public void Check(string name)
    {
        if (Momentum <= 0.0f)
        {
            throw new InvalidRegistryDataException(name, GetType().Name,
                "Momentum needs to be > 0.0f");
        }

        if (Torque <= 0.0f)
        {
            throw new InvalidRegistryDataException(name, GetType().Name,
                "Torque needs to be > 0.0f");
        }
    }
}
