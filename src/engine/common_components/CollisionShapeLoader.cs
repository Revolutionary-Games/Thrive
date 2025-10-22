namespace Components;

using SharedBase.Archive;

/// <summary>
///   Specifies a collision shape resource to be loaded into a <see cref="PhysicsShapeHolder"/>
/// </summary>
public struct CollisionShapeLoader : IArchivableComponent
{
    public const ushort SERIALIZATION_VERSION = 1;

    public string CollisionResourcePath;

    /// <summary>
    ///   Density of the shape. Only applies if <see cref="ApplyDensity"/> is true.
    /// </summary>
    public float Density;

    /// <summary>
    ///   If false, a default density (if known for the collision resource) is used
    /// </summary>
    public bool ApplyDensity;

    /// <summary>
    ///   If this is set to true then when this shape is created it doesn't force a <see cref="Physics"/> to
    ///   recreate the body for the changed shape (if the body was already created). When false, it is ensured that
    ///   the body gets recreated when the shape changes.
    /// </summary>
    public bool SkipForceRecreateBodyIfCreated;

    /// <summary>
    ///   Must be set to false if parameters are changed for the shape to be reloaded
    /// </summary>
    public bool ShapeLoaded;

    public CollisionShapeLoader(string resourcePath, float density)
    {
        CollisionResourcePath = resourcePath;
        Density = density;
        ApplyDensity = true;

        SkipForceRecreateBodyIfCreated = false;
        ShapeLoaded = false;
    }

    public ushort CurrentArchiveVersion => SERIALIZATION_VERSION;
    public ThriveArchiveObjectType ArchiveObjectType => ThriveArchiveObjectType.ComponentCollisionShapeLoader;

    public void WriteToArchive(ISArchiveWriter writer)
    {
        writer.Write(CollisionResourcePath);
        writer.Write(Density);
        writer.Write(ApplyDensity);
        writer.Write(SkipForceRecreateBodyIfCreated);
    }
}

public static class CollisionShapeLoaderHelpers
{
    public static CollisionShapeLoader ReadFromArchive(ISArchiveReader reader, ushort version)
    {
        if (version is > CollisionShapeLoader.SERIALIZATION_VERSION or <= 0)
            throw new InvalidArchiveVersionException(version, CollisionShapeLoader.SERIALIZATION_VERSION);

        return new CollisionShapeLoader
        {
            CollisionResourcePath = reader.ReadString()!,
            Density = reader.ReadFloat(),
            ApplyDensity = reader.ReadBool(),
            SkipForceRecreateBodyIfCreated = reader.ReadBool(),
        };
    }
}
