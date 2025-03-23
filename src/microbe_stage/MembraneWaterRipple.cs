using System;
using Godot;

/// <summary>
/// Manages spawning and processing ripple effect
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
 /// Number of past positions to store for the movement history
 /// </summary>
 [Export]
 public int PositionHistorySize = 14;

 /// <summary>
 /// How frequently to record a new position (in seconds)
 /// </summary>
 [Export]
 public float PositionRecordInterval = 0.02f;

 [Export]
 public float VisibilityCheckInterval = 0.2f;

 /// <summary>
 /// Distance multiplier beyond which the effect is disabled
 /// </summary>
 [Export]
 public float DistanceCullThreshold = 25.0f;

 /// <summary>
 /// Default radius if we can't determine it from the membrane
 /// </summary>
 private const float DEFAULT_MEMBRANE_RADIUS = 5.0f;

 /// <summary>
 /// Predefined LOD distance values
 /// </summary>
 private const float VERY_LOW_LOD_DISTANCE = 40.0f;
 private const float LOW_LOD_DISTANCE = 30.0f;
 private const float HIGH_LOD_DISTANCE = 15.0f;

 /// <summary>
 /// Predefined LOD subdivision values
 /// </summary>
 private const int VERY_LOW_LOD_SUBDIVISION = 40;
 private const int LOW_LOD_SUBDIVISION = 60;
 private const int MEDIUM_LOD_SUBDIVISION = 90;
 private const int HIGH_LOD_SUBDIVISION = 120;

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
 private MeshInstance3D waterPlane = null!;
 private ShaderMaterial? waterMaterial;
 private Membrane? parentMembrane;
 private PlaneMesh? planeMesh;

 /// <summary>
 /// Position tracking and effect state variables
 /// </summary>
 private Vector2[] pastPositions = null!;
 private Vector3[] membranePositionHistory = null!;
 private int currentPositionIndex = 0;
 private bool isPositionHistoryFull = false;
 private float positionRecordTimer;
 private Vector3 lastPosition;
 private Vector2 currentDirection = Vector2.Right;
 private float currentSpeed;
 private float timeAccumulator;
 private bool isCurrentlyVisible = true;
 private float visibilityCheckTimer = 0f;
 private bool isEffectEnabled = true;

 /// <summary>
 /// Camera state caching variables
 /// </summary>
 private Camera3D? currentCamera;
 private float lastCameraDistance;
 private Vector3 lastCameraPosition;
 private bool isCameraPositionValid;

 /// <summary>
 /// Predefined LOD levels for optimization
 /// </summary>
#pragma warning disable SA1201
 private enum LodLevel
 {
  VeryLow,
  Low,
  Medium,
  High,
 }

 private LodLevel currentLodLevel = LodLevel.Medium;
