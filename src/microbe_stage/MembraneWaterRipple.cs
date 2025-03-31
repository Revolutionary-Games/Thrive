using System;
using Godot;

/// <summary>
///   Manages spawning and processing ripple effect for microbes
/// </summary>
public partial class MembraneWaterRipple : Node
{
    [Export]
    public bool EnableEffect = true;

    [Export]
    public float RippleStrength = 0.8f;

    [Export]
    public float VerticalOffset = -0.05f;

    /// <summary>
    ///   Number of past positions to store for the movement history
    /// </summary>
    [Export]
    public int PositionHistorySize = 14;

    /// <summary>
    ///   How frequently to record a new position
    /// </summary>
    [Export]
    public float PositionRecordInterval = 0.02f;

    [Export]
    public float VisibilityCheckInterval = 0.2f;

    /// <summary>
    ///   Time after which ripples start to fade when cell is not moving
    /// </summary>
    [Export]
    public float StillnessFadeDelay = 1.5f;

    /// <summary>
    ///   How quickly ripples fade out when cell is not moving
    /// </summary>
    [Export]
    public float StillnessFadeRate = 0.8f;

    /// <summary>
    ///   Delay before ripples start to form
    /// </summary>
    [Export]
    public float RippleFormationDelay = 0.2f;

    /// <summary>
    ///   Minimal movement threshold
    /// </summary>
    [Export]
    public float MovementThresholdSqr = 0.0001f;

    /// <summary>
    ///   Threshold for resuming movement
    /// </summary>
    [Export]
    public float ResumeMovementThresholdSqr = 0.0003f;

    /// <summary>
    ///   Predefined LOD distance values
    /// </summary>
    private const float VERY_LOW_LOD_DISTANCE = 120.0f;
    private const float LOW_LOD_DISTANCE = 80.0f;
    private const float HIGH_LOD_DISTANCE = 40.0f;

    /// <summary>
    ///   Predefined LOD subdivision values
    /// </summary>
    private const int VERY_LOW_LOD_SUBDIVISION = 40;
    private const int LOW_LOD_SUBDIVISION = 60;
    private const int MEDIUM_LOD_SUBDIVISION = 90;
    private const int HIGH_LOD_SUBDIVISION = 120;

    /// <summary>
    ///   Maximum delta time to prevent jittering during lag spikes
    /// </summary>
    private const float MAX_DELTA_TIME = 0.1f;

    /// <summary>
    ///   Maximum time to remain in a transition state
    /// </summary>
    private const float MAX_TRANSITION_TIME = 5.0f;

    /// <summary>
    ///   Pre-created meshes for each LOD level to avoid runtime regeneration
    /// </summary>
    private static readonly PlaneMesh?[] LodMeshes = new PlaneMesh?[4];

#pragma warning disable CA2213

    private readonly StringName timeOffsetParam = new("TimeOffset");
    private readonly StringName movementDirectionParam = new("MovementDirection");
    private readonly StringName movementSpeedParam = new("MovementSpeed");
    private readonly StringName waterColorParam = new("WaterColor");
    private readonly StringName rippleStrengthParam = new("RippleStrength");
    private readonly StringName phaseParam = new("Phase");
    private readonly StringName attenuationParam = new("Attenuation");
    private readonly StringName pastPositionsParam = new("PastPositions");
    private readonly StringName pastPositionsCountParam = new("PastPositionsCount");
    private readonly StringName stillnessFactorParam = new("StillnessFactor");
    private readonly StringName membraneRadiusParam = new("MembraneRadius");

    /// <summary>
    ///   Fade-in speed multiplier (higher = faster fade-in)
    /// </summary>
    [Export]
    private float fadeInSpeed = 3.5f;

    /// <summary>
    ///   Fade-out speed multiplier
    /// </summary>
    [Export]
    private float fadeOutSpeed = 1.2f;

    [Export]
    private MeshInstance3D waterPlane = null!;
    private ShaderMaterial waterMaterial = null!;
    private PlaneMesh planeMesh = null!;

    /// <summary>
    ///   Size of the membrane we're creating ripples for
    /// </summary>
    private float membraneRadius = 5.0f;

    /// <summary>
    ///   Parent node for positioning
    /// </summary>
    private Node3D? positionTarget;

