using System;
using SharedBase.Archive;

/// <summary>
///   Action that can be undone and redone
/// </summary>
public abstract class ReversibleAction : IArchivable
{
    public const ushort SERIALIZATION_VERSION_REVERSIBLE = 1;

    /// <summary>
    ///   True when the action has been performed and can be undone
    /// </summary>
    public bool Performed { get; private set; }

    public abstract ushort CurrentArchiveVersion { get; }
    public abstract ArchiveObjectType ArchiveObjectType { get; }
    public virtual bool CanBeReferencedInArchive => true;

    public static void WriteToArchive(ISArchiveWriter writer, ArchiveObjectType type, object obj)
    {
        writer.WriteObject((IArchivable)obj);
    }

    public virtual void WriteToArchive(ISArchiveWriter writer)
    {
        writer.Write(Performed);
    }

    /// <summary>
    ///   Does this action
    /// </summary>
    public void Perform()
    {
        if (Performed)
            throw new InvalidOperationException("cannot perform already performed action");

        DoAction();
        Performed = true;
    }

    /// <summary>
    ///   Undoes this action
    /// </summary>
    public void Undo()
    {
        if (!Performed)
            throw new InvalidOperationException("cannot undo not performed action");

        UndoAction();
        Performed = false;
    }

    // Subclass callbacks
    public abstract void DoAction();
    public abstract void UndoAction();

    public virtual void OnDeserializerFirstPartComplete(ISArchiveReader reader)
    {
        ReadBasePropertiesFromArchive(reader, reader.ReadUInt16());
    }

    protected virtual void ReadBasePropertiesFromArchive(ISArchiveReader reader, ushort version)
    {
        if (version is > SERIALIZATION_VERSION_REVERSIBLE or <= 0)
            throw new InvalidArchiveVersionException(version, SERIALIZATION_VERSION_REVERSIBLE);

        Performed = reader.ReadBool();
    }
}
