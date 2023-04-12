using System;
using Godot;
using Newtonsoft.Json;

/// <summary>
///   Camera script for the microbe stage and the cell editor
/// </summary>
public class MicrobeCamera : Camera, IGodotEarlyNodeResolve, ISaveLoadedTracked
{
    /// <summary>
    ///   Object the camera positions itself over
    /// </summary>
    public Spatial? ObjectToFollow;

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

#pragma warning disable CA2213

    /// <summary>
    ///   Background plane that is moved farther away from the camera when zooming out
    /// </summary>
    [JsonIgnore]
    private Spatial? backgroundPlane;

    [JsonIgnore]
    private Particles? backgroundParticles;
#pragma warning restore CA2213

    private ShaderMaterial? materialToUpdate;

    private Vector3 cursorWorldPos = new(0, 0, 0);
    private bool cursorDirty = true;

    [JsonProperty]
    private float lightLevel = 1.0f;

    [Signal]
    public delegate void OnZoomChanged(float zoom);

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

            UpdateLightLevel();
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
        var material = GetNode<CSGMesh>("BackgroundPlane").Material;
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
        UpdateLightLevel();
    }

    public void ResolveNodeReferences()
    {
        if (NodeReferencesResolved)
            return;

        NodeReferencesResolved = true;

        if (HasNode("BackgroundPlane"))
            backgroundPlane = GetNode<Spatial>("BackgroundPlane");
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

    /// <summary>
    ///   Updates camera position to follow the object
    /// </summary>
    public override void _PhysicsProcess(float delta)
    {
        var currentFloorPosition = new Vector3(Translation.x, 0, Translation.z);
        var currentCameraHeight = new Vector3(0, Translation.y, 0);
        var newCameraHeight = new Vector3(0, CameraHeight, 0);

        if (ObjectToFollow != null)
        {
            var newFloorPosition = new Vector3(
                ObjectToFollow.GlobalTransform.origin.x, 0, ObjectToFollow.GlobalTransform.origin.z);

            var target = currentFloorPosition.LinearInterpolate(newFloorPosition, InterpolateSpeed)
                + currentCameraHeight.LinearInterpolate(newCameraHeight, InterpolateZoomSpeed);

            Translation = target;
        }
        else
        {
            var target = new Vector3(Translation.x, 0, Translation.z)
                + currentCameraHeight.LinearInterpolate(newCameraHeight, InterpolateZoomSpeed);

            Translation = target;
        }

        if (backgroundPlane != null)
        {
            var target = new Vector3(0, 0, -15 - CameraHeight);

            backgroundPlane.Translation = backgroundPlane.Translation.LinearInterpolate(
                target, InterpolateZoomSpeed);
        }

        cursorDirty = true;
    }

    public void ResetHeight()
    {
        CameraHeight = DefaultCameraHeight;
        EmitSignal(nameof(OnZoomChanged), CameraHeight);
    }

    /// <summary>
    ///   As this camera has special display resources all <see cref="Camera.Current"/> changes need to go through
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
    public bool Zoom(float delta, float value)
    {
        if (!Current)
            return false;

        var old = CameraHeight;

        if (FramerateAdjustZoomSpeed)
        {
            // The constant on next line is for converting from delta corrected value to a good zooming speed.
            // ZoomSpeed was not adjusted because different speeds were already used in different parts of the game.
            CameraHeight += ZoomSpeed * value * delta * 165;
        }
        else
        {
            CameraHeight += ZoomSpeed * value;
        }

        CameraHeight = CameraHeight.Clamp(MinCameraHeight, MaxCameraHeight);

        // ReSharper disable once CompareOfFloatsByEqualityOperator
        if (CameraHeight != old)
            EmitSignal(nameof(OnZoomChanged), CameraHeight);

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
            materialToUpdate.SetShaderParam($"layer{i:n0}", GD.Load<Texture>(background.Textures[i]));
        }

        backgroundParticles?.DetachAndQueueFree();

        backgroundParticles = (Particles)background.ParticleEffectScene.Instance();
        backgroundParticles.Rotation = Rotation;
        backgroundParticles.LocalCoords = false;

        AddChild(backgroundParticles);

        OnDisplayBackgroundParticlesChanged(Settings.Instance.DisplayBackgroundParticles);
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

        var intersection = worldPlane.IntersectRay(ProjectRayOrigin(mousePos),
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

    private void UpdateLightLevel()
    {
        materialToUpdate?.SetShaderParam("lightLevel", LightLevel);
    }
}
