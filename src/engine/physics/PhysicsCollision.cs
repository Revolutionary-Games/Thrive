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
    ///   counting and object allocations
    /// </summary>
    public readonly IntPtr FirstBody;

    public readonly IntPtr SecondBody;

    public readonly int FirstSubShapeData;

    public readonly int SecondSubShapeData;

    public readonly float PenetrationAmount;

    // ReSharper restore UnassignedReadonlyField
}
