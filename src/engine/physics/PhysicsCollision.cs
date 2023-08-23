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
    // Native code side handles writing to these objects
    // ReSharper disable UnassignedReadonlyField

    // The fields are here in an order optimized to minimize padding and not grouped logically

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
