using System;
using System.Collections.Generic;
using Godot;
using SharedBase.Archive;

public class MacroscopicMetaball : Metaball, IArchivable
{
    public const ushort SERIALIZATION_VERSION = 1;

    public MacroscopicMetaball(CellType cellType)
    {
        CellType = cellType;
    }

    /// <summary>
    ///   The cell type this metaball consists of
    /// </summary>
    public CellType CellType { get; }

    public override Color Colour => CellType.Colour;

    public override ushort CurrentArchiveVersion => SERIALIZATION_VERSION;

    public override ArchiveObjectType ArchiveObjectType =>
        (ArchiveObjectType)ThriveArchiveObjectType.MacroscopicMetaball;

    public static void WriteToArchive(ISArchiveWriter writer, ArchiveObjectType type, object obj)
    {
        if (type != (ArchiveObjectType)ThriveArchiveObjectType.MacroscopicMetaball)
            throw new NotSupportedException();

        writer.WriteObject((MacroscopicMetaball)obj);
    }

    public static MacroscopicMetaball ReadFromArchive(ISArchiveReader reader, ushort version, int referenceId)
    {
        if (version is > SERIALIZATION_VERSION or <= 0)
            throw new InvalidArchiveVersionException(version, SERIALIZATION_VERSION);

        var instance = new MacroscopicMetaball(reader.ReadObject<CellType>());
        instance.ReadBasePropertiesFromArchive(reader, version);
        return instance;
    }

    public override void WriteToArchive(ISArchiveWriter writer)
    {
        writer.WriteObject(CellType);
        WriteBasePropertiesToArchive(writer);
    }

    public override bool MatchesDefinition(Metaball other)
    {
        if (other is MacroscopicMetaball asMulticellular)
        {
            return CellType == asMulticellular.CellType;
        }

        return false;
    }

    public override bool Equals(object? obj)
    {
        if (ReferenceEquals(null, obj))
            return false;
        if (ReferenceEquals(this, obj))
            return true;
        if (obj.GetType() != GetType())
            return false;

        return Equals((MacroscopicMetaball)obj);
    }

    /// <summary>
    ///   Clones this metaball while keeping the parent references intact.
    /// </summary>
    /// <param name="oldToNewMapping">
    ///   Where to find new reference to parent nodes. This will also add the newly cloned object here.
    /// </param>
    /// <returns>The clone of this</returns>
    public MacroscopicMetaball Clone(Dictionary<Metaball, MacroscopicMetaball> oldToNewMapping)
    {
        var clone = new MacroscopicMetaball(CellType)
        {
            Position = Position,
            Parent = Parent,
            Size = Size,
        };

        if (Parent != null)
        {
            if (oldToNewMapping.TryGetValue(Parent, out var newParent))
            {
                clone.Parent = newParent;
            }
        }

        oldToNewMapping[this] = clone;

        return clone;
    }

    public override int GetHashCode()
    {
        return CellType.GetHashCode() * 29 ^ base.GetHashCode();
    }

    protected bool Equals(MacroscopicMetaball other)
    {
        // This seems to cause infinite recursion, so this is not done for now and parents need to equal references
        // and not values
        // if (!ReferenceEquals(Parent, other.Parent))
        // {
        //     if (ReferenceEquals(Parent, null) && !ReferenceEquals(other.Parent, null))
        //         return false;
        //
        //     if (!ReferenceEquals(Parent, null) && ReferenceEquals(other.Parent, null))
        //         return false;
        //
        //     if (!Parent!.Equals(other.Parent))
        //         return false;
        // }

        if (!ReferenceEquals(Parent, other.Parent))
            return false;

        return CellType.Equals(other.CellType) && Position == other.Position;
    }
}