    /// <summary>
    ///   Position tracking and effect state variables
    /// </summary>
    private Vector2[] pastPositions = new Vector2[14];
    private Vector3[] positionHistory = new Vector3[14];
    private Godot.Collections.Array<Vector2> godotPastPositions = new();
    private int currentPositionIndex;
    private bool isPositionHistoryFull;
    private float positionRecordTimer;
    private Vector3 lastPosition;
    private Vector3 previousPosition;
    private Vector2 currentDirection = Vector2.Right;
    private Vector2 lastValidDirection = Vector2.Right;
    private float currentSpeed;
    private float timeAccumulator;
    private bool isCurrentlyVisible = true;
    private float visibilityCheckTimer;
    private bool isEffectEnabled = true;

    /// <summary>
    ///   Stillness tracking variables
    /// </summary>
    private float stillnessTimer;
    private float stillnessFactor = 0.0f;
    private bool wasMovingLastFrame = false;
    private float averageMovementSqr = 0.0f;
    private float directionChangeTimer = 0.0f;
    private float lastDirectionChangeTime = 0.0f;
    private float timeWithoutMovement = 0.0f;

    /// <summary>
    ///   State machine variables for fade transitions
    /// </summary>
    private FadeState currentFadeState = FadeState.Inactive;
    private float fadeProgress = 0.0f;
    private float stateTime = 0.0f;
    private float minAlpha = 0.00002f;
    private float fullAlpha = 0.02f;
    private float formationProgress = 0.0f;

    /// <summary>
    ///   Camera state caching variables
    /// </summary>
    private Camera3D? currentCamera;
    private float lastCameraDistance;
    private Vector3 lastCameraPosition;
    private bool isCameraPositionValid;

    private LodLevel currentLodLevel = LodLevel.Medium;
#pragma warning restore CA2213

    /// <summary>
    ///   Predefined LOD levels for optimization
    /// </summary>
    private enum LodLevel
    {
        VeryLow,
        Low,
        Medium,
        High,
    }

    /// <summary>
    ///   Possible states for the ripple effect
    /// </summary>
    private enum FadeState
    {
        Inactive,
        Forming,
        FadingIn,
        Active,
        FadingOut,
        Minimal,
    }

    public override void _Ready()
    {
        waterMaterial = (ShaderMaterial)waterPlane.MaterialOverride;
        planeMesh = (PlaneMesh)waterPlane.Mesh;
        InitializeLodMeshes();

        // Initialize arrays
        for (int i = 0; i < PositionHistorySize; ++i)
        {
            godotPastPositions.Add(Vector2.Zero);
            pastPositions[i] = Vector2.Zero;
            positionHistory[i] = Vector3.Zero;
        }

        // Initialize state
        isEffectEnabled = true;
        fadeProgress = 0.0f;
        formationProgress = 0.0f;
        currentFadeState = FadeState.Inactive;
        stillnessFactor = 0.0f;
        stillnessTimer = 0.0f;
        timeWithoutMovement = 0.0f;
        currentPositionIndex = 0;
        isPositionHistoryFull = false;

        InitializeShaderParameters();
        currentCamera = GetViewport().GetCamera3D();
        waterPlane.Visible = false;
    }

    /// <summary>
    ///   Initialize the ripple effect for a membrane
    /// </summary>
    public void Initialize(Membrane membrane)
    {
        positionTarget = membrane ?? throw new ArgumentNullException(nameof(membrane));

        lastPosition = membrane.GlobalPosition;
        previousPosition = lastPosition;

        // Initialize position history with current position
        for (int i = 0; i < PositionHistorySize; ++i)
        {
            positionHistory[i] = lastPosition;
        }

        membraneRadius = Math.Max(1.0f, membrane.EncompassingCircleRadius);
        waterMaterial.SetShaderParameter(membraneRadiusParam, membraneRadius);
        UpdateMeshSize();
        currentPositionIndex = 0;
        isPositionHistoryFull = false;
        TransitionToState(FadeState.Forming);
        waterPlane.Visible = true;
        UpdatePosition();
    }

