using Godot;
using Newtonsoft.Json;

/// <summary>
///   Camera for handling a 3rd person camera in 3D space
///   TODO: implement the camera rotating logic
/// </summary>
public class MulticellularCamera : Spatial, IGodotEarlyNodeResolve
{
    private Camera camera = null!;
    private Listener listener = null!;

    private Spatial? offsetNode;
    private SpringArm? arm;

    private float armLength = 8;
    private Vector3 followOffset = new(1.2f, 1.5f, 0.5f);

    [Export]
    [JsonProperty]
    public float ArmLength
    {
        get => armLength;
        set
        {
            armLength = value;
            ApplyArmLength();
        }
    }

    [Export]
    [JsonProperty]
    public Vector3 FollowOffset
    {
        get => followOffset;
        set
        {
            followOffset = value;
            ApplyFollowOffset();
        }
    }

    [Export]
    [JsonProperty]
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

    [JsonIgnore]
    public bool NodeReferencesResolved { get; set; }

    public Spatial? FollowedNode { get; set; }

    public override void _Ready()
    {
        ResolveNodeReferences();

        ApplyArmLength();
        ApplyFollowOffset();
    }

    public void ResolveNodeReferences()
    {
        if (NodeReferencesResolved)
            return;

        camera = GetNode<Camera>("CameraPosition/SpringArm/Camera");
        listener = GetNode<Listener>("CameraPosition/SpringArm/Camera/Listener");

        offsetNode = GetNode<Spatial>("CameraPosition");
        arm = GetNode<SpringArm>("CameraPosition/SpringArm");

        NodeReferencesResolved = true;
    }

    public override void _PhysicsProcess(float delta)
    {
        base._PhysicsProcess(delta);

        if (FollowedNode == null)
            return;

        // Move ourselves (the base node) to be on top of the target, our offset node will then position the camera
        // correctly
        Transform = new Transform(Quat.Identity, FollowedNode.Translation);

        var rotation = Quat.Identity;

        // TODO: rotation
        arm!.Transform = new Transform(rotation, Vector3.Zero);
    }

    private void ApplyArmLength()
    {
        if (arm != null)
            arm.SpringLength = armLength;
    }

    private void ApplyFollowOffset()
    {
        if (offsetNode != null)
            offsetNode.Translation = FollowOffset;
    }
}
