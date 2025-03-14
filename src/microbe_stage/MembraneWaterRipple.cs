using System;
using Godot;

/// <summary>
///   Manages spawning and processing ripple effect
/// </summary>
#pragma warning disable CA1816
public partial class MembraneWaterRipple : Node
{
    [Export]
    public bool EnableEffect = true;

    [Export]
    public float RippleStrength = 0.8f;

    [Export]
    public Color WaterColor = new(0.0f, 0.0f, 0.0f, 0.02f);

    [Export]
    public float WaterSize = 18.0f;

    [Export]
    public float Phase = 0.2f;

    /// <summary>
    ///   Controls how quickly ripples dissipate
    ///   higher values = less viscous appearance
    /// </summary>
    [Export]
    public float Attenuation = 0.998f;

    [Export]
    public string ShaderPath = "res://shaders/MembraneWaterRipple.gdshader";

    [Export]
    public bool DynamicScaling = true;

    [Export]
    public float ScaleCheckInterval = 0.5f;

    [Export]
    public float RadiusMultiplier = 2.5f;

    /// <summary>
    ///   Increase it for a more detailed effect
    /// </summary>
    [Export]
    public int MaxMeshSubdivision = 100;

    /// <summary>
    ///   How far below the membrane the water plane should be positioned
    /// </summary>
    [Export]
    public float VerticalOffset = -0.05f;

    /// <summary>
    ///   Default radius if we can't determine it from the membrane
    /// </summary>
    private const float DEFAULT_MEMBRANE_RADIUS = 5.0f;

#pragma warning disable CA2213
    private readonly StringName timeOffsetParam = new("TimeOffset");
    private readonly StringName movementDirectionParam = new("MovementDirection");
    private readonly StringName movementSpeedParam = new("MovementSpeed");
    private readonly StringName waterColorParam = new("WaterColor");
    private readonly StringName rippleStrengthParam = new("RippleStrength");
    private readonly StringName phaseParam = new("Phase");
    private readonly StringName attenuationParam = new("Attenuation");
    private MeshInstance3D? waterPlane;
    private ShaderMaterial? waterMaterial;
    private Membrane? parentMembrane;
#pragma warning restore CA2213

    // Trail tracking variables
    private Vector3 lastPosition;
    private Vector3 previousPosition;
    private Vector2 currentDirection = Vector2.Right;
    private float currentSpeed;
    private float timeAccumulator;

    // State tracking
    private float lastScaleCheckTime;
    private float lastCheckedMembraneRadius;
    private bool isSetup;

    /// <summary>
    ///   Called when the node enters the scene tree for the first time
    /// </summary>
    public override void _Ready()
    {
        // Find the parent membrane
        parentMembrane = GetParent<Membrane>();
        if (parentMembrane == null)
        {
            QueueFree();
            return;
        }

        // Initialize position tracking for movement calculation
        lastPosition = parentMembrane.GlobalPosition;
        previousPosition = lastPosition;

        CreateWaterPlane();

        // Set initial position
        UpdatePosition();
    }

    /// <summary>
    ///   Called every frame
    /// </summary>
    public override void _Process(double delta)
    {
        if (!EnableEffect || !isSetup || parentMembrane == null)
            return;

        // Update position every frame to ensure perfect alignment
        UpdatePosition();

        // Update time accumulator for animation with scale based on speed
        // The animation only accelerates when the membrane speeds up
        // and returns to a slower speed when the membrane slows down
        if (currentSpeed > 0.1f)
        {
            // Accelerate the effect when membrane is moving faster
            float timeScale = 1.0f + currentSpeed * 0.3f;
            timeAccumulator += (float)delta * timeScale;
        }
        else
        {
            // Use normal speed when membrane is moving slowly or stopped
            timeAccumulator += (float)delta;
        }

        waterMaterial?.SetShaderParameter(timeOffsetParam, timeAccumulator);

        // Check if membrane size has changed periodically
        lastScaleCheckTime += (float)delta;
        if (DynamicScaling && lastScaleCheckTime > ScaleCheckInterval)
        {
            UpdateMeshScale();
            lastScaleCheckTime = 0;
        }

        // Update movement parameters
        UpdateMovementParameters((float)delta);
    }

    /// <summary>
    ///   Creates the water plane mesh
    /// </summary>
    private void CreateWaterPlane()
    {
        // Create the mesh instance and initializes it
        waterPlane = new MeshInstance3D();
        AddChild(waterPlane);
        float membraneRadius = GetMembraneRadius();
        lastCheckedMembraneRadius = membraneRadius;
        float meshSize = Math.Max(WaterSize, membraneRadius * RadiusMultiplier);

        var planeMesh = new PlaneMesh
        {
            Size = new Vector2(meshSize, meshSize),
        };

        // Optimize subdivision based on size
        int subdivision = Math.Clamp((int)(meshSize * 6), 60, MaxMeshSubdivision);
        planeMesh.SubdivideWidth = subdivision;
        planeMesh.SubdivideDepth = subdivision;

        // Set the mesh
        waterPlane.Mesh = planeMesh;

        // Create and setup material
        SetupMaterial();
        waterPlane.CastShadow = GeometryInstance3D.ShadowCastingSetting.Off;

        // Position the water plane initially
        UpdatePosition();

        isSetup = true;
    }

