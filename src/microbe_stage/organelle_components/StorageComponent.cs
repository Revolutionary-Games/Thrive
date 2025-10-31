using SharedBase.Archive;
using ThriveScriptsShared;

/// <summary>
///   Allows cell to store more stuff
/// </summary>
public class StorageComponent : EmptyOrganelleComponent
{
    public StorageComponent(float capacity)
    {
        Capacity = capacity;
    }

    public float Capacity { get; }
}

public class StorageComponentFactory : IOrganelleComponentFactory
{
    public float Capacity;

    public IOrganelleComponent Create()
    {
        return new StorageComponent(Capacity);
    }

    public void Check(string name)
    {
        if (Capacity <= 0.0f)
        {
            throw new InvalidRegistryDataException(name, GetType().Name,
                "Storage component capacity must be > 0.0f");
        }
    }
}

public class StorageComponentUpgrades : IComponentSpecificUpgrades
{
    public const ushort SERIALIZATION_VERSION = 1;

    public StorageComponentUpgrades(Compound specializedFor)
    {
        SpecializedFor = specializedFor;
    }

    public Compound SpecializedFor { get; set; }

    public ushort CurrentArchiveVersion => SERIALIZATION_VERSION;

    public ArchiveObjectType ArchiveObjectType => (ArchiveObjectType)ThriveArchiveObjectType.StorageComponentUpgrades;

    public bool CanBeReferencedInArchive => false;

    public static StorageComponentUpgrades ReadFromArchive(ISArchiveReader reader, ushort version, int referenceId)
    {
        if (version is > SERIALIZATION_VERSION or <= 0)
            throw new InvalidArchiveVersionException(version, SERIALIZATION_VERSION);

        return new StorageComponentUpgrades((Compound)reader.ReadInt32());
    }

    public void WriteToArchive(ISArchiveWriter writer)
    {
        writer.Write((int)SpecializedFor);
    }

    public bool Equals(IComponentSpecificUpgrades? other)
    {
        if (other is not StorageComponentUpgrades otherVacuole)
            return false;

        return SpecializedFor == otherVacuole.SpecializedFor;
    }

    public object Clone()
    {
        return new StorageComponentUpgrades(SpecializedFor);
    }

    public override int GetHashCode()
    {
        return int.RotateLeft(SpecializedFor.GetHashCode(), 7);
    }

    public ulong GetVisualHashCode()
    {
        // Specialization doesn't affect the visuals
        return 7;
    }
}
