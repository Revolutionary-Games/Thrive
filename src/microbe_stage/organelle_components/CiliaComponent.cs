using System;
using System.Diagnostics.CodeAnalysis;
using Godot;

[SuppressMessage("Design", "CA1001:Types that own disposable fields should be disposable",
    Justification = "We don't dispose Godot scene-attached objects")]
public class CiliaComponent : ExternallyPositionedComponent
{
    private const string CILIA_PULL_UPGRADE_NAME = "pull";

    private readonly Compound atp = SimulationParameters.Instance.GetCompound("atp");

    private float currentSpeed = 1.0f;
    private float targetSpeed;

    private float timeSinceRotationSample;
    private Quat? previousCellRotation;

    private AnimationPlayer? animation;

    private Area? attractorArea;
    private SphereShape? attractorShape;

    public override void UpdateAsync(float delta)
    {
        // Visual positioning code
        base.UpdateAsync(delta);

        var microbe = organelle!.ParentMicrobe!;

        if (microbe.PhagocytosisStep != PhagocytosisPhase.None)
        {
            targetSpeed = 0;
            return;
        }

        var currentCellRotation = microbe.Transform.basis.Quat();

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

        if (microbe.State == MicrobeState.Engulf && attractorArea != null)
        {
            // We are using cilia pulling, play animation at fixed rate
            targetSpeed = Constants.CILIA_CURRENT_GENERATION_ANIMATION_SPEED;
        }
        else
        {
            targetSpeed = Mathf.Clamp(rotationSpeed, Constants.CILIA_MIN_ANIMATION_SPEED,
                Constants.CILIA_MAX_ANIMATION_SPEED);
        }

        previousCellRotation = currentCellRotation;

        // Consume extra ATP when rotating (above certain speed
        if (rawRotation > Constants.CILIA_ROTATION_NEEDED_FOR_ATP_COST)
        {
            var cost = Mathf.Clamp(rawRotation * Constants.CILIA_ROTATION_ENERGY_BASE_MULTIPLIER,
                Constants.CILIA_ROTATION_NEEDED_FOR_ATP_COST, Constants.CILIA_ENERGY_COST);

            var requiredEnergy = cost * timeSinceRotationSample;

            var availableEnergy = microbe.Compounds.TakeCompound(atp, requiredEnergy);

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

        if (attractorArea != null)
        {
            // Enable cilia pulling force if parent cell is not in engulf mode and is not being engulfed
            var enable = organelle!.ParentMicrobe!.State == MicrobeState.Engulf &&
                organelle.ParentMicrobe.PhagocytosisStep == PhagocytosisPhase.None;

            // The approach of disabling the underlying collision shape or the Area's Monitoring property makes the
            // Area lose its hold over any overlapping bodies once re-enabled. The following should be the proper way
            // to do it without side effects.
            attractorArea.SpaceOverride = enable ? Area.SpaceOverrideEnum.Combine : Area.SpaceOverrideEnum.Disabled;
        }

        if (attractorShape != null)
        {
            // Make the pulling force's radius scales with the organelle's growth value
            attractorShape.Radius = Constants.CILIA_PULLING_FORCE_FIELD_RADIUS +
                (Constants.CILIA_PULLING_FORCE_GROW_STEP * organelle!.GrowthValue);
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

        // Only pulling cilia gets the following physics features
        if (organelle.Upgrades?.UnlockedFeatures.Contains(CILIA_PULL_UPGRADE_NAME) != true)
            return;

        var microbe = organelle.ParentMicrobe!;

        throw new NotImplementedException();

        /*attractorArea = new Area
        {
            GravityPoint = true,
            GravityDistanceScale = Constants.CILIA_PULLING_FORCE_FALLOFF_FACTOR,
            Gravity = Constants.CILIA_PULLING_FORCE,
            CollisionLayer = 0,
            CollisionMask = microbe.CollisionMask,
            Translation = Hex.AxialToCartesian(organelle.Position),
        };

        attractorShape ??= new SphereShape();
        attractorArea.ShapeOwnerAddShape(attractorArea.CreateShapeOwner(attractorShape), attractorShape);
        microbe.AddChild(attractorArea);*/
    }

    protected override void CustomDetach()
    {
        attractorArea?.DetachAndQueueFree();
        attractorArea = null;
        attractorShape = null;
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
