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
        RegisterEnumType((ArchiveObjectType)ThriveArchiveObjectType.CompoundEnum, ArchiveEnumType.UInt16,
            typeof(Compound));
    }

    private void RegisterBaseObjects()
    {
        RegisterObjectType((ArchiveObjectType)ThriveArchiveObjectType.Save, typeof(Save), Save.ReadFromArchive);

        RegisterObjectType((ArchiveObjectType)ThriveArchiveObjectType.LocalizedString, typeof(LocalizedString),
            LocalizedString.WriteToArchive);
        RegisterObjectType((ArchiveObjectType)ThriveArchiveObjectType.LocalizedStringBuilder,
            typeof(LocalizedStringBuilder), LocalizedStringBuilder.WriteToArchive);
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

        RegisterObjectType((ArchiveObjectType)ThriveArchiveObjectType.Species,
            typeof(Species), Species.WriteToArchive);

        RegisterObjectType((ArchiveObjectType)ThriveArchiveObjectType.OrganelleTemplate,
            typeof(OrganelleTemplate), OrganelleTemplate.WriteToArchive);

        RegisterObjectType((ArchiveObjectType)ThriveArchiveObjectType.GameEventDescription,
            typeof(GameEventDescription), GameEventDescription.WriteToArchive);
    }
}
