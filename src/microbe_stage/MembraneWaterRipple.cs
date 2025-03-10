using System;
using System.Reflection;
using Godot;

/// <summary>
///   Manages spawning and processing ripple effect
/// </summary>
public partial class MembraneWaterRipple : Node
{
    [Export]
    public bool EnableEffect = true;

    [Export]
    public float RippleStrength = 0.8f;

    [Export]
    public Color WaterColor = new(0.0f, 0.0f, 0.0f, 0.02f);

    [Export]
    public float WaterSize = 18.0f; // Base water size

    [Export]
    public float Phase = 0.2f;

    /*Controls how quickly ripples dissipate
    higher values = less viscous appearance*/

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

    [Export]
    public int MaxMeshSubdivision = 100; // Reduced for better performane can be increased for a more detailed effect

    [Export]
    public float VerticalOffset = -0.15f; // How far below the membrane the water plane should be

    // Default radius if we can't determine it from the membrane
    private const float DEFAULT_MEMBRANE_RADIUS = 5.0f;
    private MeshInstance3D? waterPlane;
    private ShaderMaterial? waterMaterial;
    private Membrane? parentMembrane;

    // Trail tracking variables
    private Vector3 lastPosition;
    private Vector3 previousPosition;
    private float lastMovementTime;
    private Vector2 currentDirection = Vector2.Right;
    private float currentSpeed;

    // State tracking
    private float lastScaleCheckTime;
    private float lastCheckedMembraneRadius;
    private bool isSetup;

    // Called when the node enters the scene tree for the first time
    public override void _Ready()
    {
        // Initialize the effect
        SetProcess(true);
        SetPhysicsProcess(true);

        // Find the parent membrane
        parentMembrane = GetParent<Membrane>();
        if (parentMembrane == null)
        {
            GD.PrintErr("MembraneWaterRipple must be a child of a Membrane");
            QueueFree();
            return;
        }

        // Initialize position tracking for movement calculation
        lastPosition = parentMembrane.GlobalPosition;
        previousPosition = lastPosition;
        lastMovementTime = Time.GetTicksMsec() / 1000.0f;
        CreateWaterPlane();

        // Set initial position
        UpdatePosition();
    }

    // Called every frame for visual updates
    public override void _Process(double delta)
    {
        if (!EnableEffect || !isSetup || parentMembrane == null)
            return;

        try
        {
            // Update position every frame to ensure perfect alignment
            UpdatePosition();

            // Update shader time with exact original timing
            if (waterMaterial != null)
            {
                float currentTime = Time.GetTicksMsec() / 1000.0f;
                waterMaterial.SetShaderParameter("time_offset", currentTime);
            }
        }
        catch (Exception e)
        {
            GD.PrintErr("Error in water ripple process: " + e.Message);
        }
    }

    // Called on physics process for movement and simulation updates
    public override void _PhysicsProcess(double delta)
    {
        if (!EnableEffect || !isSetup || parentMembrane == null || waterMaterial == null)
            return;

        try
        {
            float currentTime = Time.GetTicksMsec() / 1000.0f;

            // Check if membrane size has changed
            if (DynamicScaling && currentTime - lastScaleCheckTime > ScaleCheckInterval)
            {
                UpdateMeshScale();
                lastScaleCheckTime = currentTime;
            }

            // Update movement parameters
            UpdateMovementParameters();
        }
        catch (Exception e)
        {
            GD.PrintErr("Error in water ripple physics process: " + e.Message);
        }
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            // Dispose managed resources
            if (parentMembrane is IDisposable disposableMembrane)
            {
                disposableMembrane.Dispose();
            }

            waterMaterial?.Dispose();

            if (waterPlane != null)
            {
                // For C# compiler warning satisfaction - call Dispose explicitly if it implements IDisposable
                // This needs to be suppressed with pragma because it will cause an ObjectDisposedException
                // when QueueFree is called, but we need it to satisfy the compiler
#pragma warning disable CS8600, CS8602
                ((IDisposable)waterPlane).Dispose();
#pragma warning restore CS8600, CS8602

                // We still need to queue the node for freeing in the Godot engine
                try
                {
                    waterPlane.QueueFree();
                }
                catch (ObjectDisposedException)
                {
                    // Ignore ObjectDisposedException - this is expected
                    // because we've already disposed the object above
                }
            }
        }