    public override void _Process(double delta)
    {
        if (!EnableEffect || positionTarget == null)
            return;

        // Clamp delta to ensure no jittering during lag spikes
        float clampedDelta = MathF.Min((float)delta, MAX_DELTA_TIME);
        currentCamera = GetViewport().GetCamera3D();

        // Updates camera and visibility
        visibilityCheckTimer += clampedDelta;
        if (visibilityCheckTimer >= VisibilityCheckInterval)
        {
            visibilityCheckTimer = 0.0f;
            UpdateCameraCache();
            isCurrentlyVisible = IsVisible();

            // Toggle water plane visibility
            if (waterPlane.Visible != (isCurrentlyVisible && isEffectEnabled))
            {
                waterPlane.Visible = isCurrentlyVisible && isEffectEnabled;
            }
        }

        if (!isCurrentlyVisible || !isEffectEnabled)
            return;

        UpdatePosition();
        stateTime += clampedDelta;
        ProcessFadeState(clampedDelta);

        // Update time for animation based on LOD level and movement
        float timeScale = 1.0f;
        if (currentSpeed > 0.1f)
        {
            // Scale time update by current LOD level
            switch (currentLodLevel)
            {
                case LodLevel.VeryLow:
                    timeScale = 1.0f + currentSpeed * 0.05f;
                    break;

                case LodLevel.Low:
                    timeScale = 1.0f + currentSpeed * 0.08f;
                    break;

                case LodLevel.Medium:
                    timeScale = 1.0f + currentSpeed * 0.1f;
                    break;

                case LodLevel.High:
                    timeScale = 1.0f + currentSpeed * 0.12f;
                    break;
            }
        }

        timeScale = Mathf.Lerp(timeScale, 0.5f, stillnessFactor);
        timeAccumulator += clampedDelta * timeScale;
        waterMaterial.SetShaderParameter(timeOffsetParam, timeAccumulator);

        // Update movement parameters with frequency based on LOD
        if (currentLodLevel == LodLevel.VeryLow)
        {
            // For very distant membranes, update less frequently (every 0.3 seconds)
            if ((int)(timeAccumulator * 10) % 3 == 0)
            {
                // Pass accumulated time since last update (approximately 0.3 seconds)
                UpdateMovementParameters(0.3f);
            }
        }
        else
        {
            UpdateMovementParameters(clampedDelta);
        }
    }

    /// <summary>
    ///   Initialize predefined meshes for each LOD level
    /// </summary>
    private void InitializeLodMeshes()
    {
        // Clone the base mesh for each LOD level
        float baseSize = planeMesh.Size.X;

        // Create meshes with different subdivision levels
        bool needsInitialization = LodMeshes[0] == null;
        if (needsInitialization)
        {
            for (int i = 0; i < 4; ++i)
            {
                LodMeshes[i] = new PlaneMesh();
                PlaneMesh mesh = LodMeshes[i]!;
                mesh.Size = new Vector2(baseSize, baseSize);

                // Set appropriate subdivision based on LOD level
                switch ((LodLevel)i)
                {
                    case LodLevel.VeryLow:
                        mesh.SubdivideWidth = VERY_LOW_LOD_SUBDIVISION;
                        mesh.SubdivideDepth = VERY_LOW_LOD_SUBDIVISION;
                        break;
                    case LodLevel.Low:
                        mesh.SubdivideWidth = LOW_LOD_SUBDIVISION;
                        mesh.SubdivideDepth = LOW_LOD_SUBDIVISION;
                        break;
                    case LodLevel.Medium:
                        mesh.SubdivideWidth = MEDIUM_LOD_SUBDIVISION;
                        mesh.SubdivideDepth = MEDIUM_LOD_SUBDIVISION;
                        break;
                    case LodLevel.High:
                        mesh.SubdivideWidth = HIGH_LOD_SUBDIVISION;
                        mesh.SubdivideDepth = HIGH_LOD_SUBDIVISION;
                        break;
                }
            }
        }
    }

