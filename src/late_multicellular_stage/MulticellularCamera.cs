using Godot;

/// <summary>
///   Camera for handling a 3rd person camera in 3D space
///   TODO: implement the camera rotating logic
/// </summary>
public class MulticellularCamera : Spatial
{
    private Camera camera = null!;

    public bool Current
    {
        get => camera.Current;
        set => camera.Current = value;
    }

    public override void _Ready()
    {
        camera = GetNode<Camera>("CameraPosition/SpringArm/Camera");
    }
}
