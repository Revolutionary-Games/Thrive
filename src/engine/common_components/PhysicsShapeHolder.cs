namespace Components;

using SharedBase.Archive;

/// <summary>
///   Holds a physics shape once one is ready and then allows creating a physics body from it
/// </summary>
public struct PhysicsShapeHolder : IArchivableComponent
{
    public const ushort SERIALIZATION_VERSION = 1;

    public PhysicsShape? Shape;

    /// <summary>
    ///   When true, the body is created as a static body that cannot move
    /// </summary>
    public bool BodyIsStatic;

    /// <summary>
    ///   When true the related physics body will be updated from <see cref="Shape"/> when the shape is ready.
    ///   Will be automatically reset to false afterwards.
    /// </summary>
    public bool UpdateBodyShapeIfCreated;

    public ushort CurrentArchiveVersion => SERIALIZATION_VERSION;
    public ThriveArchiveObjectType ArchiveObjectType => ThriveArchiveObjectType.ComponentPhysicsShapeHolder;

    public void WriteToArchive(ISArchiveWriter writer)
    {
        writer.Write(BodyIsStatic);
        writer.Write(UpdateBodyShapeIfCreated);
    }
}

public static class PhysicsShapeHolderHelpers
{
    public static PhysicsShapeHolder ReadFromArchive(ISArchiveReader reader, ushort version)
    {
        if (version is > PhysicsShapeHolder.SERIALIZATION_VERSION or <= 0)
            throw new InvalidArchiveVersionException(version, PhysicsShapeHolder.SERIALIZATION_VERSION);

        return new PhysicsShapeHolder
        {
            BodyIsStatic = reader.ReadBool(),
            UpdateBodyShapeIfCreated = reader.ReadBool(),
        };
    }

    /// <summary>
    ///   Gets the mass of a shape holder's shape if exist (if it doesn't exist sets mass to 1000)
    /// </summary>
    /// <param name="shapeHolder">Shape holder to look at</param>
    /// <param name="mass">The found shape mass or 1000 if not found</param>
    /// <returns>True if mass was retrieved</returns>
    public static bool TryGetShapeMass(this ref PhysicsShapeHolder shapeHolder, out float mass)
    {
        if (shapeHolder.Shape == null)
        {
            mass = 1000;
            return false;
        }

        mass = shapeHolder.Shape.GetMass();
        return true;
    }
}
