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
    public Spatial ObjectToFollow;

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

    public MicrobeCamera()
    {
        RunOnInputAttribute.AddInstance(this);
    }

    /// <summary>
    ///   How high the camera is above the followed object
    /// </summary>
    public float CameraHeight { get; set; }

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

    public void ResolveNodeReferences()
    {
        if (NodeReferencesResolved)
            return;

        NodeReferencesResolved = true;

        if (HasNode("BackgroundPlane"))
            BackgroundPlane = GetNode<Spatial>("BackgroundPlane");
    }

    [RunOnAxis(new[] { "g_zoom_in", "g_zoom_out" }, new[] { -1, 1 })]
    public void ZoomIn(float delta, int acceptedValue)
    {
        _ = delta;
        CameraHeight += acceptedValue * ZoomSpeed;
        CameraHeight = CameraHeight.Clamp(MinCameraHeight, MaxCameraHeight);
    }

    /// <summary>
    ///   Updates camera position to follow the object
    /// </summary>
    public override void _PhysicsProcess(float delta)
    {
        var currFloorPos = new Vector3(Translation.x, 0, Translation.z);
        var currCamHeight = new Vector3(0, Translation.y, 0);
        var newCamPos = new Vector3(0, CameraHeight, 0);
        if (ObjectToFollow != null)
        {
            var newFloorPos = new Vector3(ObjectToFollow.Transform.origin.x, 0, ObjectToFollow.Transform.origin.z);

            var target = currFloorPos.LinearInterpolate(newFloorPos, InterpolateSpeed)
                + currCamHeight.LinearInterpolate(newCamPos, InterpolateZoomSpeed);

            Translation = target;
        }
        else
        {
            var target = new Vector3(Translation.x, 0, Translation.z)
                + currCamHeight.LinearInterpolate(newCamPos, InterpolateZoomSpeed);

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

        BackgroundParticles?.QueueFree();

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

        var mousePos = GetViewport()?.GetMousePosition();
        if (mousePos == null)
            return;

        var intersection = worldPlane.IntersectRay(ProjectRayOrigin(mousePos.Value),
            ProjectRayNormal(mousePos.Value));

        if (intersection.HasValue)
        {
            CursorWorldPos = intersection.Value;
        }

        cursorDirty = false;
    }
}
