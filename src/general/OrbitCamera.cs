using Godot;

/// <summary>
///   Camera that rotates around a pivot.
/// </summary>
public class OrbitCamera : Spatial
{
	[Export]
	public float Distance = 5;

	[Export]
	public float MinCameraDistance = 3.0f;

	[Export]
	public float MaxCameraDistance = 100.0f;

	[Export]
	public float RotationSpeed = 0.6f;

	[Export]
	public float ZoomSpeed = 1.0f;

	[Export]
	public float InterpolateRotationSpeed = 5.0f;

	[Export]
	public float InterpolateZoomSpeed = 5.0f;

#pragma warning disable CA2213
	private Camera camera = null!;
#pragma warning restore CA2213

	private Vector3 rotation;
	private Vector2 moveSpeed;

	public override void _Ready()
	{
		camera = GetNode<Camera>("Camera");
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

	public override void _Process(float delta)
	{
		rotation.x -= moveSpeed.y * delta * RotationSpeed;
		rotation.y -= moveSpeed.x * delta * RotationSpeed;
		rotation.x = rotation.x.Clamp(-Mathf.Pi / 2, Mathf.Pi / 2);
		moveSpeed = Vector2.Zero;

		Distance = Distance.Clamp(MinCameraDistance, MaxCameraDistance);

		camera.Translation = camera.Translation.LinearInterpolate(
			new Vector3(0, 0, Distance), InterpolateZoomSpeed * delta);

		var currentRotation = new Quat(Transform.basis);
		var targetRotation = new Quat(rotation);
		var smoothRotation = currentRotation.Slerp(targetRotation, InterpolateRotationSpeed * delta);
		Transform = new Transform(new Basis(smoothRotation), Translation);
	}

	public override void _Input(InputEvent @event)
	{
		if (@event is InputEventMouseMotion motion && Input.IsMouseButtonPressed((int)ButtonList.Left))
		{
			moveSpeed = motion.Relative;
		}
	}

	[RunOnAxis(new[] { "g_zoom_in", "g_zoom_out" }, new[] { -1.0f, 1.0f }, UseDiscreteKeyInputs = true,
		OnlyUnhandled = false)]
	public void Zoom(float delta, float value)
	{
		Distance += ZoomSpeed * value * delta * 165;
	}
}
