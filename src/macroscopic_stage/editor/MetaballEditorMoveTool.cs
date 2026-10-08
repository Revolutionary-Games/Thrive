using System;
using Godot;

/// <summary>
///   Arrows that allow the player to move metaballs in the macroscopic editor. Can probably be repurposed to move
///   anything else.
/// </summary>
public partial class MetaballEditorMoveTool : Node3D
{
#pragma warning disable CA2213
    [Export]
    private MeshInstance3D horizontalRing = null!;

    [Export]
    private MeshInstance3D verticalRing = null!;

    [Export]
    private Material highlightMaterial = null!;

    [Export]
    private float maxRingDistanceToSelect = 0.2f;
#pragma warning restore CA2213

    private float angleOffset;
    private Vector3 rotationAxis;
    private Vector3 parentOrigin;
    private Vector3 rotationOrigin;
    private Vector3 initialRotation;

    public bool IsDragging { get; private set; }

    public void InitializeDisplay(Vector3 parentPos, Vector3 metaballPos)
    {
        SetTorusRotations(parentPos, metaballPos);
    }

    public override void _Process(double delta)
    {
        var viewPort = GetViewport();
        var camera = viewPort.GetCamera3D();
        var mousePos = viewPort.GetMousePosition();

        if (IsDragging)
        {
            SetTorusRotations(parentOrigin, GetDraggingPosition());
        }
        else
        {
            horizontalRing.MaterialOverride = null;
            verticalRing.MaterialOverride = null;

            var rayOrigin = camera.ProjectRayOrigin(mousePos);
            var rayNormal = camera.ProjectRayNormal(mousePos);

            BestSelectedRing(rayOrigin, rayNormal)?.MaterialOverride = highlightMaterial;
        }
    }

    public Vector3 GetDraggingPosition()
    {
        var viewPort = GetViewport();
        var mousePos = viewPort.GetMousePosition();
        var camera = viewPort.GetCamera3D();

        var angle = GetRotationAngle(camera.UnprojectPosition(rotationOrigin), mousePos);

        return rotationOrigin + initialRotation.Rotated(rotationAxis, angle - angleOffset);
    }

    public void EndDisplay()
    {
        Visible = false;

        StopDragging();
    }

    public bool TryStartDragging(Vector3 parentPos, Vector3 metaballPos, float combinedScale)
    {
        var viewPort = GetViewport();

        if (viewPort == null)
            throw new InvalidOperationException("No viewport");

        var camera = viewPort.GetCamera3D();

        if (camera == null)
            throw new InvalidOperationException("No camera");

        var mousePos = viewPort.GetMousePosition();

        var ring = BestSelectedRing(camera.ProjectRayOrigin(mousePos), camera.ProjectRayNormal(mousePos));

        if (ring != null)
        {
            StartDraggingArrow(ring, camera, mousePos, parentPos, metaballPos, combinedScale);
            return true;
        }

        return false;
    }

    public void StopDragging()
    {
        IsDragging = false;

        horizontalRing.MaterialOverride = null;
        verticalRing.MaterialOverride = null;
    }

    /// <summary>
    ///   Determines which of the two rings is more likely to be the one that the player wants to select
    /// </summary>
    private MeshInstance3D? BestSelectedRing(Vector3 rayStart, Vector3 rayDir)
    {
        float horizontalDistance = ProjectedDistanceToRing(horizontalRing, rayStart, rayDir);
        float verticalDistance = ProjectedDistanceToRing(verticalRing, rayStart, rayDir);

        if (horizontalDistance > maxRingDistanceToSelect && verticalDistance > maxRingDistanceToSelect)
        {
            return null;
        }

        if (horizontalDistance < verticalDistance)
        {
            return horizontalRing;
        }

        return verticalRing;
    }

    private float ProjectedDistanceToRing(MeshInstance3D ring, Vector3 rayStart, Vector3 rayDir)
    {
        var intersection = new Plane(ring.Quaternion * Vector3.Up, ring.GlobalPosition).IntersectsRay(rayStart, rayDir);

        if (intersection == null)
        {
            return float.MaxValue;
        }

        return MathF.Abs(intersection.Value.DistanceSquaredTo(ring.GlobalPosition) - ring.Scale.X * ring.Scale.X);
    }

    private void StartDraggingArrow(MeshInstance3D ring, Camera3D camera, Vector2 mousePos, Vector3 parentPos,
        Vector3 metaballPos, float combinedScale)
    {
        parentOrigin = parentPos;

        if (ring == verticalRing)
        {
            rotationOrigin = parentPos;
            rotationAxis = Vector3.Up.Cross(parentPos - metaballPos).Normalized();

            if (rotationAxis == Vector3.Zero)
            {
                // The current metaball is right above or below its parent, so we need to arbitrarily pick a rotation
                // axis
                rotationAxis = Vector3.Right;
            }
        }
        else
        {
            rotationOrigin = parentPos;
            rotationOrigin.Y = metaballPos.Y;
            rotationAxis = Vector3.Up;
        }

        // For the angle returned by GetRotationAngle, the axis needs to be facing away from the camera
        if (rotationAxis.Dot(camera.Quaternion * Vector3.Forward) < 0.0f)
            rotationAxis *= -1.0f;

        initialRotation = (metaballPos - rotationOrigin) * combinedScale / parentPos.DistanceTo(metaballPos);

        angleOffset = GetRotationAngle(camera.UnprojectPosition(rotationOrigin), mousePos);

        IsDragging = true;

        horizontalRing.MaterialOverride = null;
        verticalRing.MaterialOverride = null;
        ring.MaterialOverride = highlightMaterial;
    }

    private float GetRotationAngle(Vector2 screenRotationOrigin, Vector2 mousePos)
    {
        var toMousePos = (mousePos - screenRotationOrigin).Normalized();

        return toMousePos.Angle();
    }

    private void SetTorusRotations(Vector3 parentPos, Vector3 metaballPos)
    {
        Position = parentPos;
        Visible = true;

        horizontalRing.Position = new Vector3(0.0f, metaballPos.Y - parentPos.Y, 0.0f);

        var projectedVectorToMetaball = metaballPos - parentPos;
        projectedVectorToMetaball.Y = 0.0f;

        horizontalRing.Scale = projectedVectorToMetaball.Length() * Vector3.One;
        verticalRing.Scale = (parentPos - metaballPos).Length() * Vector3.One;

        // Vertical ring's X rotation, and horizontal ring rotation aren't actually necessary or visible if the rotation
        // rings look fully uniform
        var rotationToMetaball = MathF.PI - projectedVectorToMetaball.SignedAngleTo(Vector3.Forward, Vector3.Up);
        var xAxisRotationToMetaball = projectedVectorToMetaball.SignedAngleTo(metaballPos - parentPos,
            Vector3.Up.Cross(metaballPos - parentPos));

        verticalRing.Rotation = new Vector3(xAxisRotationToMetaball, rotationToMetaball, MathF.PI * 0.5f);
        horizontalRing.Rotation = new Vector3(0.0f, rotationToMetaball, 0.0f);
    }
}
