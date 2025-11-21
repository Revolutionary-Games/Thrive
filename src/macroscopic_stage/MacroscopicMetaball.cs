using System;
using System.Collections.Generic;
using Godot;
using SharedBase.Archive;

public class MacroscopicMetaball : Metaball, IReadonlyMacroscopicMetaball, IArchivable
{
    public const ushort SERIALIZATION_VERSION = 1;

    public MacroscopicMetaball(CellType cellType)
    {
        ModifiableCellType = cellType;
    }

    /// <summary>
    ///   The cell type this metaball consists of. Should not change after creation. This is protected to allow a
    ///   caching type to work.
    /// </summary>
    public CellType ModifiableCellType { get; protected set; }

    public IReadOnlyCellDefinition CellType => ModifiableCellType;

    public override Color Colour => ModifiableCellType.Colour;

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
        writer.WriteObject(ModifiableCellType);
        WriteBasePropertiesToArchive(writer);
    }

    public override bool MatchesDefinition(Metaball other)
    {
        if (other is MacroscopicMetaball asMulticellular)
        {
            return ModifiableCellType == asMulticellular.ModifiableCellType;
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
    ///   Where to find new references to parent nodes. This will also add the newly cloned object here.
    /// </param>
    /// <returns>The clone of this</returns>
    public MacroscopicMetaball Clone(Dictionary<Metaball, MacroscopicMetaball> oldToNewMapping)
    {
        var clone = new MacroscopicMetaball(ModifiableCellType)
        {
            Position = Position,
            ModifiableParent = ModifiableParent,
            Size = Size,
        };

        if (ModifiableParent != null)
        {
            if (oldToNewMapping.TryGetValue(ModifiableParent, out var newParent))
            {
                clone.ModifiableParent = newParent;
            }
        }

        oldToNewMapping[this] = clone;

        return clone;
    }

    public override int GetHashCode()
    {
        return ModifiableCellType.GetHashCode() * 29 ^ base.GetHashCode();
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

        return ModifiableCellType.Equals(other.ModifiableCellType) && Position == other.Position;
    }
}
