using SharedBase.Archive;

/// <summary>
///   Serializable state of all cheats managed by <see cref="CheatManager"/>
/// </summary>
public class CheatManagerState : IArchivable
{
    public const ushort SERIALIZATION_VERSION = 1;

    public CheatManagerState()
    {
    }

    public CheatManagerState(bool infiniteCompounds, bool godMode, bool noAI, bool unlimitedGrowthSpeed,
        bool lockTime, bool manuallySetTime, float speed, bool infiniteMP, bool moveToAnyPatch,
        float dayNightFraction)
    {
        InfiniteCompounds = infiniteCompounds;
        GodMode = godMode;
        NoAI = noAI;
        UnlimitedGrowthSpeed = unlimitedGrowthSpeed;
        LockTime = lockTime;
        ManuallySetTime = manuallySetTime;
        Speed = speed;
        InfiniteMP = infiniteMP;
        MoveToAnyPatch = moveToAnyPatch;
        DayNightFraction = dayNightFraction;
    }

    public bool InfiniteCompounds { get; set; }
    public bool GodMode { get; set; }
    public bool NoAI { get; set; }
    public bool UnlimitedGrowthSpeed { get; set; }
    public bool LockTime { get; set; }
    public bool ManuallySetTime { get; set; }
    public float Speed { get; set; } = 1.0f;
    public bool InfiniteMP { get; set; }
    public bool MoveToAnyPatch { get; set; }
    public float DayNightFraction { get; set; }

    public ushort CurrentArchiveVersion => SERIALIZATION_VERSION;
    public ArchiveObjectType ArchiveObjectType => (ArchiveObjectType)ThriveArchiveObjectType.CheatManagerState;
    public bool CanBeReferencedInArchive => false;

    public static CheatManagerState ReadFromArchive(ISArchiveReader reader, ushort version, int referenceId)
    {
        if (version is > SERIALIZATION_VERSION or <= 0)
            throw new InvalidArchiveVersionException(version, SERIALIZATION_VERSION);

        var instance = new CheatManagerState
        {
            InfiniteCompounds = reader.ReadBool(),
            GodMode = reader.ReadBool(),
            NoAI = reader.ReadBool(),
            UnlimitedGrowthSpeed = reader.ReadBool(),
            LockTime = reader.ReadBool(),
            ManuallySetTime = reader.ReadBool(),
            Speed = reader.ReadFloat(),
            InfiniteMP = reader.ReadBool(),
            MoveToAnyPatch = reader.ReadBool(),
            DayNightFraction = reader.ReadFloat(),
        };

        return instance;
    }

    public void WriteToArchive(ISArchiveWriter writer)
    {
        writer.Write(InfiniteCompounds);
        writer.Write(GodMode);
        writer.Write(NoAI);
        writer.Write(UnlimitedGrowthSpeed);
        writer.Write(LockTime);
        writer.Write(ManuallySetTime);
        writer.Write(Speed);
        writer.Write(InfiniteMP);
        writer.Write(MoveToAnyPatch);
        writer.Write(DayNightFraction);
    }
}
