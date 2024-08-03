using System;
using Godot;
using Newtonsoft.Json;

/// <summary>
///   Camera script for the microbe stage and the cell editor
/// </summary>
public partial class MicrobeCamera : Camera3D, IGodotEarlyNodeResolve, ISaveLoadedTracked, IGameCamera
{
    /// <summary>
    ///   Automatically process the camera position while game is paused (used to still process zooming easily while
    ///   microbe stage is paused)
    /// </summary>
    [Export]
    public bool AutoProcessWhilePaused;

    /// <summary>
    ///   How fast the camera zooming is
    /// </summary>
    [Export]
    [JsonProperty]
    public float ZoomSpeed = 1.4f;

    /// <summary>
    ///   The height at which the camera starts at
    /// </summary>
    [Export]
    [JsonProperty]
    public float DefaultCameraHeight = 40.0f;

    /// <summary>
    ///   Min height the camera can be scrolled to
    /// </summary>
    [Export]
    [JsonProperty]
    public float MinCameraHeight = 3.0f;

    /// <summary>
    ///   Maximum height the camera can be scrolled to
    /// </summary>
    [Export]
    [JsonProperty]
    public float MaxCameraHeight = 80.0f;

    [Export]
    [JsonProperty]
    public float InterpolateSpeed = 0.3f;

    [Export]
    [JsonProperty]
    public float InterpolateZoomSpeed = 0.3f;

    /// <summary>
    ///   Now required with native physics to ensure that there's no occasional hitching with the camera
    /// </summary>
    [Export]
    [JsonProperty]
    public float SnapWithDistanceLessThan = 7.0f;

    /// <summary>
    ///   How many units of light level can change per second
    /// </summary>
    [Export]
    public float LightLevelInterpolateSpeed = 4;

    private readonly StringName lightLevelParameter = new("lightLevel");

#pragma warning disable CA2213

    /// <summary>
    ///   Background plane that is moved farther away from the camera when zooming out
    /// </summary>
    [JsonIgnore]
    private Node3D? backgroundPlane;

    [JsonIgnore]
    private GpuParticles3D? backgroundParticles;

    private ShaderMaterial materialToUpdate = null!;
#pragma warning restore CA2213

    /// <summary>
    ///   Used to manually tween the light level to the target value
    /// </summary>
    private float lastSetLightLevel = 1;

    private Vector3 cursorWorldPos = new(0, 0, 0);
    private bool cursorDirty = true;

    [JsonProperty]
    private float lightLevel = 1.0f;

    [Signal]
    public delegate void OnZoomChangedEventHandler(float zoom);

    /// <summary>
    ///   How high the camera is above the followed object
    /// </summary>
    public float CameraHeight { get; set; }

    /// <summary>
    ///   If true zoom speed is adjusted based on the elapsed time.
    /// </summary>
    /// <remarks>
    ///   <para>
    ///     At least in my experience this actually makes the zooming feel more laggy -hhyyrylainen
    ///   </para>
    /// </remarks>
    public bool FramerateAdjustZoomSpeed { get; set; }

    /// <summary>
    ///   Current relative light level for the camera (between 0 and 1).
    /// </summary>
    [JsonIgnore]
    public float LightLevel
    {
        get => lightLevel;
        set
        {
            if (Math.Abs(lightLevel - value) < MathUtils.EPSILON)
                return;

            lightLevel = value;
        }
    }

    /// <summary>
    ///   Returns the position the player is pointing to with their cursor
    /// </summary>
    [JsonIgnore]
    public Vector3 CursorWorldPos
    {
        get
        {
            if (cursorDirty)
                UpdateCursorWorldPos();
            return cursorWorldPos;
        }
        private set => cursorWorldPos = value;
    }

    public bool NodeReferencesResolved { get; private set; }

    public bool IsLoadedFromSave { get; set; }

    public override void _Ready()
    {
        var material = GetNode<CsgMesh3D>("BackgroundPlane").Material;
        if (material == null)
        {
            GD.PrintErr("MicrobeCamera didn't find material to update");
            return;
        }

        materialToUpdate = (ShaderMaterial)material;

        ResolveNodeReferences();

        if (!IsLoadedFromSave)
            ResetHeight();

        UpdateBackgroundVisibility();

        ProcessMode = ProcessModeEnum.Always;
    }

