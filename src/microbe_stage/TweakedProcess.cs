using System;
using SharedBase.Archive;

/// <summary>
///   A concrete process that organelle does. Applies a modifier to the process
/// </summary>
/// <remarks>
///   <para>
///     This is a struct as this just packs a few values and a single object reference in here. This allows much tighter
///     data packing when this is used in lists.
///   </para>
/// </remarks>
public struct TweakedProcess : IEquatable<TweakedProcess>, IArchivable
{
    public const ushort SERIALIZATION_VERSION = 1;

    public readonly BioProcess Process;

    public float Rate;

    public float SpeedMultiplier = 1;

    /// <summary>
    ///   Indicates if this process is marked for a specific operation.
    /// </summary>
    /// <remarks>
    ///   <para>
    ///     Should be reset to false by any function using this field.
    ///   </para>
    /// </remarks>
    internal bool Marked;

    public TweakedProcess(BioProcess process, float rate = 1.0f)
    {
        Rate = rate;
        Process = process;
    }

    public ushort CurrentArchiveVersion => SERIALIZATION_VERSION;
    public ArchiveObjectType ArchiveObjectType => (ArchiveObjectType)ThriveArchiveObjectType.TweakedProcess;
    public bool CanBeReferencedInArchive => false;

    public static bool operator ==(TweakedProcess left, TweakedProcess right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(TweakedProcess left, TweakedProcess right)
    {
        return !(left == right);
    }

    public static void WriteToArchive(ISArchiveWriter writer, ArchiveObjectType type, object obj)
    {
        if (type != (ArchiveObjectType)ThriveArchiveObjectType.TweakedProcess)
            throw new NotSupportedException();

        writer.WriteObject((TweakedProcess)obj);
    }

    public static TweakedProcess ReadFromArchive(ISArchiveReader reader, ushort version, int referenceId)
    {
        if (version is > SERIALIZATION_VERSION or <= 0)
            throw new InvalidArchiveVersionException(version, SERIALIZATION_VERSION);

        return new TweakedProcess(reader.ReadObject<BioProcess>(), reader.ReadFloat())
        {
            SpeedMultiplier = reader.ReadFloat(),
        };
    }

    public static object ReadFromArchiveBoxed(ISArchiveReader reader, ushort version, int referenceId)
    {
        return ReadFromArchive(reader, version, referenceId);
    }

    public void WriteToArchive(ISArchiveWriter writer)
    {
        writer.WriteObject(Process);
        writer.Write(Rate);
        writer.Write(SpeedMultiplier);
    }

    public override bool Equals(object? obj)
    {
        if (obj is TweakedProcess casted)
            return Equals(casted);

        return false;
    }

    public bool Equals(TweakedProcess other)
    {
        if (Process != other.Process)
            return false;

        // This equality check may not be very strict, because otherwise the ProcessPanel breaks! Specifically
        // ProcessStatistics.GetAndMarkUsed doesn't return the correct entry
        return Rate == other.Rate && ReferenceEquals(Process, other.Process);
    }

    public TweakedProcess Clone()
    {
        return new TweakedProcess(Process, Rate);
    }

    public override int GetHashCode()
    {
        unchecked
        {
            return Rate.GetHashCode() * 397 ^ Process.GetHashCode();
        }
    }

    public override string ToString()
    {
        if (SpeedMultiplier != 1)
            return $"{Process} at {Rate}x (mult: {SpeedMultiplier})";

        return $"{Process} at {Rate}x";
    }
}