    private void InitializeShaderParameters()
    {
        waterMaterial.SetShaderParameter(waterColorParam, new Color(0, 0, 0, 0.02f));
        waterMaterial.SetShaderParameter(rippleStrengthParam, RippleStrength);
        waterMaterial.SetShaderParameter(timeOffsetParam, 0.0f);
        waterMaterial.SetShaderParameter(movementSpeedParam, 0.0f);
        waterMaterial.SetShaderParameter(movementDirectionParam, Vector2.Zero);
        waterMaterial.SetShaderParameter(phaseParam, 0.2f);
        waterMaterial.SetShaderParameter(attenuationParam, 0.9998f);
        waterMaterial.SetShaderParameter(stillnessFactorParam, 0.0f);
        waterMaterial.SetShaderParameter(pastPositionsParam, godotPastPositions);
        waterMaterial.SetShaderParameter(pastPositionsCountParam, 0);
        waterMaterial.SetShaderParameter(membraneRadiusParam, 5.0f);
    }

    /// <summary>
    ///   Extremely gradual start with faster overall transition
    /// </summary>
    private float UltraGradualEase(float x)
    {
        if (x < 0.25f)
        {
            float t = x / 0.25f;
            return 0.15f * (t * t * t * t * t * t * t * t);
        }
        else if (x < 0.85f)
        {
            float t = (x - 0.25f) / 0.6f;
            float value = 0.15f + 0.8f * (t * t * t);
            return value;
        }
        else
        {
            float t = (x - 0.85f) / 0.15f;
            float value = 0.95f + 0.05f * t;
            return value;
        }
    }

    /// <summary>
    ///   Process the current fade state and handle transitions
    /// </summary>
    private void ProcessFadeState(float delta)
    {
        // Check for stuck transitions
        if ((currentFadeState is FadeState.FadingIn or FadeState.FadingOut or FadeState.Forming) &&
            stateTime > MAX_TRANSITION_TIME)
        {
            // Force completion of transition if stuck
            if (currentFadeState is FadeState.Forming or FadeState.FadingIn)
            {
                fadeProgress = 1.0f;
                TransitionToState(FadeState.Active);
            }
            else if (currentFadeState == FadeState.FadingOut)
            {
                fadeProgress = 1.0f;
                TransitionToState(FadeState.Minimal);
            }
        }

        // Process current state
        switch (currentFadeState)
        {
            case FadeState.Inactive:
                break;

            case FadeState.Forming:
                formationProgress = Math.Min(1.0f, formationProgress + delta * 2.0f);
                waterMaterial.SetShaderParameter(waterColorParam, new Color(0, 0, 0, 0.0f));

                if (stateTime >= RippleFormationDelay)
                {
                    // Once delay passes, start the fade
                    TransitionToState(FadeState.FadingIn);
                }

                break;

            case FadeState.FadingIn:
                fadeProgress = Math.Min(1.0f, fadeProgress + delta * fadeInSpeed);
                float easedProgress = UltraGradualEase(fadeProgress);
                float movementFactor = Math.Min(1.0f, currentSpeed * 2.5f);
                float alphaScale = Mathf.Lerp(0.6f, 1.0f, movementFactor);
                float alphaIn = Mathf.Lerp(minAlpha, fullAlpha * alphaScale, easedProgress);
                float stillnessValue = fadeProgress < 0.25f ?
                    Mathf.Lerp(0.99f, 0.7f, fadeProgress / 0.25f) :
                    Mathf.Lerp(0.7f, 0.0f, (fadeProgress - 0.25f) / 0.75f);

                waterMaterial.SetShaderParameter(waterColorParam, new Color(0, 0, 0, alphaIn));
                waterMaterial.SetShaderParameter(stillnessFactorParam, stillnessValue);

                if (fadeProgress >= 1.0f)
                {
                    TransitionToState(FadeState.Active);
                }

                break;

            case FadeState.Active:
                break;

            case FadeState.FadingOut:
                fadeProgress = Math.Min(1.0f, fadeProgress + delta * fadeOutSpeed);
                float easedFadeOut = fadeProgress * fadeProgress * fadeProgress;
                float alphaOut = Mathf.Lerp(fullAlpha, minAlpha, easedFadeOut);
                float stillnessOut = Mathf.Lerp(0.0f, 1.0f, easedFadeOut);
                waterMaterial.SetShaderParameter(waterColorParam, new Color(0, 0, 0, alphaOut));
                waterMaterial.SetShaderParameter(stillnessFactorParam, stillnessOut);

                if (fadeProgress >= 1.0f)
                {
                    TransitionToState(FadeState.Minimal);
                }

                break;

            case FadeState.Minimal:
                break;
        }
    }

