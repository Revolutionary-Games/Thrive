using Godot;
using System;

/// <summary>
///   Arrows that allow the player to move metaballs in the macroscopic editor
/// </summary>
public partial class EditorMovingArrows : Node3D
{
#pragma warning disable CA2213
    [Export]
    private Node3D xArrow = null!;

    [Export]
    private Node3D yArrow = null!;

    [Export]
    private Node3D zArrow = null!;
#pragma warning restore CA2213

    private Vector2 screenDirection;
    private Vector2 screenMouseOrigin;
    private Vector3 worldDirection;
    private Vector3 worldMetaballOrigin;

    private float screenArrowLength;

    private bool dragging;

    public bool IsDragging => dragging;

    public Vector3 GetDraggingPosition()
    {
        var mousePos = GetViewport().GetMousePosition();

        return worldMetaballOrigin + ((mousePos - screenMouseOrigin).Dot(screenDirection) / (screenArrowLength * screenArrowLength)) * worldDirection;
    }

    public bool TryStartDragging()
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

        if (RayIntersectsGivenArrow(xArrow, rayOrigin, rayEnd))
        {
            StartDraggingArrow(xArrow, camera, mousePos);
            return true;
        }

        if (RayIntersectsGivenArrow(yArrow, rayOrigin, rayEnd))
        {
            StartDraggingArrow(yArrow, camera, mousePos);
            return true;
        }

        if (RayIntersectsGivenArrow(zArrow, rayOrigin, rayEnd))
        {
            StartDraggingArrow(zArrow, camera, mousePos);
            return true;
        }

        return false;
    }

    public void StopDragging()
    {
        dragging = false;
    }

    private bool RayIntersectsGivenArrow(Node3D arrowNode, Vector3 rayStart, Vector3 rayEnd)
    {
        var invertedTransforms = arrowNode.GlobalTransform.Inverse();

        var collisions = Geometry3D.SegmentIntersectsCylinder(invertedTransforms * rayStart,
            invertedTransforms * rayEnd, 1.5f, 0.4f);

        return collisions.Length > 0;
    }

    private void StartDraggingArrow(Node3D arrowNode, Camera3D camera, Vector2 mousePos)
    {
        var start = camera.UnprojectPosition(arrowNode.GlobalPosition);
        var end = camera.UnprojectPosition(arrowNode.GlobalPosition + arrowNode.Transform * Vector3.Forward);

        worldDirection = arrowNode.Transform * Vector3.Forward;
        worldMetaballOrigin = arrowNode.GlobalPosition;
        screenDirection = end - start;
        screenMouseOrigin = mousePos;
        screenArrowLength = screenDirection.Length();

        dragging = true;
    }

    private Vector2 ProjectToScreen(Vector3 pos, Projection projection)
    {
        var projected = new Vector4(pos.X, pos.Y, pos.Z, 1.0f) * projection;

        return new Vector2(projected.X, projected.Y);
    }
}
