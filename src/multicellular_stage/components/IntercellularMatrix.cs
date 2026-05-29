namespace Components;

using Godot;
using SharedBase.Archive;
using Systems;

public struct IntercellularMatrix : IArchivableComponent
{
    public const ushort SERIALIZATION_VERSION = 1;

    /// <summary>
    ///   True when the cell doesn't need a connection because it's already close enough, false otherwise.
    /// </summary>
    /// <remarks>
    ///   <para>
    ///     Should be reset to false when the cell gets disconnected.
    ///   </para>
    /// </remarks>
    public bool IsConnectionRedundant;

    public Node3D? GeneratedConnection;

    public ushort CurrentArchiveVersion => SERIALIZATION_VERSION;
    public ThriveArchiveObjectType ArchiveObjectType => ThriveArchiveObjectType.ComponentIntercellularMatrix;

    public void WriteToArchive(ISArchiveWriter writer)
    {
        // Nothing to save
    }

    /// <summary>
    ///   Removes this entity's connection. Note that if this cell is still a <see cref="MicrobeColonyMember"/>,
    ///   the connection will be restored by <see cref="IntercellularMatrixSystem"/>.
    /// </summary>
    public void RemoveConnection()
    {
        GeneratedConnection?.QueueFree();
        GeneratedConnection = null;
        IsConnectionRedundant = false;
    }
}

public static class IntercellularMatrixHelpers
{
    public static IntercellularMatrix ReadFromArchive(ISArchiveReader reader, ushort version)
    {
        if (version is > IntercellularMatrix.SERIALIZATION_VERSION or <= 0)
            throw new InvalidArchiveVersionException(version, IntercellularMatrix.SERIALIZATION_VERSION);

        return default(IntercellularMatrix);
    }
}
