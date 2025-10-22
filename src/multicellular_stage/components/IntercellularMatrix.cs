namespace Components;

using Godot;
using SharedBase.Archive;

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
