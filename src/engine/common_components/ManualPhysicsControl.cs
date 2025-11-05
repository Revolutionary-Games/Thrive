namespace Components;

using Godot;
using SharedBase.Archive;

/// <summary>
///   Allows manual physics control over physical entities
/// </summary>
public struct ManualPhysicsControl : IArchivableComponent
{
    public const ushort SERIALIZATION_VERSION = 1;

    // Note: to allow multiple places in the code to use this, this should have values added with += instead of
    // assigning to not remove the previous value.
    public Vector3 ImpulseToGive;
    public Vector3 AngularImpulseToGive;

    public bool RemoveVelocity;
    public bool RemoveAngularVelocity;

    /// <summary>
    ///   Needs to be set false whenever anything is changed here, otherwise the physics state is not applied
    /// </summary>
    /// <remarks>
    ///   <para>
    ///     This is actually saved (unlike many applied state variables) as the velocities applied to the physics
    ///     object are persistent state as they have already affected the physics object properties.
    ///   </para>
    /// </remarks>
    public bool PhysicsApplied;

    public ushort CurrentArchiveVersion => SERIALIZATION_VERSION;
    public ThriveArchiveObjectType ArchiveObjectType => ThriveArchiveObjectType.ComponentManualPhysicsControl;

    public void WriteToArchive(ISArchiveWriter writer)
    {
        writer.Write(ImpulseToGive);
        writer.Write(AngularImpulseToGive);
        writer.Write(RemoveVelocity);
        writer.Write(RemoveAngularVelocity);
        writer.Write(PhysicsApplied);
    }
}

public static class ManualPhysicsControlHelpers
{
    public static ManualPhysicsControl ReadFromArchive(ISArchiveReader reader, ushort version)
    {
        if (version is > ManualPhysicsControl.SERIALIZATION_VERSION or <= 0)
            throw new InvalidArchiveVersionException(version, ManualPhysicsControl.SERIALIZATION_VERSION);

        return new ManualPhysicsControl
        {
            ImpulseToGive = reader.ReadVector3(),
            AngularImpulseToGive = reader.ReadVector3(),
            RemoveVelocity = reader.ReadBool(),
            RemoveAngularVelocity = reader.ReadBool(),
            PhysicsApplied = reader.ReadBool(),
        };
    }

    /// <summary>
    ///   Resets any accumulated impulse and rotation on this control. Used when enabling disabled bodies as the
    ///   bodies may have accumulated a ton of force from some system.
    /// </summary>
    public static void ResetAccumulatedForce(this ref ManualPhysicsControl manualPhysicsControl)
    {
        manualPhysicsControl.ImpulseToGive = Vector3.Zero;
        manualPhysicsControl.AngularImpulseToGive = Vector3.Zero;
    }
}
