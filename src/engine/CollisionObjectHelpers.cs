using Godot;

/// <summary>
///   Common helper operations for CollisionObjects
/// </summary>
public static class CollisionObjectHelpers
{
    /// <summary>
    ///  Creates and returns new shape owner for a shape
    ///  applies a given transform to the new shapeOwner
    /// </summary>
    public static uint CreateShapeOwnerWithTransform(this CollisionObject entity, Transform transform, Shape shape)
    {
        var newShapeOwnerId = entity.CreateShapeOwner(shape);
        entity.ShapeOwnerAddShape(newShapeOwnerId, shape);
        entity.ShapeOwnerSetTransform(newShapeOwnerId, transform);
        return newShapeOwnerId;
    }

    /// <summary>
    ///  Creates a new shapeOwner for the first shape in the old shapeOwner
    ///  and applies a given transform
    ///  Doesnt destroy the old shapeOwner
    /// </summary>
    public static uint NewOwnerId(this CollisionObject oldParent,
        CollisionObject newParent, Transform transform, uint oldShapeOwnerId)
    {
        var shape = oldParent.ShapeOwnerGetShape(oldShapeOwnerId, 0);
        var newShapeOwnerId = CreateShapeOwnerWithTransform(newParent, transform, shape);
        return newShapeOwnerId;
    }
}