    /// <summary>
    ///   Transition to a new fade state
    /// </summary>
    private void TransitionToState(FadeState newState)
    {
        if (newState == currentFadeState)
            return;

        // Reset state tracking variables
        stateTime = 0.0f;
        fadeProgress = 0.0f;

        // Handle specific state transitions
        switch (newState)
        {
            case FadeState.Inactive:
                waterMaterial.SetShaderParameter(waterColorParam, new Color(0, 0, 0, 0.0f));
                waterMaterial.SetShaderParameter(stillnessFactorParam, 1.0f);
                break;

            case FadeState.Forming:
                formationProgress = 0.0f;
                waterMaterial.SetShaderParameter(waterColorParam, new Color(0, 0, 0, 0.0f));
                waterMaterial.SetShaderParameter(stillnessFactorParam, 1.0f);
                break;

            case FadeState.FadingIn:
                waterMaterial.SetShaderParameter(waterColorParam, new Color(0, 0, 0, minAlpha));
                waterMaterial.SetShaderParameter(stillnessFactorParam, 0.99f);
                break;

            case FadeState.Active:
                waterMaterial.SetShaderParameter(waterColorParam, new Color(0, 0, 0, fullAlpha));
                waterMaterial.SetShaderParameter(stillnessFactorParam, 0.0f);
                break;

            case FadeState.FadingOut:
                waterMaterial.SetShaderParameter(waterColorParam, new Color(0, 0, 0, fullAlpha));
                waterMaterial.SetShaderParameter(stillnessFactorParam, 0.0f);
                break;

            case FadeState.Minimal:
                waterMaterial.SetShaderParameter(waterColorParam, new Color(0, 0, 0, minAlpha));
                waterMaterial.SetShaderParameter(stillnessFactorParam, 1.0f);
                break;
        }

        currentFadeState = newState;
    }

    /// <summary>
    ///   Check if the effect is currently visible
    /// </summary>
    private bool IsVisible()
    {
        if (positionTarget == null || currentCamera == null)
            return false;

        return GetViewport().GetVisibleRect().HasPoint(
            currentCamera.UnprojectPosition(positionTarget.GlobalPosition));
    }

    /// <summary>
    ///   Updates the camera position cache for LOD selection
    /// </summary>
    private void UpdateCameraCache()
    {
        if (currentCamera == null || positionTarget == null)
        {
            isCameraPositionValid = false;
            return;
        }

        Vector3 cameraPos = currentCamera.GlobalPosition;

        // Only update if camera moved significantly
        if (!isCameraPositionValid || lastCameraPosition.DistanceSquaredTo(cameraPos) > 0.25f)
        {
            lastCameraPosition = cameraPos;
            lastCameraDistance = positionTarget.GlobalPosition.DistanceTo(cameraPos);
            isCameraPositionValid = true;
            UpdateLodLevel();
        }
    }

    /// <summary>
    ///   Updates the LOD level based on camera distance
    /// </summary>
    private void UpdateLodLevel()
    {
        if (!isCameraPositionValid)
            return;

        LodLevel newLodLevel;
        float sizeScale = Math.Clamp(membraneRadius / 5.0f, 1.0f, 2.0f);

        if (lastCameraDistance > VERY_LOW_LOD_DISTANCE * sizeScale)
        {
            newLodLevel = LodLevel.VeryLow;
        }
        else if (lastCameraDistance > LOW_LOD_DISTANCE * sizeScale)
        {
            newLodLevel = LodLevel.Low;
        }
        else if (lastCameraDistance > HIGH_LOD_DISTANCE * sizeScale)
        {
            newLodLevel = LodLevel.Medium;
        }
        else
        {
            newLodLevel = LodLevel.High;
        }

        // If LOD level changed update mesh
        if (newLodLevel != currentLodLevel)
        {
            currentLodLevel = newLodLevel;
            ApplyLodMesh();
        }
    }

    /// <summary>
    ///   Apply the pre-created mesh for current LOD level
    /// </summary>
    private void ApplyLodMesh()
    {
        PlaneMesh lodMesh = LodMeshes[(int)currentLodLevel]!;
        lodMesh.Size = planeMesh.Size;
        waterPlane.Mesh = lodMesh;
        planeMesh = lodMesh;
    }

