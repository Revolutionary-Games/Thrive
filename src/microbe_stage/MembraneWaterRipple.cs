using System;
using Godot;
using Godot.Collections;
using Newtonsoft.Json;

/// <summary>
///   Manages spawning and processing ripple effect for a single microbe
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
    ///   Number of past positions to store for the movement history.
    ///   WARNING: these configuration values must match what is set in the shader.
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
    ///   Time after which ripples start to fade when the cell is not moving
    /// </summary>
    [Export]
    public float StillnessFadeDelay = 1.5f;

    /// <summary>
    ///   How quickly ripples fade out when the cell is not moving
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
    ///   Maximum delta time to prevent jitter
    /// </summary>
    private const float MAX_DELTA_TIME = 0.1f;

    /// <summary>
    ///   The maximum number of past positions to store
    ///   This must match the array size in the shader
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
    private readonly StringName globalAlphaParam = new("GlobalAlpha");

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
    ///   Cached reference to the active camera
    /// </summary>
    private Camera3D? currentCamera;

    private Node3D? followTargetNode;

#pragma warning restore CA2213

    private float effectRadius = 5;

    // Position tracking and effect state variables
    private Vector2[] pastPositions = new Vector2[MAX_POSITION_HISTORY];
    private Vector3[] positionHistory = new Vector3[MAX_POSITION_HISTORY];
    private Array<Vector2> godotPastPositions = new();
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

    // Stillness tracking variables
    private float stillnessTimer;

    private float stillnessFactor;
    private bool wasMovingLastFrame;
    private float averageMovementSqr;
    private float directionChangeTimer;
    private float lastDirectionChangeTime;
    private float timeWithoutMovement;

    // Ripple effect variables
    private float targetAlpha;

    private float currentAlpha;
    private float stillnessValue = 1.0f;
    private float targetStillness = 1.0f;
    private bool isForming;
    private float formingTime;
    private float minAlpha = 0.0f;
    private float fullAlpha = 1.0f;

    // Non-linear alpha curve parameters
    private float alphaCurvePower = 2.5f;

    // Camera state caching variables
    private float lastCameraDistance;

    private Vector3 lastCameraPosition;
    private bool isCameraPositionValid;

    /// <summary>
    ///   This effect follows this world position node instead of just being attached to the parent as that reduced
    ///   visual jitter.
    /// </summary>
    [JsonIgnore]
    public Node3D? FollowTargetNode
    {
        get => followTargetNode;
        set
        {
            if (followTargetNode == value)
                return;

            if (value == null)
            {
                followTargetNode = null;
                EnableEffect = false;
                return;
            }

            if (!value.IsInsideTree())
            {
                // As we don't lazily update our data in _Process, the node to follow must be valid already
                throw new ArgumentException("Node to follow should already be in the tree");
            }

            followTargetNode = value;
            InitializePositionTracking();
        }
    }

    /// <summary>
    ///   The size of the membrane this effect is attached to and controls how large the effect is
    /// </summary>
    [Export]
    public float EffectRadius
    {
        get => effectRadius;
        set
        {
            var newValue = Math.Max(1.0f, value);

            if (Math.Abs(effectRadius - newValue) > 0.01f)
            {
                effectRadius = newValue;
                UpdateSizing();
            }
        }
    }

    public override void _Ready()
    {
        // Check for required components first - invert the if statement to reduce nesting
        if (waterPlane is not { MaterialOverride: ShaderMaterial material, Mesh: PlaneMesh mesh })
        {
            GD.PrintErr("MembraneWaterRipple: Required components missing (scene is incorrectly set up)");
            return;
        }

        waterMaterial = material;
        planeMesh = mesh;

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
        waterMaterial.SetShaderParameter(globalAlphaParam, 0.0f);
        waterPlane.Visible = false;
    }

    public override void _Process(double delta)
    {
        if (!EnableEffect || FollowTargetNode == null)
        {
            if (waterPlane.Visible)
                waterPlane.Visible = false;

            return;
        }

        // Clamp delta to further prevent jitter
        float clampedDelta = MathF.Min((float)delta, MAX_DELTA_TIME);

        // Fetch new camera reference if needed
        if (currentCamera == null || !currentCamera.IsCurrent())
            currentCamera = GetViewport().GetCamera3D();

        // Updates camera and visibility
        visibilityCheckTimer += clampedDelta;
        if (visibilityCheckTimer >= VisibilityCheckInterval)
        {
            visibilityCheckTimer = 0.0f;
            UpdateCameraPositionCache();
            isCurrentlyVisible = IsVisible();

            // Only show the plane if there's actual ripple activity
            bool shouldShow = isCurrentlyVisible && EnableEffect && ShouldRipplesBeVisible();
            waterPlane.Visible = shouldShow;
        }

        if (!isCurrentlyVisible || !EnableEffect)
            return;

        if (FollowTargetNode.IsInsideTree())
        {
            // Optimize position calculation by reusing the base vector and only adding offset
            waterPlane.GlobalTransform = new Transform3D(Basis.Identity,
                FollowTargetNode.GlobalPosition + new Vector3(0, VerticalOffset, 0));
        }
        else
        {
            GD.PrintErr("MembraneWaterRipple: Target node not inside tree");
        }

        UpdateRippleEffect(clampedDelta);
        float timeScale = CalculateTimeScale();
        timeAccumulator += clampedDelta * timeScale;
        waterMaterial.SetShaderParameter(timeOffsetParam, timeAccumulator);
        UpdateMovementParameters(clampedDelta);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            timeOffsetParam.Dispose();
            movementDirectionParam.Dispose();
            movementSpeedParam.Dispose();
            waterColorParam.Dispose();
            rippleStrengthParam.Dispose();
            phaseParam.Dispose();
            attenuationParam.Dispose();
            pastPositionsParam.Dispose();
            pastPositionsCountParam.Dispose();
            stillnessFactorParam.Dispose();
            membraneRadiusParam.Dispose();
            globalAlphaParam.Dispose();
        }

        base.Dispose(disposing);
    }

    private void InitializePositionTracking()
    {
        // Initialize the position history arrays
        godotPastPositions.Clear();
        for (int i = 0; i < MAX_POSITION_HISTORY; ++i)
        {
            godotPastPositions.Add(Vector2.Zero);

            // The C# native arrays are implicitly initialized
        }

        // Initialize position history with the default position
        lastPosition = Vector3.Zero;
        previousPosition = Vector3.Zero;

        for (int i = 0; i < MAX_POSITION_HISTORY; ++i)
        {
            positionHistory[i] = lastPosition;
            pastPositions[i] = Vector2.Zero;
        }

        if (FollowTargetNode != null)
        {
            lastPosition = FollowTargetNode.GlobalPosition;
            previousPosition = lastPosition;

            // Update position in world space
            waterPlane.GlobalTransform = new Transform3D(Basis.Identity,
                lastPosition + new Vector3(0, VerticalOffset, 0));

            // Make visible if not already
            if (isCurrentlyVisible && EnableEffect)
            {
                waterPlane.Visible = true;
            }
        }
        else
        {
            GD.PrintErr("MembraneWaterRipple: Target node not set");
        }

        // Reset state tracking
        currentPositionIndex = 0;
        isPositionHistoryFull = false;

        // Start forming state
        isForming = true;
        formingTime = 0.0f;
        currentAlpha = 0.0f;
        targetAlpha = 0.0f;
        stillnessValue = 1.0f;
        targetStillness = 1.0f;
    }

    private void UpdateSizing()
    {
        // Set radius and update mesh
        waterMaterial.SetShaderParameter(membraneRadiusParam, EffectRadius);
        waterMaterial.SetShaderParameter(waterColorParam, new Color(0, 0, 0, 0.0f));
        waterMaterial.SetShaderParameter(stillnessFactorParam, 1.0f);

        float desiredSize = Math.Max(18.0f, EffectRadius * 2.2f);
        if (Math.Abs(planeMesh.Size.X - desiredSize) > 0.5f)
        {
            planeMesh.Size = new Vector2(desiredSize, desiredSize);
        }
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
    ///   Non-linear alpha scaling function
    /// </summary>
    private float ApplyNonLinearAlphaScaling(float linearAlpha)
    {
        // Allow true zero for complete transparency
        if (linearAlpha <= 0.0001f)
            return 0.0f;

        // Apply power curve for smooth transitions
        float curved = MathF.Pow(linearAlpha, alphaCurvePower);

        // Ensure we use the full range from 0 to 1
        return Math.Clamp(curved, 0.0f, 1.0f);
    }

    /// <summary>
    ///   Check if ripples should be visible at all
    /// </summary>
    private bool ShouldRipplesBeVisible()
    {
        // Don't show during the formation phase if alpha is too low
        if (isForming && currentAlpha < 0.01f)
            return false;

        // Check if we have any movement history
        if (!isPositionHistoryFull && currentPositionIndex == 0)
            return false;

        // Check if we're in complete stillness
        if (stillnessFactor >= 0.99f)
            return false;

        // Check if alpha is effectively zero
        if (currentAlpha < 0.005f)
            return false;

        return true;
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
            else
            {
                // Gradually fade in during formation
                float formProgress = formingTime / RippleFormationDelay;
                targetAlpha = fullAlpha * formProgress * 0.5f;
                currentAlpha = Mathf.Lerp(currentAlpha, targetAlpha, delta * fadeInSpeed);

                // Update shader with forming alpha
                float formingScaledAlpha = ApplyNonLinearAlphaScaling(currentAlpha);
                waterMaterial.SetShaderParameter(globalAlphaParam, formingScaledAlpha);
                waterMaterial.SetShaderParameter(waterColorParam, new Color(0, 0, 0, 0.02f));
                waterMaterial.SetShaderParameter(stillnessFactorParam, 1.0f - formProgress);
            }

            return;
        }

        // Set target alpha based on movement
        targetAlpha = wasMovingLastFrame ? fullAlpha : minAlpha;
        targetStillness = wasMovingLastFrame ? 0.0f : 1.0f;

        // Apply fade delay when stopping
        if (!wasMovingLastFrame && stillnessTimer > StillnessFadeDelay)
        {
            targetAlpha = minAlpha;
            targetStillness = 1.0f;
        }

        // Smooth interpolation for fade-in/out
        float fadeSpeed = currentAlpha < targetAlpha ? fadeInSpeed : fadeOutSpeed;
        currentAlpha = Mathf.Lerp(currentAlpha, targetAlpha, delta * fadeSpeed);
        stillnessValue = Mathf.Lerp(stillnessValue, targetStillness, delta * 3.0f);

        // Apply non-linear scaling
        float scaledAlpha = ApplyNonLinearAlphaScaling(currentAlpha);

        // Update shader parameters with the global alpha multiplier
        waterMaterial.SetShaderParameter(globalAlphaParam, scaledAlpha);
        waterMaterial.SetShaderParameter(waterColorParam, new Color(0, 0, 0, 0.02f));
        waterMaterial.SetShaderParameter(stillnessFactorParam, stillnessValue);

        // Hide the plane entirely when alpha is effectively zero
        if (scaledAlpha < 0.001f && isCurrentlyVisible)
        {
            waterPlane.Visible = false;
        }
        else if (scaledAlpha >= 0.001f && isCurrentlyVisible && EnableEffect)
        {
            waterPlane.Visible = true;
        }
    }

    /// <summary>
    ///   Check if the effect is currently visible
    ///   This is used to optimize performance by only processing effects that are visible to the camera,
    ///   going beyond normal frustum culling for more expensive effects
    /// </summary>
    private bool IsVisible()
    {
        if (FollowTargetNode == null || currentCamera == null)
            return false;

        // TODO: shouldn't this allow extra space based on how big the water plane is currently so as not to frustrum
        // cull too early?
        return GetViewport().GetVisibleRect()
            .HasPoint(currentCamera.UnprojectPosition(FollowTargetNode.GlobalPosition));
    }

    /// <summary>
    ///   Updates the camera position cache and detail level
    /// </summary>
    private void UpdateCameraPositionCache()
    {
        if (currentCamera == null || FollowTargetNode == null)
        {
            isCameraPositionValid = false;
            return;
        }

        Vector3 cameraPos = currentCamera.GlobalPosition;

        // Only update if the camera moved significantly
        if (!isCameraPositionValid || lastCameraPosition.DistanceSquaredTo(cameraPos) > 0.25f)
        {
            lastCameraPosition = cameraPos;
            lastCameraDistance = FollowTargetNode.GlobalPosition.DistanceTo(cameraPos);
            isCameraPositionValid = true;
        }
    }

    /// <summary>
    ///   Updates movement parameters for the effect and records position history
    /// </summary>
    private void UpdateMovementParameters(float delta)
    {
        if (FollowTargetNode == null)
            return;

        // Store previous position before calculating new movement
        previousPosition = lastPosition;

        // Calculates movement since the last frame
        var currentPos = FollowTargetNode.GlobalPosition;
        var movement = currentPos - lastPosition;
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

        // Store position in the circular history buffer at timed intervals
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

        if (significantMovement)
        {
            Vector2 direction;
            float calculatedSpeed;
            if (lastCameraDistance > 80.0f)
            {
                direction = new Vector2(movement.X, movement.Z).Normalized();
                calculatedSpeed = Math.Clamp(movement.Length() / delta * 2.5f, 0.05f, 1.0f);
                currentDirection = currentDirection.Lerp(direction, 0.4f);
                currentSpeed = Mathf.Lerp(currentSpeed, calculatedSpeed, 0.3f);
            }
            else
            {
                float distance = movement.Length() / delta;

                if (distance > 0.0001f)
                {
                    direction = new Vector2(movement.X, movement.Z).Normalized();

                    var forwardVector = FollowTargetNode.GlobalTransform.Basis.Z;
                    var orientationDir = new Vector2(forwardVector.X, forwardVector.Z).Normalized();
                    direction = direction.Lerp(orientationDir, 0.15f).Normalized();
                    float sizeAdjustedDistance = distance / Math.Max(1.0f, EffectRadius / 5.0f);
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

            // If the direction hasn't changed for a while, but we're moving, nudge it
            if (directionChangeTimer > 1.0f && wasMovingLastFrame)
            {
                // Force direction update if it's been frozen
                var forwardVector = FollowTargetNode.GlobalTransform.Basis.Z;
                var orientationDir = new Vector2(forwardVector.X, forwardVector.Z).Normalized();
                currentDirection = currentDirection.Lerp(orientationDir, 0.05f).Normalized();
                directionChangeTimer = 0.0f;
            }
        }

        // Make sure the direction is always normalized
        if (currentDirection.LengthSquared() < 0.01f)
        {
            currentDirection = lastValidDirection;
        }

        // TODO: shader parameters should not be set again if they haven't changed for performance reasons
        waterMaterial.SetShaderParameter(movementDirectionParam, currentDirection);
        waterMaterial.SetShaderParameter(movementSpeedParam, currentSpeed);
    }

    /// <summary>
    ///   Updates the local position array for the shader
    ///   based on the real position history
    /// </summary>
    private void UpdateLocalPositions()
    {
        if (FollowTargetNode == null)
            return;

        var currentPos = FollowTargetNode.GlobalPosition;

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

        // Reset all positions to zero first to ensure a clean state
        var pastPositionCount = godotPastPositions.Count;

        // TODO: optimize this by only resetting the ones that are actually not updated later
        for (int i = 0; i < MAX_POSITION_HISTORY && i < pastPositionCount; ++i)
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
            var offset = positionHistory[index] - currentPos;
            var position = new Vector2(offset.X, offset.Z);

            int targetIndex = i / step;
            if (targetIndex < MAX_POSITION_HISTORY && targetIndex < pastPositionCount)
            {
                pastPositions[targetIndex] = position;
                godotPastPositions[targetIndex] = position;
                targetPositionsCount = targetIndex + 1;
            }
            else
            {
                GD.PrintErr("MembraneWaterRipple: Calculated Position index out of bounds");
            }
        }

        // Update the count of non-zero entries in the array
        waterMaterial.SetShaderParameter(pastPositionsCountParam, targetPositionsCount);
    }
}
