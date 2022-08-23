using Godot;

/// <summary>
///   Camera for handling a 3rd person camera in 3D space
///   TODO: implement the camera rotating logic
/// </summary>
public class MulticellularCamera : Spatial
{
    private Camera camera = null!;
    private Listener listener = null!;

    public bool Current
    {
        get => camera.Current;
        set
        {
            camera.Current = value;

            if (value)
            {
                listener.MakeCurrent();
            }
            else if (!value && listener.IsCurrent())
            {
                listener.ClearCurrent();
            }
        }
    }

    public override void _Ready()
    {
        camera = GetNode<Camera>("CameraPosition/SpringArm/Camera");
        listener = GetNode<Listener>("CameraPosition/SpringArm/Camera/Listener");
    }
}
