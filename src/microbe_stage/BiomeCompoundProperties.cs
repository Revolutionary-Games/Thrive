using System;
using Newtonsoft.Json;
using SharedBase.Archive;

/// <summary>
///   Properties of a compound in a given <see cref="BiomeConditions"/>. Has info on how common / available the
///   compound is.
/// </summary>
public struct BiomeCompoundProperties : IEquatable<BiomeCompoundProperties>, IArchivable
{
    public const ushort SERIALIZATION_VERSION = 1;

    /// <summary>
    ///   How much compound there is in each spawned cloud (<see cref="CompoundCloudSystem"/>) of this type
    /// </summary>
    public float Amount;

    /// <summary>
    ///   How often clouds of this compound spawn. Higher density means clouds of this type are more likely to spawn.
    /// </summary>
    public float Density;

    /// <summary>
    ///   When not zero, this says how much of this compound there is available as an environmental compound uniformly
    ///   distributed (available all around in this biome for all cells to use). Usually if this is non-zero
    ///   <see cref="Amount"/> and <see cref="Density"/> are zero.
    /// </summary>
    public float Ambient;

    [JsonIgnore]
    public ushort CurrentArchiveVersion => SERIALIZATION_VERSION;

    [JsonIgnore]
    public ArchiveObjectType ArchiveObjectType => (ArchiveObjectType)ThriveArchiveObjectType.BiomeCompoundProperties;

    [JsonIgnore]
    public bool CanBeReferencedInArchive => false;

    public static bool operator ==(BiomeCompoundProperties left, BiomeCompoundProperties right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(BiomeCompoundProperties left, BiomeCompoundProperties right)
    {
        return !(left == right);
    }

    public static void WriteToArchive(ISArchiveWriter writer, ArchiveObjectType type, object obj)
    {
        if (type != (ArchiveObjectType)ThriveArchiveObjectType.BiomeCompoundProperties)
            throw new NotSupportedException();

        ((BiomeCompoundProperties)obj).WriteToArchive(writer);
    }

    public static BiomeCompoundProperties ReadFromArchive(ISArchiveReader reader, ushort version)
    {
        if (version is > SERIALIZATION_VERSION or <= 0)
            throw new InvalidArchiveVersionException(version, SERIALIZATION_VERSION);

        return new BiomeCompoundProperties
        {
            Amount = reader.ReadFloat(),
            Density = reader.ReadFloat(),
            Ambient = reader.ReadFloat(),
        };
    }

    public void WriteToArchive(ISArchiveWriter writer)
    {
        writer.Write(Amount);
        writer.Write(Density);
        writer.Write(Ambient);
    }

    /// <summary>
    ///   Clamps the density and ambient values to be in the given range
    /// </summary>
    public void Clamp(float min, float max)
    {
        Density = Math.Clamp(Density, min, max);
        Ambient = Math.Clamp(Ambient, min, max);

        // Amount is in different units so it isn't clamped
        // Amount = Math.Clamp(Amount, min, max);
    }

    public override bool Equals(object? obj)
    {
        if (obj is BiomeCompoundProperties other)
        {
            return Equals(other);
        }

        return false;
    }

    public override int GetHashCode()
    {
        return (int)(Amount + Density + Ambient);
    }

    public bool Equals(BiomeCompoundProperties other)
    {
        return Amount == other.Amount && Density == other.Density && Ambient == other.Ambient;
    }

    public override string ToString()
    {
        return $"Amount: {Amount} ({Density} density), ambient: {Ambient}";
    }
}
