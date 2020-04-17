using System;
using Godot;

/// <summary>
///   Flagellum for making cells move faster
/// </summary>
public class MovementComponent : ExternallyPositionedComponent
{
    public float Momentum;
    public float Torque;

    private bool movingTail = false;
    private Vector3 force;

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
        var pos = microbe.Translation;

        var movement = CalculateMovementForce(microbe, elapsed, pos);

        if (movement != new Vector3(0, 0, 0))
            microbe.AddMovementForce(movement);
    }

    protected override void CustomAttach()
    {
        force = CalculateForce(organelle.Position, Momentum);

        // TODO: animation
        // SimpleAnimation moveAnimation("flagellum_move.animation");
        // moveAnimation.Loop = true;
        // // 0.25 is the "idle" animation speed when the flagellum isn't used
        // moveAnimation.SpeedFactor = 0.25f;
        // animated.AddAnimation(moveAnimation);
        // // Don't forget to mark to apply the new animation
        // animated.Marked = true;
    }

    protected override void OnPositionChanged(Quat rotation, float angle,
        Vector3 membraneCoords)
    {
        organelle.OrganelleGraphics.Transform = new Transform(rotation, membraneCoords);
    }

    /// <summary>
    ///   Calculate the momentum of the movement organelle based on
    ///   angle towards middle of cell
    /// </summary>
    private static Vector3 CalculateForce(Hex pos, float momentum)
    {
        Vector3 organelle = Hex.AxialToCartesian(pos);
        Vector3 middle = Hex.AxialToCartesian(new Hex(0, 0));
        var delta = middle - organelle;
        return delta.Normalized() * momentum;
    }

    private void SetSpeedFactor(float speed)
    {
        // TODO: fix flagellum animation
        // if(animated !is null){
        //     if(animated.GetAnimation(0).SpeedFactor != speed){
        //         animated.GetAnimation(0).SpeedFactor = speed;
        //         animated.Marked = true;
        //     }
        // }
    }

    /// <summary>
    ///   The final calculated force is multiplied by elapsed before
    ///   applying. So we don't have to do that. But we need to take
    ///   the right amount of atp.
    /// </summary>
    private Vector3 CalculateMovementForce(Microbe microbe, float elapsed, Vector3 position)
    {
        // The movementDirection is the player or AI input
        Vector3 direction = microbe.MovementDirection;

        var forceMagnitude = this.force.Dot(direction);
        if (forceMagnitude <= 0 || direction.LengthSquared() < MathUtils.EPSILON ||
            this.force.LengthSquared() < MathUtils.EPSILON)
        {
            if (movingTail)
            {
                movingTail = false;

                SetSpeedFactor(0.25f);
            }

            return new Vector3(0, 0, 0);
        }

        // TODO: make only one speedfactor call per update (currently 2 might be made)
        movingTail = true;
        SetSpeedFactor(2.3f);

        var energy = Constants.FLAGELLA_ENERGY_COST * elapsed;

        var availableEnergy = microbe.Compounds.TakeCompound("atp", energy);

        if (availableEnergy <= 0.0f)
        {
            forceMagnitude = Math.Sign(forceMagnitude) * availableEnergy * 20.0f;
            movingTail = false;

            SetSpeedFactor(0.25f);
        }

        float impulseMagnitude = (Constants.FLAGELLA_BASE_FORCE * microbe.MovementFactor *
            forceMagnitude) / 100.0f;

        // Rotate the 'thrust' based on our orientation
        direction = microbe.Transform.basis.Xform(direction);

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
