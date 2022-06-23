using System;
using Godot;

public class CiliaComponent : ExternallyPositionedComponent
{
    private readonly Compound atp = SimulationParameters.Instance.GetCompound("atp");

    private float currentSpeed = 1.0f;
    private float targetSpeed;

    private float timeSinceRotationSample;
    private Quat? previousCellRotation;

    private AnimationPlayer? animation;

    public override void UpdateAsync(float delta)
    {
        // Visual positioning code
        base.UpdateAsync(delta);

        var currentCellRotation = organelle!.ParentMicrobe!.GlobalTransform.basis.Quat();

        if (previousCellRotation == null)
        {
            targetSpeed = Constants.CILIA_DEFAULT_ANIMATION_SPEED;
            previousCellRotation = currentCellRotation;
            timeSinceRotationSample = Constants.CILIA_ROTATION_SAMPLE_INTERVAL;
            return;
        }

        timeSinceRotationSample += delta;

        // This is way too sensitive if we sample on each process, so we only sample tens of times per second
        if (timeSinceRotationSample < Constants.CILIA_ROTATION_SAMPLE_INTERVAL)
            return;

        // Calculate how fast the cell is turning for controlling the animation speed
        var rawRotation = previousCellRotation.Value.AngleTo(currentCellRotation);
        var rotationSpeed = rawRotation * Constants.CILIA_ROTATION_ANIMATION_SPEED_MULTIPLIER;

        targetSpeed = Mathf.Clamp(rotationSpeed, Constants.CILIA_MIN_ANIMATION_SPEED,
            Constants.CILIA_MAX_ANIMATION_SPEED);

        previousCellRotation = currentCellRotation;

        // Consume extra ATP when rotating (above certain speed
        if (rawRotation > Constants.CILIA_ROTATION_NEEDED_FOR_ATP_COST)
        {
            var cost = Mathf.Clamp(rawRotation * Constants.CILIA_ROTATION_ENERGY_BASE_MULTIPLIER,
                Constants.CILIA_ROTATION_NEEDED_FOR_ATP_COST, Constants.CILIA_ENERGY_COST);

            var requiredEnergy = cost * timeSinceRotationSample;

            var availableEnergy = organelle.ParentMicrobe!.Compounds.TakeCompound(atp, requiredEnergy);

            if (availableEnergy < requiredEnergy)
            {
                // TODO: slow down rotation when we don't have enough ATP to use our cilia
            }
        }

        timeSinceRotationSample = 0;
    }

    public override void UpdateSync()
    {
        base.UpdateSync();

        // ReSharper disable once CompareOfFloatsByEqualityOperator
        if (currentSpeed != targetSpeed)
        {
            // It seems it would be safe to call this in an async way as the MovementComponent does, but it's probably
            // better to do things correctly here as this is newer code...
            SetSpeedFactor(targetSpeed);
        }
    }

    protected override void CustomAttach()
    {
        if (organelle?.OrganelleGraphics == null)
            throw new InvalidOperationException("Cilia needs parent organelle to have graphics");

        animation = organelle.OrganelleAnimation;

        if (animation == null)
        {
            GD.PrintErr("CiliaComponent's organelle has no animation player set");
        }

        SetSpeedFactor(Constants.CILIA_DEFAULT_ANIMATION_SPEED);
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

    private void SetSpeedFactor(float speed)
    {
        currentSpeed = speed;

        if (animation != null)
        {
            animation.PlaybackSpeed = speed;
        }
    }
}

public class CiliaComponentFactory : IOrganelleComponentFactory
{
    public IOrganelleComponent Create()
    {
        return new CiliaComponent();
    }

    public void Check(string name)
    {
    }
}
