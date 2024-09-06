﻿using System;
using Godot;

/// <summary>
///   Camera that rotates around a pivot.
/// </summary>
public partial class OrbitCamera : Node3D
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
    private Camera3D camera = null!;
#pragma warning restore CA2213

    private Vector3 rotation;
    private Vector2 moveSpeed;

    public override void _Ready()
    {
        camera = GetNode<Camera3D>("Camera3D");
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
        var convertedDelta = (float)delta;

        rotation.X -= moveSpeed.Y * convertedDelta * RotationSpeed;
        rotation.Y -= moveSpeed.X * convertedDelta * RotationSpeed;
        rotation.X = Math.Clamp(rotation.X, -MathF.PI / 2, MathF.PI / 2);
        moveSpeed = Vector2.Zero;

        Distance = Math.Clamp(Distance, MinCameraDistance, MaxCameraDistance);

        camera.Position = camera.Position.Lerp(new Vector3(0, 0, Distance), InterpolateZoomSpeed * convertedDelta);

        var currentRotation = new Quaternion(Transform.Basis);
        var targetRotation = Quaternion.FromEuler(rotation);
        var smoothRotation = currentRotation.Slerp(targetRotation, InterpolateRotationSpeed * convertedDelta);
        Transform = new Transform3D(new Basis(smoothRotation), Position);
    }

    public override void _Input(InputEvent @event)
    {
        if (@event is InputEventMouseMotion motion && Input.IsMouseButtonPressed(MouseButton.Left))
        {
            moveSpeed = motion.Relative;
        }
    }

    [RunOnAxis(new[] { "g_zoom_in", "g_zoom_out" }, new[] { -1.0f, 1.0f }, UseDiscreteKeyInputs = true,
        OnlyUnhandled = false)]
    public void Zoom(double delta, float value)
    {
        Distance += ZoomSpeed * value * (float)delta * 165;
    }
}