    /// <summary>
    ///   Updates the mesh size based on membrane radius
    /// </summary>
    private void UpdateMeshSize()
    {
        // Scale mesh size based on membrane radius
        float desiredSize = Math.Max(18.0f, membraneRadius * 2.2f);

        if (Math.Abs(planeMesh.Size.X - desiredSize) > 0.5f)
        {
            // Update size for all LOD meshes
            for (int i = 0; i < 4; ++i)
            {
                LodMeshes[i]!.Size = new Vector2(desiredSize, desiredSize);
            }

            // Update current mesh
            planeMesh.Size = new Vector2(desiredSize, desiredSize);
        }
    }

    /// <summary>
    ///   Updates the position to match the parent node's position
    /// </summary>
    private void UpdatePosition()
    {
        if (positionTarget == null)
            return;

        Transform3D transform = new Transform3D(
            Basis.Identity,
            new Vector3(positionTarget.GlobalPosition.X,
                positionTarget.GlobalPosition.Y + VerticalOffset,
                positionTarget.GlobalPosition.Z));

        waterPlane.GlobalTransform = transform;
    }

    /// <summary>
    ///   Updates movement parameters for the effect and records position history
    /// </summary>
    private void UpdateMovementParameters(float delta)
    {
        if (positionTarget == null)
            return;

        // Store previous position before calculating new movement
        previousPosition = lastPosition;

        // Calculates movement since the last frame
        Vector3 currentPos = positionTarget.GlobalPosition;
        Vector3 movement = currentPos - lastPosition;
        float movementSqr = movement.LengthSquared();
        averageMovementSqr = Mathf.Lerp(averageMovementSqr, movementSqr, 0.2f);
        bool significantMovement;

        if (wasMovingLastFrame)
        {
            significantMovement = averageMovementSqr > MovementThresholdSqr;
        }
        else
        {
            significantMovement = averageMovementSqr > ResumeMovementThresholdSqr;
        }

        // Update stillness tracking
        if (significantMovement)
        {
            // Reset stillness timer when moving
            stillnessTimer = 0.0f;
            timeWithoutMovement = 0.0f;
            wasMovingLastFrame = true;

            // Handle state transitions based on movement
            if (currentFadeState is FadeState.Minimal or FadeState.FadingOut)
            {
                TransitionToState(FadeState.FadingIn);
            }

            if (stillnessFactor > 0.0f)
            {
                stillnessFactor = Math.Max(0.0f, stillnessFactor - delta * 0.5f);
            }
        }
        else
        {
            // Increment stillness timer when not moving
            stillnessTimer += delta;
            timeWithoutMovement += delta;

            if (stillnessTimer > StillnessFadeDelay)
            {
                // Handle transition to fading out if the state is active
                if (currentFadeState == FadeState.Active)
                {
                    TransitionToState(FadeState.FadingOut);
                }

                float targetStillness = Math.Min(1.0f, (stillnessTimer - StillnessFadeDelay) * StillnessFadeRate);
                stillnessFactor = Mathf.Lerp(stillnessFactor, targetStillness, delta * 1.2f);
            }

            wasMovingLastFrame = false;
        }

        // Store position in circular history buffer at timed intervals
        positionRecordTimer += delta;
        if (positionRecordTimer >= PositionRecordInterval)
        {
            positionHistory[currentPositionIndex] = currentPos;
            currentPositionIndex = (currentPositionIndex + 1) % PositionHistorySize;

            if (currentPositionIndex == 0)
            {
                isPositionHistoryFull = true;
            }

            // Reset timer
            positionRecordTimer = 0;

            UpdateLocalPositions();

            // Only send as many positions as we actually need
            int actualCount = isPositionHistoryFull ? PositionHistorySize : currentPositionIndex;
            waterMaterial.SetShaderParameter(pastPositionsCountParam, actualCount);
            waterMaterial.SetShaderParameter(pastPositionsParam, godotPastPositions);
        }

        lastPosition = currentPos;

        // Direction and speed calculation
        directionChangeTimer += delta;
        Vector2 direction;
        float calculatedSpeed;

        if (significantMovement)
        {
            if (currentLodLevel == LodLevel.VeryLow || currentLodLevel == LodLevel.Low)
            {
                direction = new Vector2(movement.X, movement.Z).Normalized();
                calculatedSpeed = Math.Clamp(movement.Length() / delta * 2.5f, 0.05f, 1.0f);
                currentDirection = currentDirection.Lerp(direction, 0.4f);
                currentSpeed = Mathf.Lerp(currentSpeed, calculatedSpeed, 0.3f);
            }
            else
            {
                direction = new Vector2(movement.X, movement.Z).Normalized();
                float distance = movement.Length() / delta;

                if (distance > 0.0001f)
                {
                    Vector3 forwardVector = positionTarget.GlobalTransform.Basis.Z;
                    Vector2 orientationDir = new Vector2(forwardVector.X, forwardVector.Z).Normalized();
                    direction = direction.Lerp(orientationDir, 0.15f).Normalized();
                    float sizeAdjustedDistance = distance / Math.Max(1.0f, membraneRadius / 5.0f);
                    calculatedSpeed = Math.Clamp(sizeAdjustedDistance * 3.0f, 0.05f, 1.0f);
                    float directionChangeMagnitude = 1.0f - currentDirection.Dot(direction);

                    if (directionChangeMagnitude > 0.2f)
                    {
                        calculatedSpeed = Math.Max(calculatedSpeed, 0.2f + directionChangeMagnitude * 0.3f);

                        // Track significant direction changes
                        lastDirectionChangeTime = timeAccumulator;
                        directionChangeTimer = 0.0f;
                    }

                    // Use a more responsive lerp for direction changes
                    float dirLerpFactor = Math.Min(0.15f + directionChangeMagnitude * 0.5f, 0.6f);
                    currentDirection = currentDirection.Lerp(direction, dirLerpFactor);
                    currentSpeed = Mathf.Lerp(currentSpeed, calculatedSpeed, 0.15f);

                    // Store last valid direction when moving
                    if (movement.LengthSquared() > 0.001f)
                    {
                        lastValidDirection = currentDirection;
                    }
                }
            }
        }
        else
        {
            currentSpeed = Mathf.Lerp(currentSpeed, 0.05f, 0.01f);

            // If direction hasn't changed for a while but we're moving, nudge it
            if (directionChangeTimer > 1.0f && wasMovingLastFrame)
            {
                // Force direction update if it's been frozen
                Vector3 forwardVector = positionTarget.GlobalTransform.Basis.Z;
                Vector2 orientationDir = new Vector2(forwardVector.X, forwardVector.Z).Normalized();
                currentDirection = currentDirection.Lerp(orientationDir, 0.05f).Normalized();
                directionChangeTimer = 0.0f;
            }
        }

        // Make sure direction is always normalized
        if (currentDirection.LengthSquared() < 0.01f)
        {
            currentDirection = lastValidDirection;
        }

        waterMaterial.SetShaderParameter(movementDirectionParam, currentDirection);
        waterMaterial.SetShaderParameter(movementSpeedParam, currentSpeed);
    }

