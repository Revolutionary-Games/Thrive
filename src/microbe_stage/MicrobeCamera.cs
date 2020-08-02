using System.Collections.Generic;
using Godot;
using Newtonsoft.Json;

/// <summary>
///   Camera script for the microbe stage and the cell editor
/// </summary>
public class MicrobeCamera : Camera
{
    /// <summary>
    ///   Object the camera positions itself over
    /// </summary>
    [JsonProperty]
    public Spatial ObjectToFollow;

    /// <summary>
    ///   Background plane that is moved farther away from the camera when zooming out
    /// </summary>
    [JsonIgnore]
    public Spatial BackgroundPlane;

    [JsonIgnore]
    public Particles BackgroundParticles;

    [JsonProperty]
    public float CameraHeight;

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

    [JsonProperty]
    public float InterpolateSpeed = 0.3f;

    private ShaderMaterial materialToUpdate;

    private Vector3 cursorWorldPos;
    private bool cursorDirty = true;

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

        CursorWorldPos = new Vector3(0, 0, 0);

        if (HasNode("BackgroundPlane"))
            BackgroundPlane = GetNode<Spatial>("BackgroundPlane");

        ResetHeight();
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        if (@event.IsActionPressed("g_zoom_in", true))
        {
            CameraHeight -= ZoomSpeed;
        }

        if (@event.IsActionPressed("g_zoom_out", true))
        {
            CameraHeight += ZoomSpeed;
        }

        CameraHeight = CameraHeight.Clamp(MinCameraHeight, MaxCameraHeight);
    }

    /// <summary>
    ///   Updates camera position to follow the object
    /// </summary>
    public override void _PhysicsProcess(float delta)
    {
        if (ObjectToFollow != null)
        {
            var target = ObjectToFollow.Transform.origin + new Vector3(0, CameraHeight, 0);

            Translation = Translation.LinearInterpolate(target, InterpolateSpeed);
        }
        else
        {
            var target = new Vector3(Translation.x, CameraHeight, Translation.z);

            Translation = Translation.LinearInterpolate(target, InterpolateSpeed);
        }

        if (BackgroundPlane != null)
        {
            var target = new Vector3(0, 0, -15 - CameraHeight);

            BackgroundPlane.Translation = BackgroundPlane.Translation.LinearInterpolate(
                target, InterpolateSpeed);
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

    public void ApplyPropertiesFromSave(MicrobeCamera camera)
    {
        SaveApplyHelper.CopyJSONSavedPropertiesAndFields(this, camera, new List<string> { "ObjectToFollow" });
    }

    private void UpdateCursorWorldPos()
    {
        var worldPlane = new Plane(new Vector3(0, 1, 0), 0.0f);

        var mousePos = GetViewport().GetMousePosition();

        var intersection = worldPlane.IntersectRay(ProjectRayOrigin(mousePos),
            ProjectRayNormal(mousePos));

        if (intersection.HasValue)
        {
            CursorWorldPos = intersection.Value;
        }

        cursorDirty = false;
    }
}
