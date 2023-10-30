using System;
using System.Runtime.InteropServices;
using DefaultEcs;

/// <summary>
///   Info regarding a physics ray hit. Must match the PhysicsRayWithUserData class byte layout defined on the
///   native side.
/// </summary>
[StructLayout(LayoutKind.Sequential)]
public struct PhysicsRayWithUserData
{
    /// <summary>
    ///   The hit entity. Maybe be 0 bytes if hits a physics object not created through the ECS simulation
    /// </summary>
    public readonly Entity BodyEntity;

    /// <summary>
    ///   Raw pointer that is not wrapped in a <see cref="NativePhysicsBody"/> for performance reasons
    /// </summary>
    public readonly IntPtr Body;

    /// <summary>
    ///   Sub-shape that was hit. Equals <see cref="PhysicsCollision.COLLISION_UNKNOWN_SUB_SHAPE"/> if unknown.
    /// </summary>
    public readonly uint SubShapeData;

    /// <summary>
    ///   How far along the cast ray this hit was (as a fraction of the total ray length)
    /// </summary>
    public readonly float HitFraction;
}
