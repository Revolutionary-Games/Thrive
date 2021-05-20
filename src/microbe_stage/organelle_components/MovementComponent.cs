using Godot;

/// <summary>
///   Flagellum for making cells move faster
/// </summary>
public class MovementComponent : ExternallyPositionedComponent
{
    public float Momentum;
    public float Torque;

    private readonly Compound atp = SimulationParameters.Instance.GetCompound("atp");

    private bool movingTail;
    private Vector3 force;

    private AnimationPlayer animation;

    public MovementComponent(float momentum, float torque)
    {
        Momentum = momentum;
        Torque = torque;
    }

    public static Vector3 GetForceVector(int orientation)
    {
        if (orientation == 0)
        {
            return Constants.VectorDown;
        }
        else if (orientation == 1)
        {
            return Constants.VectorDownLeft;
        }
        else if (orientation == 2)
        {
            return Constants.VectorUpLeft;
        }
        else if (orientation == 3)
        {
            return Constants.VectorUp;
        }
        else if (orientation == 4)
        {
            return Constants.VectorUpRight;
        }
        else
        {
            return Constants.VectorDownRight;
        }
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

    protected override void CustomAttach()
    {
        force = CalculateForce(Momentum);

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

    /// <summary>
    ///   Calculate the vector of the thrust force provided by the flagellum based on its orientation.
    /// </summary>
    private Vector3 CalculateForce(float momentum)
    {
        Vector3 forceVector = GetForceVector(organelle.Orientation);
        forceVector = forceVector * momentum;
        return forceVector;
    }

    private void SetSpeedFactor(float speed)
    {
        if (animation != null)
        {
            animation.PlaybackSpeed = speed;
        }
    }

    // ReSharper disable once UnusedParameter.Local
    /// <summary>
    ///   The final calculated force is multiplied by elapsed before
    ///   applying. So we don't have to do that. But we need to take
    ///   the right amount of atp.
    /// </summary>
    private Vector3 CalculateMovementForce(Microbe microbe, float elapsed)
    {
        // The movementDirection is the player or AI input
        Vector3 direction = microbe.MovementDirection;

        var forceMagnitude = force.Dot(direction);
        if (forceMagnitude <= 0 || direction.LengthSquared() < MathUtils.EPSILON ||
            force.LengthSquared() < MathUtils.EPSILON)
        {
            if (movingTail)
            {
                movingTail = false;

                SetSpeedFactor(0.25f);
            }

            return new Vector3(0, 0, 0);
        }

        var animationSpeed = 2.3f;
        movingTail = true;

        var requiredEnergy = Constants.FLAGELLA_ENERGY_COST * elapsed;

        var availableEnergy = microbe.Compounds.TakeCompound(atp, requiredEnergy);

        if (availableEnergy < requiredEnergy)
        {
            // Not enough energy, scale the force down
            var fraction = availableEnergy / requiredEnergy;

            forceMagnitude *= fraction;

            animationSpeed = 0.25f + (animationSpeed - 0.25f) * fraction;
        }

        float impulseMagnitude = Constants.FLAGELLA_BASE_FORCE * microbe.MovementFactor *
            forceMagnitude / 100.0f;

        // Rotate the 'thrust' based on our orientation
        direction = microbe.Transform.basis.Xform(direction);

        SetSpeedFactor(animationSpeed);

        return direction * impulseMagnitude;
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