#pragma warning restore SA1201
#pragma warning restore CA2213

 public override void _Ready()
 {
  waterPlane = GetNode<MeshInstance3D>("WaterPlane");
  waterMaterial = waterPlane.MaterialOverride as ShaderMaterial;
  planeMesh = waterPlane.Mesh as PlaneMesh;
  pastPositions = new Vector2[PositionHistorySize];
  membranePositionHistory = new Vector3[PositionHistorySize];

  // Initialize with zero values
  for (int i = 0; i < PositionHistorySize; i++)
  {
   pastPositions[i] = Vector2.Zero;
   membranePositionHistory[i] = Vector3.Zero;
  }

  InitializeShaderParameters();
  currentCamera = GetViewport()?.GetCamera3D();
  isCameraPositionValid = false;
  waterPlane.Visible = false;
 }

 /// <summary>
 /// Initialize the effect with the parent membrane
 /// </summary>
 public void Initialize(Membrane membrane)
 {
  if (membrane == null)
   return;

  parentMembrane = membrane;
  lastPosition = parentMembrane.GlobalPosition;

  // Initialize position history with current position
  for (int i = 0; i < PositionHistorySize; i++)
  {
   membranePositionHistory[i] = lastPosition;
  }

  isEffectEnabled = true;
  waterPlane.Visible = true;
  UpdatePosition();
 }

 public override void _Process(double delta)
 {
   if (!EnableEffect || parentMembrane == null || waterPlane == null)
    return;

   // Updates camera
   visibilityCheckTimer += (float)delta;
   if (visibilityCheckTimer >= VisibilityCheckInterval)
   {
    visibilityCheckTimer = 0f;
    UpdateCameraCache();
    isCurrentlyVisible = IsMembraneVisible();

    // Toggle water plane visibility
    if (waterPlane.Visible != (isCurrentlyVisible && isEffectEnabled))
    {
     waterPlane.Visible = isCurrentlyVisible && isEffectEnabled;
    }
   }

   if (!isCurrentlyVisible || !isEffectEnabled)
    return;

   UpdatePosition();

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

   timeAccumulator += (float)delta * timeScale;
   waterMaterial?.SetShaderParameter(timeOffsetParam, timeAccumulator);

   // Update movement parameters (with reduced frequency for distant objects)
   if (currentLodLevel == LodLevel.VeryLow)
   {
    // For very distant membranes, update less frequently
    if ((int)(timeAccumulator * 10) % 3 == 0)
    {
     UpdateMovementParameters((float)delta);
    }
   }
   else
   {
    UpdateMovementParameters((float)delta);
   }
 }

 private void InitializeShaderParameters()
 {
  if (waterMaterial == null)
   return;

  waterMaterial.SetShaderParameter(waterColorParam, new Color(0, 0, 0, 0.02f));
  waterMaterial.SetShaderParameter(rippleStrengthParam, RippleStrength);
  waterMaterial.SetShaderParameter(timeOffsetParam, 0.0f);
  waterMaterial.SetShaderParameter(movementSpeedParam, 0.0f);
  waterMaterial.SetShaderParameter(movementDirectionParam, Vector2.Zero);
  waterMaterial.SetShaderParameter(phaseParam, 0.2f);
  waterMaterial.SetShaderParameter(attenuationParam, 0.9998f);
  waterMaterial.SetShaderParameter(pastPositionsParam, pastPositions);
  waterMaterial.SetShaderParameter(pastPositionsCountParam, 0);
 }

 /// <summary>
 /// Determines if the membrane is currently visible in the viewport
 /// </summary>
 private bool IsMembraneVisible()
 {
  if (parentMembrane == null)
   return false;

  var camera = currentCamera ?? GetViewport()?.GetCamera3D();
  if (camera == null)

    // If we can't determine, assume it is visible
    return true;

  // Quick distance check first
  if (isCameraPositionValid && lastCameraDistance > GetMembraneRadius() * DistanceCullThreshold)
  {
   // If very far away consider not visible
   return false;
  }

  Vector3 membranePos = parentMembrane.GlobalPosition;
  Vector2 screenPos = camera.UnprojectPosition(membranePos);
  Vector2 viewportSize = GetViewport()?.GetVisibleRect().Size ?? Vector2.One;
  float radius = GetMembraneRadius() * 1.5f;
  float thresholdX = viewportSize.X * 1.2f + radius;
  float thresholdY = viewportSize.Y * 1.2f + radius;

  // Return true if point is within expanded viewport bounds
  return screenPos.X > -thresholdX && screenPos.X < thresholdX &&
  screenPos.Y > -thresholdY && screenPos.Y < thresholdY;
 }

 /// <summary>
 /// Updates the camera position cache for distance calculations
 /// </summary>
 private void UpdateCameraCache()
 {
  currentCamera = GetViewport()?.GetCamera3D();
  if (currentCamera == null)
  {
   isCameraPositionValid = false;
   return;
  }

  Vector3 cameraPos = currentCamera.GlobalPosition;

  // Only update if camera moved significantly
  if (!isCameraPositionValid || lastCameraPosition.DistanceSquaredTo(cameraPos) > 0.25f)
  {
   lastCameraPosition = cameraPos;

   if (parentMembrane != null)
   {
    lastCameraDistance = parentMembrane.GlobalPosition.DistanceTo(cameraPos);
   }

   isCameraPositionValid = true;
   UpdateLodLevel();
  }
 }

 /// <summary>
 /// Updates the LOD level based on camera distance
 /// </summary>
 private void UpdateLodLevel()
 {
  if (!isCameraPositionValid || parentMembrane == null)
   return;

  LodLevel newLodLevel;
  float membraneRadius = GetMembraneRadius();

  // Scale the distance thresholds based on the membrane size
  float sizeScale = Mathf.Clamp(membraneRadius / 5.0f, 1.0f, 2.0f);

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

  // If LOD level changed update subdivision
  if (newLodLevel != currentLodLevel)
  {
   currentLodLevel = newLodLevel;
   UpdateMeshSubdivision();
  }
 }

 /// <summary>
 /// Updates mesh subdivision based on current LOD level
 /// </summary>
 private void UpdateMeshSubdivision()
 {
  if (planeMesh == null)
   return;

  int subdivision;

  switch (currentLodLevel)
  {
   case LodLevel.VeryLow:
    subdivision = VERY_LOW_LOD_SUBDIVISION;
    break;

   case LodLevel.Low:
    subdivision = LOW_LOD_SUBDIVISION;
    break;

   case LodLevel.Medium:
    subdivision = MEDIUM_LOD_SUBDIVISION;
    break;

   case LodLevel.High:
    subdivision = HIGH_LOD_SUBDIVISION;
    break;

   default:
    subdivision = MEDIUM_LOD_SUBDIVISION;
    break;
  }

  planeMesh.SubdivideWidth = subdivision;
  planeMesh.SubdivideDepth = subdivision;
 }

 /// <summary>
 /// Updates the position to exactly match the parent membrane's position
 /// </summary>
 private void UpdatePosition()
 {
  if (parentMembrane == null || waterPlane == null)
   return;

  Vector3 membranePos = parentMembrane.GlobalPosition;
  Vector3 waterPos = membranePos;
  waterPos.Y += VerticalOffset;
  waterPlane.GlobalPosition = waterPos;

  // Force water plane to have zero rotation (flat horizontal plane)
  waterPlane.GlobalRotation = new Vector3(0, 0, 0);

  // Update the plane's size based on the membrane radius
  if (planeMesh != null)
  {
   float membraneRadius = GetMembraneRadius();
   float desiredSize = Math.Max(18.0f, membraneRadius * 1.8f);

   if (Math.Abs(planeMesh.Size.X - desiredSize) > 0.5f)
   {
    planeMesh.Size = new Vector2(desiredSize, desiredSize);
   }
  }
 }

 /// <summary>
 /// Updates movement parameters for the effect and records position history
 /// </summary>
 private void UpdateMovementParameters(float delta)
 {
  if (waterMaterial == null || parentMembrane == null || waterPlane == null)
  return;

  // Calculates membrane movement since the last frame
  // Then determines if the motion is enough to stimulate the effect
  Vector3 currentPos = parentMembrane.GlobalPosition;
  Vector3 movement = currentPos - lastPosition;
  bool significantMovement = movement.LengthSquared() > 0.0001f;

  // Store position in circular history buffer at timed intervals
  positionRecordTimer += delta;
  if (positionRecordTimer >= PositionRecordInterval)
  {
   membranePositionHistory[currentPositionIndex] = currentPos;
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

   // update past positions array only if there's actual movement (improve)
   if (significantMovement)
   {
     waterMaterial.SetShaderParameter(pastPositionsParam, pastPositions);
    }
  }

  lastPosition = currentPos;

  // Only update movement parameters if significant movement detected
  if (significantMovement)
  {
   // Calculate movement based on LOD level
   Vector2 direction;
   float calculatedSpeed;

   if (currentLodLevel == LodLevel.VeryLow || currentLodLevel == LodLevel.Low)
   {
    // // Processes movement related parameters
    direction = new Vector2(movement.X, movement.Z).Normalized();
    calculatedSpeed = Math.Clamp(movement.Length() / delta * 2.5f, 0.05f, 1.0f);
    currentDirection = currentDirection.Lerp(direction, 0.3f);
    currentSpeed = Mathf.Lerp(currentSpeed, calculatedSpeed, 0.3f);
   }
   else
   {
    direction = new Vector2(movement.X, movement.Z).Normalized();
    float distance = movement.Length() / delta;

    if (distance > 0.0001f)
    {
     Vector3 forwardVector = parentMembrane.GlobalTransform.Basis.Z;
     Vector2 orientationDir = new Vector2(forwardVector.X, forwardVector.Z).Normalized();
     direction = direction.Lerp(orientationDir, 0.15f).Normalized();
     calculatedSpeed = Math.Clamp(distance * 3.0f, 0.05f, 1.0f);
     float directionChangeMagnitude = 1.0f - currentDirection.Dot(direction);

     if (directionChangeMagnitude > 0.2f)
     {
      calculatedSpeed = Math.Max(calculatedSpeed, 0.2f + directionChangeMagnitude * 0.3f);
     }

     currentDirection = currentDirection.Lerp(direction, 0.15f);
     currentSpeed = Mathf.Lerp(currentSpeed, calculatedSpeed, 0.15f);
    }
   }
  }
  else
  {
   // Gradually reduce speed when not moving
   currentSpeed = Mathf.Lerp(currentSpeed, 0.05f, 0.02f);
  }

  waterMaterial.SetShaderParameter(movementDirectionParam, currentDirection);
  waterMaterial.SetShaderParameter(movementSpeedParam, currentSpeed);
 }

 /// <summary>
 /// Updates the local position array for the shader
 /// based on the real position history
 /// </summary>
 private void UpdateLocalPositions()
 {
  if (waterPlane == null || membranePositionHistory == null || pastPositions == null)
   return;

  Vector3 currentPos = parentMembrane != null ? parentMembrane.GlobalPosition : Vector3.Zero;

  // Calculate local positions relative to current position
  int count = isPositionHistoryFull ? PositionHistorySize : currentPositionIndex;

  // Optimize for LOD level - use fewer samples for distant membranes
  int step = 1;
  if (currentLodLevel == LodLevel.VeryLow)
   step = 3;
  else if (currentLodLevel == LodLevel.Low)
   step = 2;

  // Access circular buffer with wrapping index
  // Transforms world positions to local XZ offsets for shader
  for (int i = 0; i < count; i += step)
  {
   int index = (currentPositionIndex - 1 - i + PositionHistorySize) % PositionHistorySize;

   if (index < 0)
   index += PositionHistorySize;
   Vector3 offset = membranePositionHistory[index] - currentPos;
   pastPositions[i / step] = new Vector2(offset.X, offset.Z);
  }

 // Fill remaining positions with zeros for lower LOD sampling
  if (step > 1)
  {
   for (int i = count / step + 1; i < count; i++)
   {
    pastPositions[i] = Vector2.Zero;
   }
  }
 }

 /// <summary>
 /// Gets the radius of the parent membrane
 /// </summary>
 private float GetMembraneRadius()
 {
  return parentMembrane == null ? DEFAULT_MEMBRANE_RADIUS :
  parentMembrane.EncompassingCircleRadius;
 }
}
