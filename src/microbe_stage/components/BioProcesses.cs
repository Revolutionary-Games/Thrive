namespace Components;

using System.Collections.Generic;
using SharedBase.Archive;

/// <summary>
///   Entity has bio processes to run by the <see cref="Systems.ProcessSystem"/>
/// </summary>
public struct BioProcesses : IArchivableComponent
{
    public const ushort SERIALIZATION_VERSION = 1;

    /// <summary>
    ///   The active processes that ProcessSystem handles
    /// </summary>
    /// <remarks>
    ///   <para>
    ///     All processes that perform the same action should be combined rather than listing that process
    ///     multiple times in this list (as that results in unexpected things as that isn't semantically how this
    ///     property is meant to be structured)
    ///   </para>
    /// </remarks>
    public List<TweakedProcess>? ActiveProcesses;

    /// <summary>
    ///   If set to not-null process statistics are gathered here
    /// </summary>
    public ProcessStatistics? ProcessStatistics;

    /// <summary>
    ///   If not 0, then controls the speed at which ATP production processes may run (negative value disables ATP
    ///   generation as 0 means "no limit"). Setting this higher than one allows ATP processes to run faster than they
    ///   physically should be able to.
    /// </summary>
    public float ATPProductionSpeedModifier;

    /// <summary>
    ///   If not 0, then this sets an overall speed modifier on *all* processes
    /// </summary>
    public float OverallSpeedModifier;

    public ushort CurrentArchiveVersion => SERIALIZATION_VERSION;

    public ThriveArchiveObjectType ArchiveObjectType => ThriveArchiveObjectType.ComponentBioProcesses;

    public void WriteToArchive(ISArchiveWriter writer)
    {
        writer.WriteObjectOrNull(ActiveProcesses);
        writer.Write(ProcessStatistics != null);
        writer.Write(ATPProductionSpeedModifier);
        writer.Write(OverallSpeedModifier);
    }
}

public static class BioProcessesHelpers
{
    public static BioProcesses ReadFromArchive(ISArchiveReader reader, ushort version)
    {
        if (version is > BioProcesses.SERIALIZATION_VERSION or <= 0)
            throw new InvalidArchiveVersionException(version, BioProcesses.SERIALIZATION_VERSION);

        return new BioProcesses
        {
            ActiveProcesses = reader.ReadObjectOrNull<List<TweakedProcess>>(),
            ProcessStatistics = reader.ReadBool() ? new ProcessStatistics() : null,
            ATPProductionSpeedModifier = reader.ReadFloat(),
            OverallSpeedModifier = reader.ReadFloat(),
        };
    }
}