    /// <summary>
    ///   Sets up the material for the water effect
    /// </summary>
    private void SetupMaterial()
    {
        if (waterPlane == null)
            return;

        // Create material
        waterMaterial = new ShaderMaterial();

        // Load the shader
        var shader = GD.Load<Shader>(ShaderPath);
        if (shader == null)
        {
            GD.PrintErr("Could not load shader from: " + ShaderPath);
            return;
        }

        waterMaterial.Shader = shader;

        // Set shader parameters
        waterMaterial.SetShaderParameter(waterColorParam, WaterColor);
        waterMaterial.SetShaderParameter(rippleStrengthParam, RippleStrength);
        waterMaterial.SetShaderParameter(timeOffsetParam, 0.0f);
        waterMaterial.SetShaderParameter(movementSpeedParam, 0.0f);
        waterMaterial.SetShaderParameter(movementDirectionParam, Vector2.Zero);
        waterMaterial.SetShaderParameter(phaseParam, Phase);
        waterMaterial.SetShaderParameter(attenuationParam, Attenuation);

        // Apply material
        waterPlane.MaterialOverride = waterMaterial;
    }

    /// <summary>
    ///   Updates the mesh scale based on membrane size
    /// </summary>
    private void UpdateMeshScale()
    {
        float currentRadius = GetMembraneRadius();

        // Only update if size changed significantly
        if (Math.Abs(currentRadius - lastCheckedMembraneRadius) > 0.5f)
        {
            float meshSize = Math.Max(WaterSize, currentRadius * RadiusMultiplier);

            // Update the mesh
            if (waterPlane?.Mesh is PlaneMesh planeMesh)
            {
                planeMesh.Size = new Vector2(meshSize, meshSize);

                // Optimize subdivision based on size
                int subdivision = Math.Clamp((int)(meshSize * 6), 60, MaxMeshSubdivision);
                planeMesh.SubdivideWidth = subdivision;
                planeMesh.SubdivideDepth = subdivision;
            }

            // Stores current radius
            lastCheckedMembraneRadius = currentRadius;
        }
    }

    /// <summary>
    ///   Updates the position to match the parent membrane
    /// </summary>
    private void UpdatePosition()
    {
        if (parentMembrane == null || waterPlane == null)
            return;

        // Create a fixed parent for the water plane if it doesn't exist
        if (waterPlane.GetParent() == null)
            AddChild(waterPlane);

        // Set the water plane's position directly
        Vector3 pos = parentMembrane.GlobalPosition;
        pos.Y += VerticalOffset;
        waterPlane.GlobalPosition = pos;
        waterPlane.GlobalRotation = Vector3.Zero;
    }

    /// <summary>
    ///   Updates movement parameters for the effect
    /// </summary>
    private void UpdateMovementParameters(float delta)
    {
        if (waterMaterial == null || parentMembrane == null || waterPlane == null)
            return;

        // Get current position
        Vector3 currentPos = parentMembrane.GlobalPosition;

        // Calculate movement with delta time
        Vector3 movement = currentPos - lastPosition;

        // Store previous position for next frame
        previousPosition = lastPosition;
        lastPosition = currentPos;

        // Only update if significant movement
        if (movement.LengthSquared() > 0.0001f)
        {
            // Get horizontal movement direction (x,z)
            Vector2 direction = new(movement.X, movement.Z);
            float distance = direction.Length() / delta;

            if (distance > 0.0001f)
            {
                // Normalize direction
                direction = direction.Normalized();

                // Smoothly interpolate the direction
                currentDirection = currentDirection.Lerp(direction, 0.3f);

                // Calculate speed - using original scaling factor
                float speed = Math.Clamp(distance * 3.5f, 0.0f, 1.0f);

                // Smoothly interpolate speed with the original interpolation factor
                currentSpeed = Mathf.Lerp(currentSpeed, speed, 0.3f);
            }
        }
        else
        {
            // Gradually reduce speed when not moving
            // Use an extremely small interpolation factor for very gradual fade-out
            currentSpeed = Mathf.Lerp(currentSpeed, 0.0f, 0.02f);
        }

        // Update shader parameters using StringNames for better performance
        waterMaterial.SetShaderParameter(movementDirectionParam, currentDirection);
        waterMaterial.SetShaderParameter(movementSpeedParam, currentSpeed);
    }

    /// <summary>
    ///   Gets the radius of the parent membrane
    /// </summary>
    private float GetMembraneRadius()
    {
        if (parentMembrane == null)
            return DEFAULT_MEMBRANE_RADIUS;

        return parentMembrane.EncompassingCircleRadius;
    }
}
#pragma warning restore CA1816
