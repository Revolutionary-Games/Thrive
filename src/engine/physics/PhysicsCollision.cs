using System;
using System.Runtime.InteropServices;
using DefaultEcs;

/// <summary>
///   Info regarding a physics collision in an entity simulation. Must match the PhysicsCollision class byte layout
///   defined on the C++ side.
/// </summary>
[StructLayout(LayoutKind.Sequential)]
public struct PhysicsCollision
{
    /// <summary>
    ///   When a sub shape data is equal to this, the shape is unknown and not a sub-shape. This must match what the
    ///   native side has defined.
    /// </summary>
    public const uint COLLISION_UNKNOWN_SUB_SHAPE = uint.MaxValue;

    // Native code side handles writing to these objects
    // ReSharper disable UnassignedReadonlyField

    // The fields are here in an order optimized to minimize padding and not grouped logically

    /// <summary>
    ///   The first entity participating in this collision. This is a bitwise copy of the entity identifier of the
    ///   entity this physics body was created for. Note that the ordering is always guaranteed so that the entity
    ///   recording the collision or checking inside a collision filter is always the first body. So code does not have
    ///   to check if the first or second entity is the entity that created this collision object.
    /// </summary>
    public readonly Entity FirstEntity;

    public readonly Entity SecondEntity;

    /// <summary>
    ///   First colliding body, this is not wrapped in a <see cref="NativePhysicsBody"/> to avoid extra reference
    ///   counting and object allocations, rather this is directly the pointer to the native side body
    /// </summary>
    public readonly IntPtr FirstBody;

    public readonly IntPtr SecondBody;

    /// <summary>
    ///   Physics sub-shape data for this collision. Unknown (uint.Max) when used in a collision filter.
    /// </summary>
    public readonly uint FirstSubShapeData;

    public readonly uint SecondSubShapeData;

    /// <summary>
    ///   How hard the collision is. This is not calculated in the collision filter
    /// </summary>
    public readonly float PenetrationAmount;

    /// <summary>
    ///   True on the first physics update this collision appeared (always true in the collision filter)
    /// </summary>
    public readonly bool JustStarted;

    // ReSharper restore UnassignedReadonlyField
}
