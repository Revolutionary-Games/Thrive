using Godot;

/// <summary>
///   Flagellum for making cells move faster
/// </summary>
public class FlagellumComponent : MovementComponent
{
    private bool movingTail;

    public FlagellumComponent(float momentum, float torque) : base(momentum, torque)
    {
    }

    protected override Vector3 CalculateMovementForce(Microbe microbe, float elapsed)
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

public abstract class FlagellumComponentFactory : MovementComponentFactory
{
    public override IOrganelleComponent Create()
    {
        return new FlagellumComponent(Momentum, Torque);
    }
}