    /// <summary>
    ///   Updates the local position array for the shader
    ///   based on the real position history
    /// </summary>
    private void UpdateLocalPositions()
    {
        if (positionTarget == null)
            return;

        Vector3 currentPos = positionTarget.GlobalPosition;

        // Calculate local positions relative to current position
        int count = isPositionHistoryFull ? PositionHistorySize : currentPositionIndex;

        // Optimize for LOD level - use fewer samples for distant objects
        int step = 1;
        if (currentLodLevel == LodLevel.VeryLow)
            step = 3;
        else if (currentLodLevel == LodLevel.Low)
            step = 2;

        // Access circular buffer with wrapping index
        // Transforms world positions to local XZ offsets for shader
        for (int i = 0; i < count; i += step)
        {
            int index = (currentPositionIndex - 1 - i).PositiveModulo(PositionHistorySize);

            Vector3 offset = positionHistory[index] - currentPos;
            Vector2 position = new Vector2(offset.X, offset.Z);

            // Update both arrays
            pastPositions[i / step] = position;
            godotPastPositions[i / step] = position;
        }

        // Fill remaining positions with zeros for lower LOD sampling
        if (step > 1)
        {
            for (int i = count / step + 1; i < count; ++i)
            {
                pastPositions[i] = Vector2.Zero;
                godotPastPositions[i] = Vector2.Zero;
            }
        }
    }
}
