namespace Saving.Serializers;

using AutoEvo;
using SharedBase.Archive;
using ThriveScriptsShared;

/// <summary>
///   Thrive-customised archive manager that registers custom types
/// </summary>
public class ThriveArchiveManager : DefaultArchiveManager
{
    public ThriveArchiveManager() : base(true)
    {
        // Register custom types for Thrive
        RegisterEnums();
        RegisterBaseObjects();
        RegisterRegistryTypes();
        RegisterOtherObjects();
    }

    private void RegisterEnums()
    {
        // This uses a few extra bytes, but it shouldn't matter as custom enums write object headers anyway
        RegisterEnumType((ArchiveObjectType)ThriveArchiveObjectType.CompoundEnum, ArchiveEnumType.Int32,
            typeof(Compound));
    }

    private void RegisterBaseObjects()
    {
        RegisterObjectType((ArchiveObjectType)ThriveArchiveObjectType.Save, typeof(Save), Save.ReadFromArchive);
    }

    private void RegisterRegistryTypes()
    {
        // These work with their internal names rather than actual objects
        RegisterObjectType((ArchiveObjectType)ThriveArchiveObjectType.OrganelleDefinition, typeof(OrganelleDefinition),
            RegistryType.WriteToArchive);
        RegisterObjectType((ArchiveObjectType)ThriveArchiveObjectType.Biome, typeof(Biome),
            RegistryType.WriteToArchive);
    }

    private void RegisterOtherObjects()
    {
        RegisterObjectType((ArchiveObjectType)ThriveArchiveObjectType.ReproductionOrganelleData,
            typeof(ReproductionStatistic.ReproductionOrganelleData),
            ReproductionStatistic.ReproductionOrganelleData.WriteToArchive);

        RegisterObjectType((ArchiveObjectType)ThriveArchiveObjectType.GenerationRecord,
            typeof(GenerationRecord), GenerationRecord.WriteToArchive);
        RegisterObjectType((ArchiveObjectType)ThriveArchiveObjectType.SpeciesRecordLite,
            typeof(SpeciesRecordLite), SpeciesRecordLite.WriteToArchive);
    }
}
