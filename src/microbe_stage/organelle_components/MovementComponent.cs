﻿using Components;
using DefaultEcs;
using Godot;
using ThriveScriptsShared;

/// <summary>
///   Flagellum for making cells move faster. TODO: rename this to FlagellumComponent (this is named like this due to
///   only it being initially being named like this)
/// </summary>
public class MovementComponent : IOrganelleComponent
{
    private readonly float momentum;

    private PlacedOrganelle parentOrganelle = null!;

    private float animationSpeed = 0.25f;
    private bool animationDirty = true;

    private float flagellumLength;

    private bool lastUsed;
    private Vector3 force;

    public MovementComponent(float momentum)
    {
        this.momentum = momentum;
    }

    public bool UsesSyncProcess => animationDirty;

    public void OnAttachToCell(PlacedOrganelle organelle)
    {
        var configuration = organelle.Upgrades?.CustomUpgradeData;

        if (configuration == null)
        {
            flagellumLength = 0;
        }
        else
        {
            if (configuration is FlagellumUpgrades flagellumUpgrades)
                flagellumLength = flagellumUpgrades.LengthFraction;
        }

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
            parentOrganelle.OrganelleAnimation.SpeedScale = animationSpeed;
            animationDirty = false;
        }
    }

    public float UseForMovement(Vector3 wantedMovementDirection, CompoundBag compounds, Quaternion extraColonyRotation,
        bool isBacteria, float delta)
    {
        return CalculateMovementForce(compounds, wantedMovementDirection, extraColonyRotation, isBacteria, delta);
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
            delta = CellPropertiesHelpers.DefaultVisualPos;
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
        Quaternion extraColonyRotation, bool isBacteria, float elapsed)
    {
        // Real force the flagella applied to the colony (considering rotation)
        var realForce = extraColonyRotation * force;

        // TODO: does the direction need to be rotated for the colony member offset here to make sense?
        var forceMagnitude = realForce.Dot(extraColonyRotation * wantedMovementDirection);

        if (forceMagnitude <= 0 || wantedMovementDirection.LengthSquared() < MathUtils.EPSILON ||
            realForce.LengthSquared() < MathUtils.EPSILON)
        {
            SetSpeedFactor(0.25f);
            return 0;
        }

        var newAnimationSpeed = 2.3f;
        lastUsed = true;

        var requiredEnergy = (Constants.FLAGELLA_ENERGY_COST + Constants.FLAGELLA_MAX_UPGRADE_ATP_USAGE
            * flagellumLength) * elapsed;

        var availableEnergy = compounds.TakeCompound(Compound.ATP, requiredEnergy);

        if (availableEnergy < requiredEnergy)
        {
            // Not enough energy, scale the force down
            var fraction = availableEnergy / requiredEnergy;

            forceMagnitude *= fraction;

            newAnimationSpeed = 0.25f + (newAnimationSpeed - 0.25f) * fraction;
        }

        SetSpeedFactor(newAnimationSpeed);

        var baseForce = Constants.FLAGELLA_BASE_FORCE + Constants.FLAGELLA_MAX_UPGRADE_FORCE * flagellumLength;

        if (isBacteria)
        {
            return baseForce * forceMagnitude;
        }

        return baseForce * Constants.EUKARYOTIC_MOVEMENT_FORCE_MULTIPLIER * forceMagnitude;
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

[JSONDynamicTypeAllowed]
public class FlagellumUpgrades : IComponentSpecificUpgrades
{
    public float LengthFraction;

    public FlagellumUpgrades(float lengthFraction)
    {
        LengthFraction = lengthFraction;
    }

    public bool Equals(IComponentSpecificUpgrades? other)
    {
        if (other is not FlagellumUpgrades otherFlagellum)
            return false;

        return otherFlagellum.LengthFraction == LengthFraction;
    }

    public object Clone()
    {
        return new FlagellumUpgrades(LengthFraction);
    }

    public override int GetHashCode()
    {
        return int.RotateRight(LengthFraction.GetHashCode(), 1);
    }
}
