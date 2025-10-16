using SharedBase.Archive;

public abstract class GenericStatistic<T> : IStatistic, IArchiveUpdatable
{
    public const ushort SERIALIZATION_VERSION = 1;

    public GenericStatistic(T value)
    {
        Value = value;
    }

    public T Value { get; protected set; }

    public ushort CurrentArchiveVersion => SERIALIZATION_VERSION;
    public abstract ArchiveObjectType ArchiveObjectType { get; }

    public abstract void Increment(T value);

    public void WritePropertiesToArchive(ISArchiveWriter writer)
    {
        writer.WriteAnyRegisteredValueAsObject(Value);
    }

    public void ReadPropertiesFromArchive(ISArchiveReader reader, ushort version)
    {
        if (version is > SERIALIZATION_VERSION or <= 0)
            throw new InvalidArchiveVersionException(version, SERIALIZATION_VERSION);

        Value = reader.ReadObjectNotNull<T>();
    }
}

public class SimpleStatistic : GenericStatistic<int>
{
    public SimpleStatistic() : this(0)
    {
    }

    public SimpleStatistic(int value) : base(value)
    {
    }

    public override ArchiveObjectType ArchiveObjectType => (ArchiveObjectType)ThriveArchiveObjectType.SimpleStatistic;

    public override void Increment(int value)
    {
        Value += value;
    }
}