        base.Dispose(disposing);
    }

    // Creates the water plane mesh
    private void CreateWaterPlane()
    {
        try
        {
            // Create the mesh instance
            waterPlane = new MeshInstance3D();
            AddChild(waterPlane);

            // Get membrane size
            float membraneRadius = GetMembraneRadius();
            lastCheckedMembraneRadius = membraneRadius;

            // Create mesh with appropriate size
            // NOLINT
            float meshSize = MathF.Max(WaterSize, membraneRadius * RadiusMultiplier);

            PlaneMesh planeMesh = new PlaneMesh();
            planeMesh.Size = new Vector2(meshSize, meshSize);

            // Optimize subdivision based on size
            // NOLINT
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
        catch (Exception e)
        {
            GD.PrintErr("Error creating water plane: " + e.Message);
        }
    }

    // Sets up the material for the water effect
    private void SetupMaterial()
    {
        try
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
            waterMaterial.SetShaderParameter("water_color", WaterColor);
            waterMaterial.SetShaderParameter("ripple_strength", RippleStrength);
            waterMaterial.SetShaderParameter("time_offset", 0.0f);
            waterMaterial.SetShaderParameter("movement_speed", 0.0f);
            waterMaterial.SetShaderParameter("movement_direction", Vector2.Zero);
            waterMaterial.SetShaderParameter("phase", Phase);
            waterMaterial.SetShaderParameter("attenuation", Attenuation);

            // Apply material
            waterPlane.MaterialOverride = waterMaterial;
        }
        catch (Exception e)
        {
            GD.PrintErr("Error setting up material: " + e.Message);
        }
    }

    // Updates the mesh scale based on membrane size
    private void UpdateMeshScale()
    {
        try
        {
            float currentRadius = GetMembraneRadius();

            // Only update if size changed significantly
            // NOLINT
            if (MathF.Abs(currentRadius - lastCheckedMembraneRadius) > 0.5f)
            {
                // NOLINT
                float meshSize = MathF.Max(WaterSize, currentRadius * RadiusMultiplier);

                // Update the mesh
                if (waterPlane?.Mesh is PlaneMesh planeMesh)
                {
                    planeMesh.Size = new Vector2(meshSize, meshSize);

                    // Optimize subdivision based on size
                    // NOLINT
                    int subdivision = Math.Clamp((int)(meshSize * 6), 60, MaxMeshSubdivision);
                    planeMesh.SubdivideWidth = subdivision;
                    planeMesh.SubdivideDepth = subdivision;
                }

                // Stores current radius
                lastCheckedMembraneRadius = currentRadius;
            }
        }
        catch (Exception e)
        {
            GD.PrintErr("Error updating mesh scale: " + e.Message);
        }
    }

    // Updates the position to match the parent membrane
    private void UpdatePosition()
    {
        try
        {
            if (parentMembrane == null || waterPlane == null)
                return;

            // Create a fixed parent for the water plane if it doesn't exist
            if (waterPlane.GetParent() == null)
                AddChild(waterPlane);

            // Set the water plane's position to match the membrane's position
            waterPlane.GlobalPosition = parentMembrane.GlobalPosition;
            Vector3 pos = waterPlane.GlobalPosition;
            pos.Y += VerticalOffset;
            waterPlane.GlobalPosition = pos;
            waterPlane.GlobalRotation = Vector3.Zero;
        }
        catch (Exception e)
        {
            GD.PrintErr("Error updating position: " + e.Message);
        }
    }

    // Updates movement parameters for the effect
    private void UpdateMovementParameters()
    {
        try
        {
            if (waterMaterial == null || parentMembrane == null || waterPlane == null)
                return;

            // Get current position
            Vector3 currentPos = parentMembrane.GlobalPosition;
            float currentTime = Time.GetTicksMsec() / 1000.0f;
            float timeSinceLastMovement = currentTime - lastMovementTime;
            Vector3 movement = timeSinceLastMovement > 0.01f ?
                currentPos - lastPosition :
                lastPosition - previousPosition;

            // Only update after minimum time to avoid jitter
            if (timeSinceLastMovement > 0.01f)
            {
                previousPosition = lastPosition;
                lastPosition = currentPos;
                lastMovementTime = currentTime;
            }
            else
            {
                movement = lastPosition - previousPosition;
            }

            // Only update if significant movement
            if (movement.LengthSquared() > 0.0001f)
            {
                // Get horizontal movement direction (x,z)
                Vector2 direction = new Vector2(movement.X, movement.Z);
                float distance = direction.Length();

                if (distance > 0.0001f)
                {
                    // Normalize direction
                    direction /= distance;

                    currentDirection = currentDirection.Lerp(direction, 0.3f);

                    float speed = Math.Clamp(distance * 3.5f, 0.0f, 1.0f);

                    currentSpeed = Mathf.Lerp(currentSpeed, speed, 0.3f);
                }
            }
            else
            {
                // Gradually reduce speed when not moving
                currentSpeed = Mathf.Lerp(currentSpeed, 0.0f, 0.1f);
            }

            // Update shader parameters
            waterMaterial.SetShaderParameter("movement_direction", currentDirection);
            waterMaterial.SetShaderParameter("movement_speed", currentSpeed);
        }
        catch (Exception e)
        {
            GD.PrintErr("Error updating movement parameters: " + e.Message);
        }
    }

    private float GetMembraneRadius()
    {
        try
        {
            if (parentMembrane == null)
                return DEFAULT_MEMBRANE_RADIUS;

            // Tries to access radius directly if available
            foreach (var propertyName in new[] { "Radius", "radius", "_radius" })
            {
                var property = parentMembrane.GetType().GetProperty(propertyName,
                    BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

                if (property != null)
                {
                    var value = property.GetValue(parentMembrane);
                    if (value != null)
                    {
                        return Convert.ToSingle(value);
                    }
                }

                var field = parentMembrane.GetType().GetField(propertyName,
                    BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

                if (field != null)
                {
                    var value = field.GetValue(parentMembrane);
                    if (value != null)
                    {
                        return Convert.ToSingle(value);
                    }
                }
            }

            // Try common getter methods
            foreach (var methodName in new[] { "GetRadius", "get_Radius", "GetSize", "get_Size" })
            {
                var method = parentMembrane.GetType().GetMethod(methodName,
                    BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

                if (method != null && method.GetParameters().Length == 0)
                {
                    var value = method.Invoke(parentMembrane, null);
                    if (value != null)
                    {
                        return Convert.ToSingle(value);
                    }
                }
            }

            // Fallback: estimate from scale if membrane is Node3D
            if (parentMembrane is Node3D node3D)
            {
                return node3D.Scale.X * 2.0f;
            }

            return DEFAULT_MEMBRANE_RADIUS;
        }
        catch (Exception)
        {
            return DEFAULT_MEMBRANE_RADIUS;
        }
    }
}
