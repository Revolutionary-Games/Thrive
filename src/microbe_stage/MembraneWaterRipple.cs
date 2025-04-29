using System;
using Godot;
using Godot.Collections;

/// <summary>
///   Manages spawning and processing ripple effect for microbes
/// </summary>
public partial class MembraneWaterRipple : Node
{
    [Export]
    public bool EnableEffect = true;

    /// <summary>
    ///   Multiplier applied to the ripple amplitude inside the shader
    ///   must remain in sync with the value in the shader
    /// </summary>
    [Export]
    public float RippleStrength = 0.8f;

    /// <summary>
    ///   Vertical offset (in world units) applied when positioning the
    ///   ripple plane beneath the cell membrane
    ///   must match the one in the shader
    /// </summary>
    [Export]
    public float VerticalOffset = -0.05f;

    /// <summary>
    ///   Number of past positions to store for the movement history
    /// </summary>
    [Export]
    public int PositionHistorySize = MAX_POSITION_HISTORY;

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
    ///   Maximum delta time to prevent jittering
    /// </summary>
    private const float MAX_DELTA_TIME = 0.1f;

    /// <summary>
    ///   The maximum number of past positions to store
    ///   This must match the array size in shader
    /// </summary>
    private const int MAX_POSITION_HISTORY = 14;

    /// <summary>
    ///   Cached handles for the shader's uniform names
    ///   (prevents per-frame allocations).
    /// </summary>
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
    private float fadeInSpeed = 7.0f;

    /// <summary>
    ///   Fade-out speed multiplier
    /// </summary>
    [Export]
    private float fadeOutSpeed = 0.6f;

#pragma warning disable CA2213

    /// <summary>
    ///   Rendering resources that together display the ripple effect
    /// </summary>
    [Export]
    private MeshInstance3D waterPlane = null!;

    private ShaderMaterial waterMaterial = null!;
    private PlaneMesh planeMesh = null!;

    /// <summary>
    ///   Parent node for positioning
    /// </summary>
    private Node3D? positionTarget;

    /// <summary>
    ///   Cached reference to the active camera
    /// </summary>
    private Camera3D? currentCamera;

#pragma warning restore CA2213

    /// <summary>
    ///   Default Size of the membrane we're creating ripples for
    /// </summary>
    private float membraneRadius = 5.0f;

    /// <summary>
    ///   Position tracking and effect state variables
    /// </summary>
    private Vector2[] pastPositions = new Vector2[MAX_POSITION_HISTORY];
    private Vector3[] positionHistory = new Vector3[MAX_POSITION_HISTORY];
    private Array<Vector2> godotPastPositions = new();
    private int currentPositionIndex = 0;
    private bool isPositionHistoryFull = false;
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
    private float stillnessTimer = 0.0f;

    private float stillnessFactor = 0.0f;
    private bool wasMovingLastFrame;
    private float averageMovementSqr;
    private float directionChangeTimer;
    private float lastDirectionChangeTime;
    private float timeWithoutMovement = 0.0f;

    /// <summary>
    ///   Ripple effect variables
    /// </summary>
    private float targetAlpha = 0.0f;

    private float currentAlpha = 0.0f;
    private float stillnessValue = 1.0f;
    private float targetStillness = 1.0f;
    private bool isForming = false;
    private float formingTime = 0.0f;
    private float minAlpha = 0.00002f;
    private float fullAlpha = 0.02f;

    /// <summary>
    ///   Camera state caching variables
    /// </summary>
    private float lastCameraDistance;
    private Vector3 lastCameraPosition;
    private bool isCameraPositionValid;

    /// <summary>
    ///   Initialization flag
    /// </summary>
    private bool isInitialized;

    /// <summary>
    ///   Tracks whether Dispose has been called
    /// </summary>
    private bool disposed = false;