    public void ResolveNodeReferences()
    {
        if (NodeReferencesResolved)
            return;

        NodeReferencesResolved = true;

        if (HasNode("BackgroundPlane"))
            backgroundPlane = GetNode<Node3D>("BackgroundPlane");
    }

    public override void _EnterTree()
    {
        base._EnterTree();
        InputManager.RegisterReceiver(this);

        Settings.Instance.DisplayBackgroundParticles.OnChanged += OnDisplayBackgroundParticlesChanged;
    }

    public override void _ExitTree()
    {
        base._ExitTree();
        InputManager.UnregisterReceiver(this);

        Settings.Instance.DisplayBackgroundParticles.OnChanged -= OnDisplayBackgroundParticlesChanged;
    }

    public override void _Process(double delta)
    {
        base._Process(delta);

        // Once target is reached the value is set exactly the same
        // ReSharper disable once CompareOfFloatsByEqualityOperator
        if (lastSetLightLevel != lightLevel)
        {
            UpdateLightLevel((float)delta);
        }

        if (AutoProcessWhilePaused && PauseManager.Instance.Paused)
        {
            UpdateCameraPosition(delta, null);
        }
    }

    public void UpdateCameraPosition(double delta, Vector3? followedObject)
    {
        var currentFloorPosition = new Vector3(Position.X, 0, Position.Z);
        var currentCameraHeight = new Vector3(0, Position.Y, 0);
        var newCameraHeight = new Vector3(0, CameraHeight, 0);

        if (followedObject != null)
        {
            var newFloorPosition = new Vector3(followedObject.Value.X, 0, followedObject.Value.Z);

            Vector3 target;

            if (currentFloorPosition.DistanceTo(newFloorPosition) < SnapWithDistanceLessThan)
            {
                // Don't interpolate floor position, this stops every few seconds slight hitching happening visually
                // with the player movement using the new physics (even when multiplying InterpolateSpeed with delta)
                target = newFloorPosition +
                    currentCameraHeight.Lerp(newCameraHeight, InterpolateZoomSpeed);
            }
            else
            {
                target = currentFloorPosition.Lerp(newFloorPosition, InterpolateSpeed)
                    + currentCameraHeight.Lerp(newCameraHeight, InterpolateZoomSpeed);
            }

            Position = target;
        }
        else
        {
            var target = new Vector3(Position.X, 0, Position.Z)
                + currentCameraHeight.Lerp(newCameraHeight, InterpolateZoomSpeed);

            Position = target;
        }

        if (backgroundPlane != null)
        {
            var target = new Vector3(0, 0, -15 - CameraHeight);

            backgroundPlane.Position = backgroundPlane.Position.Lerp(target, InterpolateZoomSpeed);
        }

        cursorDirty = true;
    }

    public void ResetHeight()
    {
        CameraHeight = DefaultCameraHeight;
        EmitSignal(SignalName.OnZoomChanged, CameraHeight);
    }

    /// <summary>
    ///   As this camera has special display resources all <see cref="Camera3D.Current"/> changes need to go through
    ///   this method
    /// </summary>
    /// <param name="current">True if this camera should be the current camera</param>
    public void SetCustomCurrentStatus(bool current)
    {
        Current = current;
        UpdateBackgroundVisibility();

        // TODO: set listener node current status
    }

    [RunOnAxis(new[] { "g_zoom_in", "g_zoom_out" }, new[] { -1.0f, 1.0f }, UseDiscreteKeyInputs = true)]
    public bool Zoom(double delta, float value)
    {
        if (!Current)
            return false;

        var old = CameraHeight;

        if (FramerateAdjustZoomSpeed)
        {
            // The constant on next line is for converting from delta corrected value to a good.Zooming speed.
            // ZoomSpeed was not adjusted because different speeds were already used in different parts of the game.
            CameraHeight += ZoomSpeed * value * (float)delta * 165;
        }
        else
        {
            CameraHeight += ZoomSpeed * value;
        }

        CameraHeight = CameraHeight.Clamp(MinCameraHeight, MaxCameraHeight);

        // ReSharper disable once CompareOfFloatsByEqualityOperator
        if (CameraHeight != old)
            EmitSignal(SignalName.OnZoomChanged, CameraHeight);

        return true;
    }

