using Components;
using DefaultEcs;
using Godot;

/// <summary>
///   Flagellum for making cells move faster. TODO: rename this to FlagellumComponent (this is named like this due to
///   only it being initially being named like this)
/// </summary>
public class MovementComponent : IOrganelleComponent
{
    private readonly Compound atp = SimulationParameters.Instance.GetCompound("atp");

    private readonly float momentum;

    private PlacedOrganelle parentOrganelle = null!;

    private float animationSpeed = 0.25f;
    private bool animationDirty = true;

    private bool lastUsed;
    private Vector3 force;

    public MovementComponent(float momentum)
    {
        this.momentum = momentum;
    }

    public bool UsesSyncProcess => animationDirty;

    public void OnAttachToCell(PlacedOrganelle organelle)
    {
        // No longer can check for animation here as the organelle graphics are created later than this is attached to
        // a cell
        parentOrganelle = organelle;

        force = CalculateForce(organelle.Position, momentum);
    }

    public void UpdateAsync(ref OrganelleContainer organelleContainer, in Entity microbeEntity,
        IWorldSimulation worldSimulation, float delta)
    {
        // Stop animating when being engulfed
        if (microbeEntity.Get<Engulfable>().PhagocytosisStep != PhagocytosisPhase.None)
        {
            SetSpeedFactor(0);
            return;
        }

        // Slow down animation when not used for movement
        if (!lastUsed)
        {
            SetSpeedFactor(0.25f);
        }

        lastUsed = false;
    }

    public void UpdateSync(in Entity microbeEntity, float delta)
    {
        // Skip applying speed if this happens before the organelle graphics are loaded
        if (parentOrganelle.OrganelleAnimation != null)
        {
            parentOrganelle.OrganelleAnimation.PlaybackSpeed = animationSpeed;
            animationDirty = false;
        }
    }

    public float UseForMovement(Vector3 wantedMovementDirection, CompoundBag compounds, Quat extraColonyRotation,
        float delta)
    {
        return CalculateMovementForce(compounds, wantedMovementDirection, extraColonyRotation, delta);
    }

    /// <summary>
    ///   Calculate the momentum of the movement organelle based on angle towards middle of cell.
    ///   If the flagella is placed in the microbe's center, hence delta equals 0, consider defaultPos as the
    ///   organelle's "false" position.
    /// </summary>
    private static Vector3 CalculateForce(Hex pos, float momentum)
    {
        Vector3 organellePosition = Hex.AxialToCartesian(pos);
        Vector3 middle = Hex.AxialToCartesian(new Hex(0, 0));
        var delta = middle - organellePosition;
        if (delta == Vector3.Zero)
            delta = Components.CellPropertiesHelpers.DefaultVisualPos;
        return delta.Normalized() * momentum;
    }

    private void SetSpeedFactor(float speed)
    {
        // We use exact values set in the code
        // ReSharper disable once CompareOfFloatsByEqualityOperator
        if (animationSpeed == speed)
            return;

        animationSpeed = speed;
        animationDirty = true;
    }

    /// <summary>
    ///   The final calculated force is multiplied by elapsed before applying. So we don't have to do that.
    ///   But we need to take the right amount of atp.
    /// </summary>
    /// <remarks>
    ///   <para>
    ///     The movementDirection is the player or AI input. It is in non-rotated cell oriented coordinates
    ///   </para>
    /// </remarks>
    private float CalculateMovementForce(CompoundBag compounds, Vector3 wantedMovementDirection,
        Quat extraColonyRotation, float elapsed)
    {
        // Real force the flagella applied to the colony (considering rotation)
        var realForce = extraColonyRotation.Xform(force);

        // TODO: does the direction need to be rotated for the colony member offset here to make sense?
        var forceMagnitude = realForce.Dot(extraColonyRotation.Xform(wantedMovementDirection));

        if (forceMagnitude <= 0 || wantedMovementDirection.LengthSquared() < MathUtils.EPSILON ||
            realForce.LengthSquared() < MathUtils.EPSILON)
        {
            SetSpeedFactor(0.25f);
            return 0;
        }

        var newAnimationSpeed = 2.3f;
        lastUsed = true;

        var requiredEnergy = Constants.FLAGELLA_ENERGY_COST * elapsed;

        var availableEnergy = compounds.TakeCompound(atp, requiredEnergy);

        if (availableEnergy < requiredEnergy)
        {
            // Not enough energy, scale the force down
            var fraction = availableEnergy / requiredEnergy;

            forceMagnitude *= fraction;

            newAnimationSpeed = 0.25f + (newAnimationSpeed - 0.25f) * fraction;
        }

        SetSpeedFactor(newAnimationSpeed);

        // TODO: adjust the flagella force for the new physics engine
        return Constants.FLAGELLA_BASE_FORCE * forceMagnitude;
    }
}

public class MovementComponentFactory : IOrganelleComponentFactory
{
    public float Momentum;

    public IOrganelleComponent Create()
    {
        return new MovementComponent(Momentum);
    }

    public void Check(string name)
    {
        if (Momentum <= 0.0f)
        {
            throw new InvalidRegistryDataException(name, GetType().Name,
                "Momentum needs to be > 0.0f");
        }
    }
}
