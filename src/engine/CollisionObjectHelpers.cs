using Godot;

/// <summary>
///   Common helper operations for CollisionObjects
/// </summary>
public static class CollisionObjectHelpers
{
    /// <summary>
    ///  Creates and returns new shape owner for a shape
    ///  apllies a given transform to the new shapeOwner
    ///  Used on collisionObjects
    /// </summary>
    public static uint CreateOwnerWithTransform(this CollisionObject entity, Transform transform, Shape shape)
    {
        var newOwnerId = entity.CreateShapeOwner(shape);
        entity.ShapeOwnerAddShape(newOwnerId, shape);
        entity.ShapeOwnerSetTransform(newOwnerId, transform);
        return newOwnerId;
    }

    /// <summary>
    ///  Creates a new shapeOwner for a given shape and its old shape owner
    ///  and applies a given transform
    ///  Doesnt destroy the old shape owner
    /// </summary>
    public static uint NewOwnerId(this CollisionObject oldParent,
        CollisionObject newParent, Transform transform, uint oldId)
    {
        var shape = oldParent.ShapeOwnerGetShape(oldId, 0);
        var newOwnerId = CreateOwnerWithTransform(newParent, transform, shape);
        return newOwnerId;
    }
}