    public override void _Ready()
    {
        // Check for required components first - invert the if statement to reduce nesting
        if (!(waterPlane.MaterialOverride is ShaderMaterial material && waterPlane.Mesh is PlaneMesh mesh))
        {
            GD.PrintErr("MembraneWaterRipple: Required components missing");
            return;
        }

        waterMaterial = material;
        planeMesh = mesh;

        // Initialize the position history arrays
        godotPastPositions.Clear();
        for (int i = 0; i < MAX_POSITION_HISTORY; ++i)
        {
            godotPastPositions.Add(Vector2.Zero);
            pastPositions[i] = Vector2.Zero;
            positionHistory[i] = Vector3.Zero;
        }

        // Initialize shader parameters
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
        currentCamera = GetViewport().GetCamera3D();
        waterPlane.Visible = false;

        // If we got here everything is properly initialized :)
        isInitialized = true;
    }

    /// <summary>
    ///   Ensure cleanup when the node is removed from the scene
    /// </summary>
    public override void _ExitTree()
    {
        base._ExitTree();
        Dispose();
    }

    public override void _Process(double delta)
    {
        // Skip if not initialized or missing required components
        if (!isInitialized || !EnableEffect || positionTarget == null)
            return;

        // Clamp delta to further prevent jittering
        float clampedDelta = MathF.Min((float)delta, MAX_DELTA_TIME);
        currentCamera = GetViewport().GetCamera3D();

        // Updates camera and visibility
        visibilityCheckTimer += clampedDelta;
        if (visibilityCheckTimer >= VisibilityCheckInterval)
        {
            visibilityCheckTimer = 0.0f;
            UpdateCameraCache();
            isCurrentlyVisible = IsVisible();

            // Toggle water plane visibility only when necessary
            if (waterPlane.Visible != (isCurrentlyVisible && isEffectEnabled))
            {
                waterPlane.Visible = isCurrentlyVisible && isEffectEnabled;
            }
        }

        if (!isCurrentlyVisible || !isEffectEnabled)
            return;

        if (positionTarget.IsInsideTree())
        {
            // Optimize position calculation by reusing the base vector and only adding offset
            waterPlane.GlobalTransform = new Transform3D(Basis.Identity,
                positionTarget.GlobalPosition + new Vector3(0, VerticalOffset, 0));
        }

        UpdateRippleEffect(clampedDelta);
        float timeScale = CalculateTimeScale();
        timeAccumulator += clampedDelta * timeScale;
        waterMaterial.SetShaderParameter(timeOffsetParam, timeAccumulator);
        UpdateMovementParameters(clampedDelta);
    }

    /// <summary>
    ///   Updates the target and size from an external source or initializes the effect.
    /// </summary>
    public void UpdateTarget(Node3D? target, float radius)
    {
        // Handle initialization error with a proper error message
        if (!isInitialized)
        {
            GD.PushWarning("MembraneWaterRipple: Node not initialized");
            return;
        }

        // Clear and re-initialize the Godot array it ensures correct sizing
        godotPastPositions.Clear();
        for (int i = 0; i < MAX_POSITION_HISTORY; ++i)
        {
            godotPastPositions.Add(Vector2.Zero);
        }

        // Initialize position history with default position
        lastPosition = Vector3.Zero;
        previousPosition = Vector3.Zero;

        for (int i = 0; i < MAX_POSITION_HISTORY; ++i)
        {
            positionHistory[i] = lastPosition;
            pastPositions[i] = Vector2.Zero;
        }

        if (target != null && target.IsInsideTree())
        {
            positionTarget = target;
            lastPosition = target.GlobalPosition;
            previousPosition = lastPosition;

            // Update position in world space
            waterPlane.GlobalTransform = new Transform3D(Basis.Identity,
                target.GlobalPosition + new Vector3(0, VerticalOffset, 0));

            // Make visible if not already
            if (!waterPlane.Visible && isCurrentlyVisible && isEffectEnabled)
            {
                waterPlane.Visible = true;
            }
        }

        // Set radius and update mesh
        membraneRadius = Math.Max(1.0f, radius);
        waterMaterial.SetShaderParameter(membraneRadiusParam, membraneRadius);
        waterMaterial.SetShaderParameter(waterColorParam, new Color(0, 0, 0, 0.0f));
        waterMaterial.SetShaderParameter(stillnessFactorParam, 1.0f);

        float desiredSize = Math.Max(18.0f, membraneRadius * 2.2f);
        if (Math.Abs(planeMesh.Size.X - desiredSize) > 0.5f)
        {
            planeMesh.Size = new Vector2(desiredSize, desiredSize);
        }

        UpdateDetailLevelSubdivisions();

        // Reset state tracking
        currentPositionIndex = 0;
        isPositionHistoryFull = false;

        // Start informing state
        isForming = true;
        formingTime = 0.0f;
        currentAlpha = 0.0f;
        targetAlpha = 0.0f;
        stillnessValue = 1.0f;
        targetStillness = 1.0f;
    }

