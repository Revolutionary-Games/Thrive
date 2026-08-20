using System;
using Arch.Core;
using Arch.Core.Extensions;
using Components;
using Godot;

public class ActomyosinComponent : IOrganelleComponent
{
    private PlacedOrganelle parentOrganelle = null!;

    private float currentSpeed = 1.0f;
    private float targetSpeed;
    private bool animationDirty = true;

    private float timeSinceRotationSample;
    private Quaternion? previousCellRotation;

    public bool UsesSyncProcess => animationDirty;

    public void OnAttachToCell(PlacedOrganelle organelle)
    {
        parentOrganelle = organelle;

        SetSpeedFactor(Constants.ACTOMYOSIN_DEFAULT_ANIMATION_SPEED);
    }

    public void UpdateAsync(ref OrganelleContainer organelleContainer, ref SpecializationFactor specializationFactor,
        in Entity microbeEntity, IWorldSimulation worldSimulation, float energyCostMultiplier, float delta)
    {
        // Stop animating when being engulfed
        if (microbeEntity.Get<Engulfable>().PhagocytosisStep != PhagocytosisPhase.None)
        {
            SetSpeedFactor(0);
            return;
        }

        // TODO: for cell colonies the animation speed of the cells should probably also take rotation around
        // the colony origin into account
        ref var position = ref microbeEntity.Get<WorldPosition>();

        var currentCellRotation = position.Rotation;

        if (previousCellRotation == null)
        {
            targetSpeed = Constants.ACTOMYOSIN_DEFAULT_ANIMATION_SPEED;
            previousCellRotation = currentCellRotation;
            timeSinceRotationSample = Constants.ACTOMYOSIN_SPEED_SAMPLE_INTERVAL;
            return;
        }

        timeSinceRotationSample += delta;

        // This is way too sensitive if we sample on each process, so we only sample tens of times per second
        if (timeSinceRotationSample < Constants.ACTOMYOSIN_SPEED_SAMPLE_INTERVAL)
            return;

        // Calculate how fast the cell is turning by controlling the animation speed
        var rawRotation = previousCellRotation.Value.AngleTo(currentCellRotation);
        var rotationSpeed = rawRotation * Constants.ACTOMYOSIN_ROTATION_ANIMATION_SPEED_MULTIPLIER;

        ref var control = ref microbeEntity.Get<MicrobeControl>();

        // Add together movement and rotation as actomyosin helps with both for the animation speed
        var rawValue = rotationSpeed + control.MovementDirection.Length() * 2;

        if (rawValue > MathUtils.EPSILON)
        {
            targetSpeed = Math.Clamp(rawValue,
                Constants.ACTOMYOSIN_MIN_ANIMATION_SPEED, Constants.ACTOMYOSIN_MAX_ANIMATION_SPEED);
        }
        else
        {
            targetSpeed = Constants.ACTOMYOSIN_DEFAULT_ANIMATION_SPEED;
        }

        // If not in a colony / colony leader, the actomyosin does nothing
        if (!microbeEntity.Has<MicrobeColony>() && !microbeEntity.Has<MicrobeColonyMember>())
        {
            targetSpeed = Constants.ACTOMYOSIN_DEFAULT_ANIMATION_SPEED;
        }

        SetSpeedFactor(targetSpeed);

        previousCellRotation = currentCellRotation;

        timeSinceRotationSample = 0;
    }

    public void UpdateSync(in Entity microbeEntity, float delta)
    {
        // Skip applying speed if this happens before the organelle graphics are loaded
        if (parentOrganelle.OrganelleAnimation != null)
        {
            parentOrganelle.OrganelleAnimation.SpeedScale = currentSpeed;
            animationDirty = false;
        }
    }

    private void SetSpeedFactor(float speed)
    {
        // We use exact speed values in the code

        // ReSharper disable once CompareOfFloatsByEqualityOperator
        if (speed == currentSpeed)
            return;

        currentSpeed = speed;
        animationDirty = true;
    }
}

public class ActomyosinComponentFactory : IOrganelleComponentFactory
{
    public IOrganelleComponent Create()
    {
        return new ActomyosinComponent();
    }

    public void Check(string name)
    {
    }
}
