using System.Collections.Generic;
using Godot;
using Godot.Collections;

/// <summary>
///   Common helper operations for CollisionObjects and other physics stuff
/// </summary>
public static class PhysicsHelpers
{
    /// <summary>
    ///   Creates and returns a new shape owner for a shape and the given entity.
    ///   Applies a given transform to the new shapeOwner.
    /// </summary>
    /// <returns>Returns the id of the newly created shapeOwner</returns>
    public static uint CreateShapeOwnerWithTransform(this CollisionObject entity, Transform transform, Shape shape)
    {
        var newShapeOwnerId = entity.CreateShapeOwner(shape);
        entity.ShapeOwnerAddShape(newShapeOwnerId, shape);
        entity.ShapeOwnerSetTransform(newShapeOwnerId, transform);
        return newShapeOwnerId;
    }

    /// <summary>
    ///   Creates a new shapeOwner for the first shape in the oldShapeOwner of the oldParent
    ///   and applies a transform to it.
    ///   Doesn't destroy the oldShapeOwner.
    /// </summary>
    /// <returns>Returns the new ShapeOwnerId.</returns>
    public static uint CreateNewOwnerId(this CollisionObject oldParent,
        CollisionObject newParent, Transform transform, uint oldShapeOwnerId)
    {
        var shape = oldParent.ShapeOwnerGetShape(oldShapeOwnerId, 0);
        var newShapeOwnerId = CreateShapeOwnerWithTransform(newParent, transform, shape);
        return newShapeOwnerId;
    }

    /// <summary>
    ///   Extension of <see cref="PhysicsDirectSpaceState.IntersectRay"/>. Results from intersections will be stored
    ///   in <paramref name="hits"/>.
    /// </summary>
    public static void IntersectRay(this PhysicsDirectSpaceState space, List<RaycastResult> hits, Vector3 from,
        Vector3 to, Array? exclude = null, uint collisionMask = 2147483647u, bool collideWithBodies = true,
        bool collideWithAreas = false)
    {
        exclude ??= new Array();

        while (true)
        {
            var hit = space.IntersectRay(from, to, exclude, collisionMask, collideWithBodies, collideWithAreas);
            if (hit.Count <= 0)
                break;

            var result = new RaycastResult(
                hit["collider"],
                (int)hit["collider_id"],
                (Vector3)hit["normal"],
                (Vector3)hit["position"],
                (RID)hit["rid"],
                (int)hit["shape"]);

            hits.Add(result);
            exclude.Add(result.Collider);
        }
    }
}
