namespace Components;

using System.Collections.Generic;
using Godot;
using SharedBase.Archive;

/// <summary>
///   Entity has storage space for compounds
/// </summary>
public struct CompoundStorage : IArchivableComponent
{
    public const ushort SERIALIZATION_VERSION = 1;

    public CompoundBag Compounds;

    public ushort CurrentArchiveVersion => SERIALIZATION_VERSION;
    public ThriveArchiveObjectType ArchiveObjectType => ThriveArchiveObjectType.ComponentCompoundStorage;

    public void WriteToArchive(ISArchiveWriter writer)
    {
        writer.WriteObject(Compounds);
    }
}

public static class CompoundStorageHelpers
{
    public static CompoundStorage ReadFromArchive(ISArchiveReader reader, ushort version)
    {
        if (version is > CompoundStorage.SERIALIZATION_VERSION or <= 0)
            throw new InvalidArchiveVersionException(version, CompoundStorage.SERIALIZATION_VERSION);

        return new CompoundStorage
        {
            Compounds = reader.ReadObject<CompoundBag>(),
        };
    }

    /// <summary>
    ///   Vent all remaining compounds immediately
    /// </summary>
    public static void VentAllCompounds(this ref CompoundStorage storage, Vector3 position,
        CompoundCloudSystem compoundClouds)
    {
        if (storage.Compounds.Compounds.Count > 0)
        {
            var keys = new List<Compound>(storage.Compounds.Compounds.Keys);

            foreach (var compound in keys)
            {
                var amount = storage.Compounds.GetCompoundAmount(compound);
                storage.Compounds.TakeCompound(compound, amount);

                if (amount < MathUtils.EPSILON)
                    continue;

                storage.VentChunkCompound(compound, amount, position, compoundClouds);
            }
        }
    }

    public static bool VentChunkCompound(this ref CompoundStorage storage, Compound compound, float amount,
        Vector3 position, CompoundCloudSystem compoundClouds)
    {
        amount = storage.Compounds.TakeCompound(compound, amount);

        if (amount <= 0)
            return false;

        compoundClouds.AddCloud(compound, amount * Constants.CHUNK_VENT_COMPOUND_MULTIPLIER, position);
        return amount > MathUtils.EPSILON;
    }
}
