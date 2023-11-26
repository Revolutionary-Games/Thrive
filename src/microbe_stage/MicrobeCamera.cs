﻿using System;
using Godot;
using Newtonsoft.Json;

/// <summary>
///   Camera script for the microbe stage and the cell editor
/// </summary>
public class MicrobeCamera : Camera, IGodotEarlyNodeResolve, ISaveLoadedTracked, IGameCamera
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

#pragma warning disable CA2213

    /// <summary>
    ///   Background plane that is moved farther away from the camera when zooming out
    /// </summary>
    [JsonIgnore]
    private Spatial? backgroundPlane;

    [JsonIgnore]
    private Particles? backgroundParticles;

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

        PauseMode = PauseModeEnum.Process;
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

    public override void _Process(float delta)
    {
        base._Process(delta);

        // Once target is reached the value is set exactly the same
        // ReSharper disable once CompareOfFloatsByEqualityOperator
        if (lastSetLightLevel != lightLevel)
        {
            UpdateLightLevel(delta);
        }

        if (AutoProcessWhilePaused && PauseManager.Instance.Paused)
        {
            UpdateCameraPosition(delta, null);
        }
    }

    public void UpdateCameraPosition(float delta, Vector3? followedObject)
    {
        var currentFloorPosition = new Vector3(Translation.x, 0, Translation.z);
        var currentCameraHeight = new Vector3(0, Translation.y, 0);
        var newCameraHeight = new Vector3(0, CameraHeight, 0);

        if (followedObject != null)
        {
            var newFloorPosition = new Vector3(
                followedObject.Value.x, 0, followedObject.Value.z);

            Vector3 target;

            if (currentFloorPosition.DistanceTo(newFloorPosition) < SnapWithDistanceLessThan)
            {
                // Don't interpolate floor position, this stops every few seconds slight hitching happening visually
                // with the player movement using the new physics (even when multiplying InterpolateSpeed with delta)
                target = newFloorPosition +
                    currentCameraHeight.LinearInterpolate(newCameraHeight, InterpolateZoomSpeed);
            }
            else
            {
                target = currentFloorPosition.LinearInterpolate(newFloorPosition, InterpolateSpeed)
                    + currentCameraHeight.LinearInterpolate(newCameraHeight, InterpolateZoomSpeed);
            }

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

        materialToUpdate.SetShaderParam("lightLevel", lastSetLightLevel);
    }
}
