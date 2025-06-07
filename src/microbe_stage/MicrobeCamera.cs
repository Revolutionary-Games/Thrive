using System;
using Godot;
using Newtonsoft.Json;

/// <summary>
///   Camera script for the microbe stage and the cell editor
/// </summary>
public partial class MicrobeCamera : Camera3D, ISaveLoadedTracked, IGameCamera
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

    [Export]
    [JsonProperty]
    public float CursorTiltSpeed = 0.15f;

    /// <summary>
    ///   Size of fragment of camera-cursor vector to use to tilt the camera
    /// </summary>
    [Export]
    [JsonProperty]
    public float CursorTiltAmplitude = 0.04f;

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
    [JsonIgnore]
    [Export]
    private BackgroundPlane backgroundPlane = null!;
#pragma warning restore CA2213

    /// <summary>
    ///   Used to manually tween the light level to the target value
    /// </summary>
    private float lastSetLightLevel = 1;

    private Vector3 cursorWorldPos = new(0, 0, 0);
    private bool cursorDirty = true;

    private Vector3 cursorVisualWorldPos = new(0, 0, 0);
    private bool cursorVisualDirty = true;

    private Vector3 lastCursorTilt = new(0, 0, 0);

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

    /// <summary>
    ///   Returns the position the player is visually pointing to with their cursor, taking screen effects into account
    /// </summary>
    [JsonIgnore]
    public Vector3 CursorVisualWorldPos
    {
        get
        {
            if (cursorVisualDirty)
                UpdateCursorVisualWorldPos();
            return cursorVisualWorldPos;
        }
        private set => cursorVisualWorldPos = value;
    }

    public bool IsLoadedFromSave { get; set; }

    public override void _Ready()
    {
        if (!IsLoadedFromSave)
            ResetHeight();

        UpdateBackgroundVisibility();

        ProcessMode = ProcessModeEnum.Always;
    }

    public override void _EnterTree()
    {
        base._EnterTree();
        InputManager.RegisterReceiver(this);
    }

    public override void _ExitTree()
    {
        base._ExitTree();
        InputManager.UnregisterReceiver(this);
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

            // Apply cursor-induced tilt
            if (Settings.Instance.MicrobeCameraTilt)
            {
                var tilt = new Vector3(cursorVisualWorldPos.X - Position.X, 0, cursorVisualWorldPos.Z - Position.Z) *
                    CursorTiltAmplitude;

                lastCursorTilt = lastCursorTilt.Lerp(tilt, CursorTiltSpeed);

                target += lastCursorTilt;
            }

            Position = target;
        }
        else
        {
            var target = new Vector3(Position.X, 0, Position.Z)
                + currentCameraHeight.Lerp(newCameraHeight, InterpolateZoomSpeed);

            Position = target;
        }

        backgroundPlane.PlaneOffset = float.Lerp(backgroundPlane.PlaneOffset, -CameraHeight - 15, InterpolateZoomSpeed);

        cursorDirty = true;
        cursorVisualDirty = true;
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

        CameraHeight = Math.Clamp(CameraHeight, MinCameraHeight, MaxCameraHeight);

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
        backgroundPlane.SetBackground(background);
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

        var intersection = worldPlane.IntersectsRay(ProjectRayOrigin(mousePos),
            ProjectRayNormal(mousePos));

        if (intersection.HasValue)
        {
            CursorWorldPos = intersection.Value;
        }

        cursorDirty = false;
    }

    private void UpdateCursorVisualWorldPos()
    {
        var worldPlane = new Plane(new Vector3(0, 1, 0), 0.0f);

        var viewPort = GetViewport();

        if (viewPort == null)
        {
            GD.PrintErr("Camera is not related to a viewport, can't update mouse world position");
            return;
        }

        var mousePos = viewPort.GetMousePosition();

        mousePos = ApplyScreenEffects(mousePos, viewPort.GetVisibleRect().Size);

        var intersection = worldPlane.IntersectsRay(ProjectRayOrigin(mousePos),
            ProjectRayNormal(mousePos));

        if (intersection.HasValue)
        {
            cursorVisualWorldPos = intersection.Value;
        }

        cursorVisualDirty = false;
    }

    private Vector2 ApplyScreenEffects(Vector2 mousePos, Vector2 viewportSize)
    {
        if (Settings.Instance.ChromaticEnabled)
        {
            float distortion = Settings.Instance.ChromaticAmount;
            mousePos = ScreenUtils.BarrelDistortion(mousePos, distortion, viewportSize);
        }

        return mousePos;
    }

    private void UpdateBackgroundVisibility()
    {
        backgroundPlane.SetVisibility(Current);
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

        backgroundPlane.UpdateLightLevel(lastSetLightLevel);
    }
}
