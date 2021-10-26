using Godot;

/// <summary>
///   Common helper operations for CollisionObjects
/// </summary>
public static class CollisionObjectHelpers
{
    /// <summary>
    ///  Creates a new shape owner for a shape with a given transform
    /// </summary>
    public static uint CreateOwner(this CollisionObject entity, Transform transform, Shape shape)
    {
        var newOwnerId = entity.CreateShapeOwner(shape);
        entity.ShapeOwnerAddShape(newOwnerId, shape);
        entity.ShapeOwnerSetTransform(newOwnerId, transform);
        return newOwnerId;
    }

    /// <summary>
    ///  Creates a new shapeOwner for a given shape and its old shape owner with a given transform
    ///  Doesnt destroy the old shape owner
    /// </summary>
    public static uint NewOwnerId(this CollisionObject oldParent, 
    CollisionObject newParent, Transform transform, uint oldId)
    {
        var shape = oldParent.ShapeOwnerGetShape(oldId, 0);
        var newOwnerId = CreateOwner(newParent, transform, shape);
        return newOwnerId;
    }

}