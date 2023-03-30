using Godot;

/// <summary>
///   Camera for the strategy stages of the game
/// </summary>
public class StrategicCamera : Camera
{
    /// <summary>
    ///   The position the camera is over
    /// </summary>
    [Export]
    public Vector3 WorldLocation { get; set; }

    [Export]
    public float ZoomLevel { get; set; } = 1;

    public override void _Ready()
    {
        ProcessPriority = 1000;
    }

    public override void _Process(float delta)
    {
        // TODO: interpolating if there's a small movement for more smoothness?
        GlobalTransform = StrategicCameraHelpers.CalculateCameraPosition(WorldLocation, ZoomLevel);
    }
}
