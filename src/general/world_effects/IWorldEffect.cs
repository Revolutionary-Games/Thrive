using SharedBase.Archive;

/// <summary>
///   Time-dependent effects running on a world
/// </summary>
public interface IWorldEffect : IArchivable
{
    /// <summary>
    ///   Called when added to a world. The best time to do dynamic casts
    /// </summary>
    public void OnRegisterToWorld();

    public void OnTimePassed(double elapsed, double totalTimePassed);

    public static void WriteToArchive(ISArchiveWriter writer, ArchiveObjectType type, object obj)
    {
        writer.WriteObject((IWorldEffect)obj);
    }

    public static IWorldEffect ReadFromArchive(ISArchiveReader reader, ushort version)
    {
        return reader.ReadObject<IWorldEffect>();
    }
}
