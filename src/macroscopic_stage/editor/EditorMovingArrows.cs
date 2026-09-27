using Godot;
using System;

/// <summary>
///   Arrows that allow the player to move metaballs in the macroscopic editor
/// </summary>
public partial class EditorMovingArrows : Node3D
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
    private Plane ringPlane;
    private Vector3 parentOrigin;
    private Vector3 rotationPivot;
    private Vector3 baseRotation;

    private bool dragging;

    public bool IsDragging => dragging;

    public override void _Process(double delta)
    {
        var viewPort = GetViewport();

        if (viewPort == null)
            throw new InvalidOperationException("No viewport");

        var camera = viewPort.GetCamera3D();

        if (camera == null)
            throw new InvalidOperationException("No camera");

        var mousePos = viewPort.GetMousePosition();

        var rayOrigin = camera.ProjectRayOrigin(mousePos);
        var rayNormal = camera.ProjectRayNormal(mousePos);

        var bestRing = BestSelectedRing(rayOrigin, rayNormal);

        if (dragging)
        {
            SetTorusRotations(parentOrigin, GetDraggingPosition());
        }
        else
        {
            horizontalRing.MaterialOverride = null;
            verticalRing.MaterialOverride = null;

            if (bestRing != null)
            {
                ((MeshInstance3D)bestRing).MaterialOverride = highlightMaterial;
            }
        }
    }

    public Vector3 GetDraggingPosition()
    {
        var viewPort = GetViewport();
        var mousePos = viewPort.GetMousePosition();
        var camera = viewPort.GetCamera3D();

        var angle = ProjectAndGetAngle(camera.ProjectRayOrigin(mousePos), camera.ProjectRayNormal(mousePos));
        var newPos = rotationPivot + baseRotation.Rotated(ringPlane.Normal, angle - angleOffset);

        return newPos;
    }

    public void InitializeDisplay(Vector3 parentPos, Vector3 metaballPos)
    {
        SetTorusRotations(parentPos, metaballPos);
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

        var rayOrigin = camera.ProjectRayOrigin(mousePos);
        var rayNormal = camera.ProjectRayNormal(mousePos);
        var rayEnd = rayOrigin + rayNormal * 1000.0f;

        var ring = BestSelectedRing(rayOrigin, rayEnd);

        if (ring != null)
        {
            StartDraggingArrow(ring, camera, mousePos, parentPos, metaballPos, combinedScale);
            return true;
        }

        return false;
    }

    public void StopDragging()
    {
        dragging = false;

        horizontalRing.MaterialOverride = null;
        verticalRing.MaterialOverride = null;
    }

    private Node3D? BestSelectedRing(Vector3 rayStart, Vector3 rayDir)
    {
        float horizontalDistance = DistanceToRing(horizontalRing, rayStart, rayDir);
        float verticalDistance = DistanceToRing(verticalRing, rayStart, rayDir);

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

    private float DistanceToRing(Node3D ring, Vector3 rayStart, Vector3 rayDir)
    {
        var intersection = new Plane(ring.Quaternion * Vector3.Up, ring.GlobalPosition).IntersectsRay(rayStart, rayDir);

        if (intersection == null)
        {
            return float.MaxValue;
        }

        return MathF.Abs(intersection.Value.DistanceSquaredTo(ring.GlobalPosition) - ring.Scale.X * ring.Scale.X);
    }

    private void StartDraggingArrow(Node3D arrowNode, Camera3D camera, Vector2 mousePos, Vector3 parentPos,
        Vector3 metaballPos, float distance)
    {
        parentOrigin = parentPos;

        if (arrowNode == verticalRing)
        {
            ringPlane = new Plane(Vector3.Up.Cross(parentPos - metaballPos).Normalized(), parentPos);
        }
        else
        {
            ringPlane = new Plane(Vector3.Up, metaballPos);
        }

        var projectedDistance = metaballPos.DistanceTo(rotationPivot);

        rotationPivot = ringPlane.Project(parentOrigin);
        baseRotation = distance * (metaballPos - rotationPivot) / projectedDistance;
        baseRotation *= projectedDistance / parentPos.DistanceTo(metaballPos);

        angleOffset = ProjectAndGetAngle(camera.ProjectRayOrigin(mousePos), camera.ProjectRayNormal(mousePos));

        dragging = true;

        horizontalRing.MaterialOverride = null;
        verticalRing.MaterialOverride = null;

        // TODO: fix this
        ((MeshInstance3D)arrowNode).MaterialOverride = highlightMaterial;

        SetTorusRotations(parentOrigin, metaballPos);
    }

    private float ProjectAndGetAngle(Vector3 rayOrigin, Vector3 rayDir)
    {
        var intersection = ringPlane.IntersectsRay(rayOrigin, rayDir);

        if (intersection.HasValue)
        {
            return GetAngle(intersection.Value);
        }
        else
        {
            return 0.0f;
        }
    }

    private float GetAngle(Vector3 to)
    {
        return -(to - rotationPivot).SignedAngleTo(baseRotation, ringPlane.Normal);
    }

    private void SetTorusRotations(Vector3 parentPos, Vector3 metaballPos)
    {
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
