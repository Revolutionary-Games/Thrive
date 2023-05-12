using System;
using Godot;

/// <summary>
///   Wraps info returned by <see cref="PhysicsDirectSpaceState.IntersectRay"/>.
/// </summary>
public readonly struct RaycastResult : IEquatable<RaycastResult>
{
    public RaycastResult(object collider, int colliderId, Vector3 normal, Vector3 position, RID rid, int shape)
    {
        Collider = collider;
        ColliderId = colliderId;
        Normal = normal;
        Position = position;
        Rid = rid;
        Shape = shape;
    }

    /// <summary>
    ///   The colliding object.
    /// </summary>
    public object Collider { get; }

    /// <summary>
    ///   The colliding object's ID.
    /// </summary>
    public int ColliderId { get; }

    /// <summary>
    ///   The object's surface normal at the intersection point.
    /// </summary>
    public Vector3 Normal { get; }

    /// <summary>
    ///   The intersection point.
    /// </summary>
    public Vector3 Position { get; }

    /// <summary>
    ///   The intersecting object's RID.
    /// </summary>
    public RID Rid { get; }

    /// <summary>
    ///   The shape index of the colliding shape.
    /// </summary>
    public int Shape { get; }

    public static bool operator ==(RaycastResult left, RaycastResult right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(RaycastResult left, RaycastResult right)
    {
        return !(left == right);
    }

    public bool Equals(RaycastResult other)
    {
        return Rid.GetId() == other.Rid.GetId() && Shape == other.Shape;
    }

    public override bool Equals(object obj)
    {
        return obj is RaycastResult result && Equals(result);
    }

    public override int GetHashCode()
    {
        return Rid.GetId().GetHashCode() ^ Shape.GetHashCode();
    }
}
