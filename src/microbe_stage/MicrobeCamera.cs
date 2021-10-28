using Godot;
using Newtonsoft.Json;

/// <summary>
///   Camera script for the microbe stage and the cell editor
/// </summary>
public class MicrobeCamera : Camera, IGodotEarlyNodeResolve, ISaveLoadedTracked
{
    /// <summary>
    ///   Background plane that is moved farther away from the camera when zooming out
    /// </summary>
    [JsonIgnore]
    public Spatial BackgroundPlane;

    [JsonIgnore]
    public Particles BackgroundParticles;

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
    public bool DisableBackgroundParticles;

    [Export]
    [JsonProperty]
    public float InterpolateSpeed = 0.3f;

    [Export]
    [JsonProperty]
    public float InterpolateZoomSpeed = 0.3f;

    private ShaderMaterial materialToUpdate;

    private Vector3 cursorWorldPos = new Vector3(0, 0, 0);
    private bool cursorDirty = true;

    /// <summary>
    ///   Object the camera positions itself over
    /// </summary>
    public Spatial ObjectToFollow { get; private set; }

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

    public void ResetHeight()
    {
        CameraHeight = DefaultCameraHeight;
    }

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
    }

    public override void _EnterTree()
    {
        InputManager.RegisterReceiver(this);
        base._EnterTree();
    }

    public override void _ExitTree()
    {
        InputManager.UnregisterReceiver(this);
        base._ExitTree();
    }

    /// <summary>
    ///   Sets the object to follow
    /// </summary>
    /// <param name="objectToFollow">The new object to follow</param>
    /// <param name="jump">If true the camera will not interpolate to the new object</param>
    public void SetObjectToFollow(Spatial objectToFollow, bool jump)
    {
        ObjectToFollow = objectToFollow;

        if (jump && ObjectToFollow != null)
            Translation = new Vector3(ObjectToFollow.Transform.origin.x, CameraHeight, ObjectToFollow.Transform.origin.z);
    }

    public void ResolveNodeReferences()
    {
        if (NodeReferencesResolved)
            return;

        NodeReferencesResolved = true;

        if (HasNode("BackgroundPlane"))
            BackgroundPlane = GetNode<Spatial>("BackgroundPlane");
    }

    [RunOnAxis(new[] { "g_zoom_in", "g_zoom_out" }, new[] { -1.0f, 1.0f }, UseDiscreteKeyInputs = true)]
    public void Zoom(float delta, float value)
    {
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
            var newFloorPosition = new Vector3(ObjectToFollow.Transform.origin.x, 0, ObjectToFollow.Transform.origin.z);

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

        if (BackgroundPlane != null)
        {
            var target = new Vector3(0, 0, -15 - CameraHeight);

            BackgroundPlane.Translation = BackgroundPlane.Translation.LinearInterpolate(
                target, InterpolateZoomSpeed);
        }

        cursorDirty = true;
    }

    /// <summary>
    ///   Set the used background images and particles
    /// </summary>
    public void SetBackground(Background background)
    {
        // TODO: skip duplicate background changes

        for (int i = 0; i < 4; ++i)
        {
            materialToUpdate.SetShaderParam($"layer{i:n0}", GD.Load<Texture>(background.Textures[i]));
        }

        BackgroundParticles?.DetachAndQueueFree();

        if (!DisableBackgroundParticles)
        {
            BackgroundParticles = (Particles)background.ParticleEffectScene.Instance();
            BackgroundParticles.Rotation = Rotation;
            BackgroundParticles.LocalCoords = false;
            AddChild(BackgroundParticles);
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
}
