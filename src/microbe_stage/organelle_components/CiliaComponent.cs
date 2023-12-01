using System;
using System.Collections.Generic;
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

    private bool usesPullingCilia;
    private int ciliaPullCount;

    private float timeSinceRotationSample;
    private Quat? previousCellRotation;

    private PullingCiliaData? sharedPullData;

    public bool UsesSyncProcess => animationDirty;

    public void OnAttachToCell(PlacedOrganelle organelle)
    {
        parentOrganelle = organelle;

        SetSpeedFactor(Constants.CILIA_DEFAULT_ANIMATION_SPEED);

        // Only pulling cilia gets the following physics features
        if (organelle.Upgrades?.UnlockedFeatures.Contains(CILIA_PULL_UPGRADE_NAME) != true)
            return;

        usesPullingCilia = true;
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

        // Calculate how fast the cell is turning for controlling the animation speed
        var rawRotation = previousCellRotation.Value.AngleTo(currentCellRotation);
        var rotationSpeed = rawRotation * Constants.CILIA_ROTATION_ANIMATION_SPEED_MULTIPLIER;

        ref var control = ref microbeEntity.Get<MicrobeControl>();
        if (control.State == MicrobeState.Engulf && usesPullingCilia)
        {
            // We are using cilia pulling, play animation at fixed rate
            targetSpeed = Constants.CILIA_CURRENT_GENERATION_ANIMATION_SPEED;

            // Update pulling cilia state and logic. This is fine to be rate limited here as we take that into
            // account in the impulse size calculation.
            UpdatePullingCilia(ref organelleContainer, microbeEntity, worldSimulation, timeSinceRotationSample);
        }
        else
        {
            targetSpeed = Mathf.Clamp(rotationSpeed, Constants.CILIA_MIN_ANIMATION_SPEED,
                Constants.CILIA_MAX_ANIMATION_SPEED);
        }

        SetSpeedFactor(targetSpeed);

        previousCellRotation = currentCellRotation;

        // Consume extra ATP when rotating (above certain speed)
        // TODO: would it make more sense to have this in the movement system?
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
                // Might need to move this code to MicrobeMovementSystem for this to be possible
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

    private void UpdatePullingCilia(ref OrganelleContainer organelleContainer, in Entity microbeEntity,
        IWorldSimulation worldSimulation, float delta)
    {
        if (sharedPullData == null)
        {
            // Pull initialization, only one cilia will be in control of the pull

            // First check if there are other initialized pulls on this entity
            sharedPullData = FindExistingPull(ref organelleContainer);

            if (sharedPullData == null)
            {
                sharedPullData = new PullingCiliaData(this, ++ciliaPullCount);

                // Also report to others so no other one will try to be in control
                ReportCreatedCiliaPull(ref organelleContainer, sharedPullData);

                // This return here is to stabilize the initialization to detect all the cilia before creating the shape
                return;
            }

            // Resume the alive counter from where it was before we were created
            ciliaPullCount = sharedPullData.PullPerformed;
        }

        // Only primary cilia does pull
        if (sharedPullData.PrimaryCilia != this)
        {
            // Check if we should take over as the new primary cilia
            if (Math.Abs(sharedPullData.PullPerformed - ciliaPullCount) > 4)
            {
                // Time to take over
                sharedPullData.PrimaryCilia = this;
                sharedPullData.PullPerformed = ++ciliaPullCount;

                ReportCreatedCiliaPull(ref organelleContainer, sharedPullData);
            }
            else
            {
                ciliaPullCount = sharedPullData.PullPerformed;

                // Not using interlocked here as only one organelle component can process at once from the same entity
                ++sharedPullData.CiliaCount;
            }

            return;
        }

        sharedPullData.PullPerformed = ++ciliaPullCount;

        // This needs to be done early to make early exits not cause issues
        // Reset this to have the other cilia report themselves to the shared object before the next update
        var ciliaCountForForce = sharedPullData.CiliaCount;
        sharedPullData.CiliaCount = 1;

        if (!microbeEntity.Has<PhysicsSensor>())
        {
            // Cilia initialization
            var recorder = worldSimulation.StartRecordingEntityCommands();

            var entityRecord = recorder.Record(microbeEntity);
            entityRecord.Set(new PhysicsSensor(Constants.MAX_SIMULTANEOUS_COLLISIONS_SENSOR)
            {
                ActiveArea = CreateCiliaDetectorShape(sharedPullData.CiliaCount),
            });

            worldSimulation.FinishRecordingEntityCommands(recorder);

            // Wait until commands are finished
            return;
        }

        ref var sensor = ref microbeEntity.Get<PhysicsSensor>();

        // When loading a save some state needs to be reinitialized. Or if the cilia count has changed then the shape
        // also needs to be recreated
        if (sensor.ActiveArea == null || sharedPullData.SizeCreatedWithCilia != sharedPullData.CiliaCount)
        {
            // TODO: could dispose the old area here to release that shape data faster

            sensor.ActiveArea = CreateCiliaDetectorShape(sharedPullData.CiliaCount);
            sensor.ApplyNewShape = true;
            return;
        }

        // TODO: should the area shape grow with the organelle growth like before? That seems pretty excessively
        // performance intensive to re-create shapes constantly with the new physics
        // Make the pulling force's radius scales with the organelle's growth value
        // attractorShape.Radius = Constants.CILIA_PULLING_FORCE_FIELD_RADIUS +
        //    (Constants.CILIA_PULLING_FORCE_GROW_STEP * organelle!.GrowthValue);

        sensor.GetDetectedBodies(sharedPullData.JustPulledEntities);

        // Skipped when count is 1 as the cell is just detecting itself
        if (sharedPullData.JustPulledEntities.Count < 2)
            return;

        ref var microbePosition = ref microbeEntity.Get<WorldPosition>();
        ref var microbePhysicsControl = ref microbeEntity.Get<ManualPhysicsControl>();

        foreach (var pulledEntity in sharedPullData.JustPulledEntities)
        {
            // Don't pull self
            if (pulledEntity == microbeEntity)
                continue;

            // Skip if something that can't be pulled
            if (!pulledEntity.Has<ManualPhysicsControl>())
                continue;

            ref var targetPosition = ref pulledEntity.Get<WorldPosition>();

            // Fall off force by squared distance
            var distanceSquared = targetPosition.Position.DistanceSquaredTo(microbePosition.Position);

            // Too close to pull
            if (distanceSquared < MathUtils.EPSILON)
                return;

            float force = Constants.CILIA_PULLING_FORCE * ciliaCountForForce *
                Constants.CILIA_FORCE_MULTIPLIER_PER_CILIA * delta / distanceSquared;

            ref var targetPhysics = ref pulledEntity.Get<ManualPhysicsControl>();
            targetPhysics.ImpulseToGive +=
                (microbePosition.Position - targetPosition.Position).Normalized() * force;
            targetPhysics.PhysicsApplied = false;

            // Follow Newton's third law
            microbePhysicsControl.ImpulseToGive +=
                (targetPosition.Position - microbePosition.Position).Normalized() * force;
            microbePhysicsControl.PhysicsApplied = false;
        }
    }

    private PullingCiliaData? FindExistingPull(ref OrganelleContainer organelleContainer)
    {
        foreach (var placedOrganelle in organelleContainer.Organelles!)
        {
            foreach (var component in placedOrganelle.Components)
            {
                if (component is not CiliaComponent ciliaComponent)
                    continue;

                if (ciliaComponent.sharedPullData != null)
                    return ciliaComponent.sharedPullData;
            }
        }

        return null;
    }

    private void ReportCreatedCiliaPull(ref OrganelleContainer organelleContainer, PullingCiliaData dataToSet)
    {
        foreach (var placedOrganelle in organelleContainer.Organelles!)
        {
            foreach (var component in placedOrganelle.Components)
            {
                if (component is not CiliaComponent ciliaComponent)
                    continue;

                if (component == this)
                    continue;

                ciliaComponent.ciliaPullCount = dataToSet.PullPerformed;
                ciliaComponent.sharedPullData = dataToSet;
            }
        }
    }

    private PhysicsShape CreateCiliaDetectorShape(int count)
    {
        if (sharedPullData == null)
            throw new InvalidOperationException("Pull data should be initialized first");

        sharedPullData.SizeCreatedWithCilia = count;

        // TODO: this will need a size parameter if cilia can ever be placed on prokaryotes
        return PhysicsShape.CreateSphere(Constants.CILIA_PULLING_FORCE_FIELD_RADIUS +
            (count * Constants.CILIA_PULL_RADIUS_PER_CILIA));
    }

    /// <summary>
    ///   Data related to pulling cilia that is shared between all of the cilia in a cell
    /// </summary>
    private class PullingCiliaData
    {
        public readonly HashSet<Entity> JustPulledEntities = new();

        public CiliaComponent PrimaryCilia;

        public int CiliaCount = 1;
        public int SizeCreatedWithCilia;

        /// <summary>
        ///   Used to detect if the primary cilia disappears
        /// </summary>
        public int PullPerformed;

        public PullingCiliaData(CiliaComponent creator, int pullCount)
        {
            PrimaryCilia = creator;
            PullPerformed = pullCount;
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
