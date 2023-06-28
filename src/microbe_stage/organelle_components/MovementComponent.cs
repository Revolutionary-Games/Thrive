using System;
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

    private AnimationPlayer? animation;

    public MovementComponent(float momentum, float torque)
    {
        Momentum = momentum;
        Torque = torque;
    }

    public override void UpdateAsync(float delta)
    {
        // Visual positioning code
        base.UpdateAsync(delta);

        // Movement force
        var microbe = organelle!.ParentMicrobe!;

        if (microbe.PhagocytosisStep != PhagocytosisPhase.None)
        {
            SetSpeedFactor(0);
            return;
        }

        var movement = CalculateMovementForce(microbe, delta);

        if (movement != new Vector3(0, 0, 0))
            microbe.AddMovementForce(movement);
    }

    protected override void CustomAttach()
    {
        if (organelle?.OrganelleGraphics == null)
            throw new InvalidOperationException("Flagellum needs parent organelle to have graphics");

        force = CalculateForce(organelle!.Position, Momentum);

        animation = organelle.OrganelleAnimation;

        if (animation == null)
        {
            GD.PrintErr("MovementComponent's organelle has no animation player set");
        }

        SetSpeedFactor(0.25f);
    }

    protected override bool NeedsUpdateAnyway()
    {
        // The basis of the transform represents the rotation, as long as the rotation is not modified,
        // the organelle needs to be updated.
        // TODO: Calculated rotations should never equal the identity,
        // it should be kept an eye on if it does. The engine for some reason doesnt update THIS basis
        // unless checked with some condition (if or return)
        // SEE: https://github.com/Revolutionary-Games/Thrive/issues/2906
        return organelle!.OrganelleGraphics!.Transform.basis == Transform.Identity.basis;
    }

    protected override void OnPositionChanged(Quat rotation, float angle,
        Vector3 membraneCoords)
    {
        organelle!.OrganelleGraphics!.Transform = new Transform(rotation, membraneCoords);
    }

    /// <summary>
    ///   Calculate the momentum of the movement organelle based on
    ///   angle towards middle of cell
    ///   If the flagella is placed in the microbe's center, hence delta equals 0,
    ///   consider defaultPos as the organelle's "false" position.
    /// </summary>
    private static Vector3 CalculateForce(Hex pos, float momentum)
    {
        Vector3 organellePosition = Hex.AxialToCartesian(pos);
        Vector3 middle = Hex.AxialToCartesian(new Hex(0, 0));
        var delta = middle - organellePosition;
        if (delta == Vector3.Zero)
            delta = DefaultVisualPos;
        return delta.Normalized() * momentum;
    }

    private void SetSpeedFactor(float speed)
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
    private Vector3 CalculateMovementForce(Microbe microbe, float elapsed)
    {
        // The movementDirection is the player or AI input
        Vector3 direction = microbe.MovementDirection;

        // Real force the flagella applied to the colony (considering rotation)
        var realForce = organelle!.RotatedPositionInsideColony(force);
        var forceMagnitude = realForce.Dot(direction);

        if (forceMagnitude <= 0 || direction.LengthSquared() < MathUtils.EPSILON ||
            realForce.LengthSquared() < MathUtils.EPSILON)
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
        if (microbe.Colony?.Master == null)
        {
            direction = microbe.Transform.basis.Quat().Xform(direction);
        }
        else
        {
            direction = microbe.Colony.Master.Transform.basis.Quat().Xform(direction);
        }

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
