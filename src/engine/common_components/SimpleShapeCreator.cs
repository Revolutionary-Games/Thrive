namespace Components;

using SharedBase.Archive;

/// <summary>
///   Allows entities to create simple shapes. Requires <see cref="PhysicsShapeHolder"/> to place the shape in.
/// </summary>
public struct SimpleShapeCreator : IArchivableComponent
{
    public const ushort SERIALIZATION_VERSION = 1;

    /// <summary>
    ///   Size of this shape. Depends on the shape type what this defines, most commonly this is the radius or
    ///   half-side length
    /// </summary>
    public float Size;

    /// <summary>
    ///   Density of the shape. 0 uses a default value
    /// </summary>
    public float Density;

    public SimpleShapeType ShapeType;

    /// <summary>
    ///   If this is set to true then when this shape is created it doesn't force a <see cref="Physics"/> to
    ///   recreate the body for the changed shape (if the body was already created). When false it is ensured that
    ///   the body gets recreated when the shape changes.
    /// </summary>
    public bool SkipForceRecreateBodyIfCreated;

    /// <summary>
    ///   Must be set to false if parameters are changed for the shape to be re-created
    /// </summary>
    public bool ShapeCreated;

    public SimpleShapeCreator(SimpleShapeType shapeType, float size, float density = 1000)
    {
        Size = size;
        Density = density;
        ShapeType = shapeType;

        // TODO: some shape types might need more parameters in the future so block them from using this constructor

        SkipForceRecreateBodyIfCreated = false;
        ShapeCreated = false;
    }

    public ushort CurrentArchiveVersion => SERIALIZATION_VERSION;
    public ThriveArchiveObjectType ArchiveObjectType => ThriveArchiveObjectType.ComponentSimpleShapeCreator;

    public void WriteToArchive(ISArchiveWriter writer)
    {
        writer.Write(Size);
        writer.Write(Density);
        writer.Write((int)ShapeType);
        writer.Write(SkipForceRecreateBodyIfCreated);
    }
}

public static class SimpleShapeCreatorHelpers
{
    public static SimpleShapeCreator ReadFromArchive(ISArchiveReader reader, ushort version)
    {
        if (version is > SimpleShapeCreator.SERIALIZATION_VERSION or <= 0)
            throw new InvalidArchiveVersionException(version, SimpleShapeCreator.SERIALIZATION_VERSION);

        return new SimpleShapeCreator
        {
            Size = reader.ReadFloat(),
            Density = reader.ReadFloat(),
            ShapeType = (SimpleShapeType)reader.ReadInt32(),
            SkipForceRecreateBodyIfCreated = reader.ReadBool(),
        };
    }
}