    public new void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            timeOffsetParam?.Dispose();
            movementDirectionParam?.Dispose();
            movementSpeedParam?.Dispose();
            waterColorParam?.Dispose();
            rippleStrengthParam?.Dispose();
            phaseParam?.Dispose();
            attenuationParam?.Dispose();
            pastPositionsParam?.Dispose();
            pastPositionsCountParam?.Dispose();
            stillnessFactorParam?.Dispose();
            membraneRadiusParam?.Dispose();
        }

        base.Dispose(disposing);
    }

    private float CalculateTimeScale()
    {
        float timeScale = 1.0f;

        if (currentSpeed > 0.1f)
        {
            // Scale based on distance from camera
            if (isCameraPositionValid)
            {
                if (lastCameraDistance > 120.0f)
                {
                    timeScale = 1.0f + currentSpeed * 0.05f;
                }
                else if (lastCameraDistance > 80.0f)
                {
                    timeScale = 1.0f + currentSpeed * 0.08f;
                }
                else if (lastCameraDistance > 40.0f)
                {
                    timeScale = 1.0f + currentSpeed * 0.1f;
                }
                else
                {
                    timeScale = 1.0f + currentSpeed * 0.12f;
                }
            }
        }

        return Mathf.Lerp(timeScale, 0.5f, stillnessFactor);
    }

    /// <summary>
    ///   Updates the ripple effect's alpha and stillness
    /// </summary>
    private void UpdateRippleEffect(float delta)
    {
        // Handle forming delay first
        if (isForming)
        {
            formingTime += delta;
            if (formingTime >= RippleFormationDelay)
            {
                isForming = false;
                targetAlpha = fullAlpha;
                targetStillness = 0.0f;
            }

            return;
        }

        targetAlpha = wasMovingLastFrame ? fullAlpha : minAlpha;
        targetStillness = wasMovingLastFrame ? 0.0f : 1.0f;

        // When newly not moving apply delay before fading out
        if (!wasMovingLastFrame && stillnessTimer > StillnessFadeDelay)
        {
            targetAlpha = minAlpha;
            targetStillness = 1.0f;
        }

        // Simple lerp with delta for transitions
        currentAlpha = Mathf.Lerp(currentAlpha, targetAlpha,
            delta * (currentAlpha < targetAlpha ? fadeInSpeed : fadeOutSpeed));
        stillnessValue = Mathf.Lerp(stillnessValue, targetStillness, delta * 3.0f);
        waterMaterial.SetShaderParameter(waterColorParam, new Color(0, 0, 0, currentAlpha));
        waterMaterial.SetShaderParameter(stillnessFactorParam, stillnessValue);
    }

    /// <summary>
    ///   Check if the effect is currently visible
    ///   This is used to optimize performance by only processing effects that are visible to the camera,
    ///   going beyond normal frustum culling for more expensive effects
    /// </summary>
    private bool IsVisible()
    {
        if (positionTarget == null || currentCamera == null || !positionTarget.IsInsideTree())
            return false;

        return GetViewport().GetVisibleRect().HasPoint(currentCamera.UnprojectPosition(positionTarget.GlobalPosition));
    }

    /// <summary>
    ///   Updates the camera position cache and detail level
    /// </summary>
    private void UpdateCameraCache()
    {
        if (currentCamera == null || positionTarget == null || !positionTarget.IsInsideTree())
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

            // Update detail level based on distance
            UpdateDetailLevelSubdivisions();
        }
    }

    /// <summary>
    ///   Updates the mesh subdivisions based on camera distance
    /// </summary>
    private void UpdateDetailLevelSubdivisions()
    {
        if (!isCameraPositionValid)
            return;

        float sizeScale = Math.Clamp(membraneRadius / 5.0f, 1.0f, 2.0f);
        float scaledDistance = lastCameraDistance / sizeScale;

        // Map distance (0-120) to subdivision (120-40) with a non-linear curve
        float t = Math.Clamp(scaledDistance / 120.0f, 0.0f, 1.0f);
        t = MathF.Pow(t, 1.5f);

        // Calculate subdivision from min (40) to max (120)
        int subdivision = (int)Mathf.Lerp(120, 40, t);

        if (planeMesh.SubdivideWidth != subdivision || planeMesh.SubdivideDepth != subdivision)
        {
            planeMesh.SubdivideWidth = subdivision;
            planeMesh.SubdivideDepth = subdivision;
        }
    }

    /// <summary>
    ///   Updates movement parameters for the effect and records position history
    /// </summary>
    private void UpdateMovementParameters(float delta)
    {
        if (positionTarget == null || !positionTarget.IsInsideTree())
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

            float stillnessRatio = Math.Min(1.0f, (stillnessTimer - StillnessFadeDelay) * StillnessFadeRate);
            stillnessFactor = Mathf.Lerp(stillnessFactor, stillnessRatio, delta * 1.2f);

            wasMovingLastFrame = false;
        }

        // Store position in circular history buffer at timed intervals
        positionRecordTimer += delta;
        if (positionRecordTimer >= PositionRecordInterval)
        {
            positionHistory[currentPositionIndex] = currentPos;
            currentPositionIndex = (currentPositionIndex + 1) % MAX_POSITION_HISTORY;

            if (currentPositionIndex == 0)
            {
                isPositionHistoryFull = true;
            }

            // Reset timer
            positionRecordTimer = 0;

            UpdateLocalPositions();

            // Only send as many positions as we actually need
            int actualCount = isPositionHistoryFull ? MAX_POSITION_HISTORY : currentPositionIndex;
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
            if (lastCameraDistance > 80.0f)
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
        if (positionTarget == null || !positionTarget.IsInsideTree())
            return;

        Vector3 currentPos = positionTarget.GlobalPosition;

        // Calculate local positions relative to current position
        int count = isPositionHistoryFull ? MAX_POSITION_HISTORY : currentPositionIndex;

        // Optimize based on camera distance so uses fewer samples for distant objects
        int step = 1;
        if (lastCameraDistance > 120.0f)
        {
            step = 3;
        }
        else if (lastCameraDistance > 80.0f)
        {
            step = 2;
        }

        // Reset all positions to zero first to ensure clean state
        for (int i = 0; i < MAX_POSITION_HISTORY && i < godotPastPositions.Count; ++i)
        {
            pastPositions[i] = Vector2.Zero;
            godotPastPositions[i] = Vector2.Zero;
        }

        // Access circular buffer with wrapping index
        // Transforms world positions to local XZ offsets for shader
        int targetPositionsCount = 0;

        for (int i = 0; i < count; i += step)
        {
            int index = (currentPositionIndex - 1 - i).PositiveModulo(MAX_POSITION_HISTORY);
            Vector3 offset = positionHistory[index] - currentPos;
            Vector2 position = new Vector2(offset.X, offset.Z);

            int targetIndex = i / step;
            if (targetIndex < MAX_POSITION_HISTORY && targetIndex < godotPastPositions.Count)
            {
                pastPositions[targetIndex] = position;
                godotPastPositions[targetIndex] = position;
                targetPositionsCount = targetIndex + 1;
            }
        }

        // Update the count of non-zero entries in the array
        waterMaterial.SetShaderParameter(pastPositionsCountParam, targetPositionsCount);
    }
}
