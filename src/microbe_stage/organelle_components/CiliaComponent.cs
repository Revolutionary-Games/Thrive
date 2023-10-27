using System;
using Components;
using DefaultEcs;
using Godot;

public class CiliaComponent : IOrganelleComponent
{
    private const string CILIA_PULL_UPGRADE_NAME = "pull";

    private readonly Compound atp = SimulationParameters.Instance.GetCompound("atp");

    private PlacedOrganelle parentOrganelle = null!;

    private float currentSpeed = 1.0f;
    private float targetSpeed;
    private bool animationDirty = true;

    private float timeSinceRotationSample;
    private Quat? previousCellRotation;

    public bool UsesSyncProcess => animationDirty;

    public void OnAttachToCell(PlacedOrganelle organelle)
    {
        parentOrganelle = organelle;

        SetSpeedFactor(Constants.CILIA_DEFAULT_ANIMATION_SPEED);

        // Only pulling cilia gets the following physics features
        if (organelle.Upgrades?.UnlockedFeatures.Contains(CILIA_PULL_UPGRADE_NAME) != true)
            return;

        throw new NotImplementedException();

        /*
         these were fields:
           private Area? attractorArea;
           private SphereShape? attractorShape;

         attractorArea = new Area
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

        // TODO: also the code for detach (destroy of the placed organelle) was the following:
        /*
                attractorArea?.DetachAndQueueFree();
                attractorArea = null;
                attractorShape = null;
        */
    }

    public void UpdateAsync(ref OrganelleContainer organelleContainer, in Entity microbeEntity, float delta)
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
            targetSpeed = Constants.CILIA_DEFAULT_ANIMATION_SPEED;
            previousCellRotation = currentCellRotation;
            timeSinceRotationSample = Constants.CILIA_ROTATION_SAMPLE_INTERVAL;
            return;
        }

        timeSinceRotationSample += delta;

        // This is way too sensitive if we sample on each process, so we only sample tens of times per second
        if (timeSinceRotationSample < Constants.CILIA_ROTATION_SAMPLE_INTERVAL)
            return;

        // ref var control = ref microbeEntity.Get<MicrobeControl>();

        // Calculate how fast the cell is turning for controlling the animation speed
        var rawRotation = previousCellRotation.Value.AngleTo(currentCellRotation);
        var rotationSpeed = rawRotation * Constants.CILIA_ROTATION_ANIMATION_SPEED_MULTIPLIER;

        // TODO: pulling cilia reimplementation
        // if (control.State == MicrobeState.Engulf && attractorArea != null)
        // {
        //     // We are using cilia pulling, play animation at fixed rate
        //     targetSpeed = Constants.CILIA_CURRENT_GENERATION_ANIMATION_SPEED;
        // }
        // else
        // {
        targetSpeed = Mathf.Clamp(rotationSpeed, Constants.CILIA_MIN_ANIMATION_SPEED,
            Constants.CILIA_MAX_ANIMATION_SPEED);

        // }

        SetSpeedFactor(targetSpeed);

        previousCellRotation = currentCellRotation;

        // Consume extra ATP when rotating (above certain speed)
        // TODO: would it make more sense
        if (rawRotation > Constants.CILIA_ROTATION_NEEDED_FOR_ATP_COST)
        {
            var cost = Mathf.Clamp(rawRotation * Constants.CILIA_ROTATION_ENERGY_BASE_MULTIPLIER,
                Constants.CILIA_ROTATION_NEEDED_FOR_ATP_COST, Constants.CILIA_ENERGY_COST);

            var requiredEnergy = cost * timeSinceRotationSample;

            var compounds = microbeEntity.Get<CompoundStorage>().Compounds;

            var availableEnergy = compounds.TakeCompound(atp, requiredEnergy);

            if (availableEnergy < requiredEnergy)
            {
                // TODO: slow down rotation when we don't have enough ATP to use our cilia
            }
        }

        timeSinceRotationSample = 0;
    }

    public void UpdateSync(in Entity microbeEntity, float delta)
    {
        // Skip applying speed if this happens before the organelle graphics are loaded
        if (parentOrganelle.OrganelleAnimation != null)
        {
            parentOrganelle.OrganelleAnimation.PlaybackSpeed = currentSpeed;
            animationDirty = false;
        }

        // TODO: pull upgrade handling (note this might need to set animation dirty every now and then to make sure
        // this gets re-run). Also if this needs access to different organelle data, this needs to mark those in the
        // tick system
        /*if (attractorArea != null)
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
        }*/
    }

    private void SetSpeedFactor(float speed)
    {
        // We use exact speed values in the code
        // ReSharper disable once CompareOfFloatsByEqualityOperator
        if (speed != currentSpeed)
            return;

        currentSpeed = speed;
        animationDirty = true;
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