    /// <summary>
    ///   Set the used background images and particles
    /// </summary>
    public void SetBackground(Background background)
    {
        // TODO: skip duplicate background changes

        if (materialToUpdate == null)
            throw new InvalidOperationException("Camera not initialized yet");

        for (int i = 0; i < 4; ++i)
        {
            // TODO: switch this loop away to reuse StringName instances if this causes significant allocations
            materialToUpdate.SetShaderParameter($"layer{i:n0}", GD.Load<Texture2D>(background.Textures[i]));
        }

        backgroundParticles?.DetachAndQueueFree();

        backgroundParticles = background.ParticleEffectScene.Instantiate<GpuParticles3D>();
        backgroundParticles.Rotation = Rotation;
        backgroundParticles.LocalCoords = false;

        AddChild(backgroundParticles);

        OnDisplayBackgroundParticlesChanged(Settings.Instance.DisplayBackgroundParticles);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            lightLevelParameter.Dispose();
        }

        base.Dispose(disposing);
    }

    private void OnDisplayBackgroundParticlesChanged(bool displayed)
    {
        if (backgroundParticles == null)
        {
            GD.PrintErr("MicrobeCamera didn't find background particles on settings change");
            return;
        }

        // If we are not current camera, we don't want to display the background particles
        if (!Current)
            displayed = false;

        backgroundParticles.Emitting = displayed;

        if (displayed)
        {
            backgroundParticles.Show();
        }
        else
        {
            backgroundParticles.Hide();
        }
    }

    /// <summary>
    /// Applies distortion for a mouse position
    /// </summary>
    private Vector2 ApplyDistortion(Vector2 mousePos, Vector2 size, float dist)
    {
        // GD.Print($"{mousePos.X / size.X}, {mousePos.Y / size.Y}");

        dist /= size.X * 75f * 0.75f;

        // Convert from viewport's coordinate system of [0, size] to [-1;1]
        mousePos = (mousePos * 2f) / size - new Vector2(1f, 1f);

        // Apply shader's distortion
        float barrelDistortion1 = 0.1f * dist;
        float barrelDistortion2 = -0.025f * dist;

        float r2 = mousePos.X * mousePos.Y * 2f; // Replaces shader's dot(Vector2, Vector2) function
        mousePos *= 1.0f + barrelDistortion1 * r2 + barrelDistortion2 * r2 * r2;

        // Return to [0, size]
        mousePos = (mousePos + new Vector2(1f, 1f)) * size / 2f;

        // GD.Print($"=> {mousePos.X / size.X}, {mousePos.Y / size.Y}");

        return mousePos;
    }

    private void UpdateCursorWorldPos()
    {
        var worldPlane = new Plane(new Vector3(0, 1, 0), 0.0f);

        var viewPort = GetViewport();

        if (viewPort == null)
        {
            GD.PrintErr("Camera is not related to a viewport, can't update mouse world position");
            return;
        }

        var mousePos = viewPort.GetMousePosition();

        mousePos = ApplyDistortion(mousePos, viewPort.GetVisibleRect().Size, Settings.Instance.ChromaticAmount);

        var intersection = worldPlane.IntersectsRay(ProjectRayOrigin(mousePos),
            ProjectRayNormal(mousePos));

        if (intersection.HasValue)
        {
            CursorWorldPos = intersection.Value;
        }

        cursorDirty = false;
    }

    private void UpdateBackgroundVisibility()
    {
        if (backgroundPlane != null)
            backgroundPlane.Visible = Current;

        if (backgroundParticles != null)
            OnDisplayBackgroundParticlesChanged(Settings.Instance.DisplayBackgroundParticles);
    }

    private void UpdateLightLevel(float delta)
    {
        if (lastSetLightLevel < lightLevel)
        {
            lastSetLightLevel += LightLevelInterpolateSpeed * delta;

            if (lastSetLightLevel > lightLevel)
                lastSetLightLevel = lightLevel;
        }
        else if (lastSetLightLevel > lightLevel)
        {
            lastSetLightLevel -= LightLevelInterpolateSpeed * delta;

            if (lastSetLightLevel < lightLevel)
                lastSetLightLevel = lightLevel;
        }
        else
        {
            lastSetLightLevel = lightLevel;
        }

        materialToUpdate.SetShaderParameter(lightLevelParameter, lastSetLightLevel);
    }
}
