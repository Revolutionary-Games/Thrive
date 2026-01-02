namespace Components;

using System.Collections.Generic;
using SharedBase.Archive;

/// <summary>
///   Specifies a collision shape resource to be loaded into a <see cref="PhysicsShapeHolder"/>
/// </summary>
public struct CollisionShapeLoader : IArchivableComponent
{
    public const ushort SERIALIZATION_VERSION = 2;

    public string? CollisionResourcePath;

    public List<ChunkConfiguration.ComplexCollisionShapeConfiguration>? ComplexCollisionShapes;

    public bool IsComplexCollision;

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
        IsComplexCollision = false;
        Density = density;
        ApplyDensity = true;

        SkipForceRecreateBodyIfCreated = false;
        ShapeLoaded = false;
    }

    public CollisionShapeLoader(List<ChunkConfiguration.ComplexCollisionShapeConfiguration> complexCollisionShapes,
        float density)
    {
        ComplexCollisionShapes = complexCollisionShapes;
        IsComplexCollision = true;
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
        writer.WriteObjectOrNull(ComplexCollisionShapes);
        writer.Write(IsComplexCollision);
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

        var collisionResourcePath = reader.ReadString();
        List<ChunkConfiguration.ComplexCollisionShapeConfiguration>? shapeConfigurations = new();

        if (version >= 2)
        {
            shapeConfigurations =
                reader.ReadObjectOrNull<List<ChunkConfiguration.ComplexCollisionShapeConfiguration>>();
        }

        var isComplexCollision = reader.ReadBool();
        var density = reader.ReadFloat();
        var applyDensity = reader.ReadBool();
        var skipForceRecreateBodyIfCreated = reader.ReadBool();

        return new CollisionShapeLoader
        {
            CollisionResourcePath = collisionResourcePath,
            ComplexCollisionShapes = shapeConfigurations,
            IsComplexCollision = isComplexCollision,
            Density = density,
            ApplyDensity = applyDensity,
            SkipForceRecreateBodyIfCreated = skipForceRecreateBodyIfCreated,
        };
    }
}
