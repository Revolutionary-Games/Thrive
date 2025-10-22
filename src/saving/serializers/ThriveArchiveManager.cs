namespace Saving.Serializers;

using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Arch.Core;
using AutoEvo;
using SharedBase.Archive;
using ThriveScriptsShared;
using Xoshiro.PRNG64;

/// <summary>
///   Thrive-customised archive manager that registers custom types
/// </summary>
public class ThriveArchiveManager : DefaultArchiveManager, ISaveContext
{
    public ThriveArchiveManager() : base(true)
    {
        RegisterThirdPartyTypes();

        // RegisterEngineTypes();

        // Register custom types for Thrive
        RegisterEnums();
        RegisterBaseObjects();
        RegisterRegistryTypes();
        RegisterOtherObjects();
    }

    /// <summary>
    ///   List of entities to not save when writing a world to a save
    /// </summary>
    public HashSet<Entity> UnsavedEntities { get; } = new();

    public World? ProcessedEntityWorld { get; set; }

    // TODO: should game stages be allowed to keep their player references with this? This is currently cleared after
    // an entity world is finished loading
    public Dictionary<Entity, Entity> OldToNewEntityMapping { get; } = new();

    public int ActiveProcessedWorldId { get; set; } = -1;

    public override void OnFinishWrite(ISArchiveWriter writer)
    {
        base.OnFinishWrite(writer);

        UnsavedEntities.Clear();
        ProcessedEntityWorld = null;
        OldToNewEntityMapping.Clear();
        ActiveProcessedWorldId = -1;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool SkipSavingEntity(in Entity value)
    {
        return UnsavedEntities.Contains(value);
    }

    private void RegisterThirdPartyTypes()
    {
        RegisterObjectType((ArchiveObjectType)ThriveArchiveObjectType.XoShiRo256StarStar, typeof(XoShiRo256starstar),
            ThirdPartyTypeHelpers.WriteXoShiRo256StarStar);
    }

    private void RegisterEnums()
    {
        RegisterEnumType((ArchiveObjectType)ThriveArchiveObjectType.CompoundEnum, ArchiveEnumType.UInt16,
            typeof(Compound));

        RegisterEnumType((ArchiveObjectType)ThriveArchiveObjectType.WorldEffectTypes, ArchiveEnumType.Int32,
            typeof(WorldEffectTypes));
    }

    private void RegisterBaseObjects()
    {
        RegisterObjectType((ArchiveObjectType)ThriveArchiveObjectType.Save, typeof(Save), Save.ReadFromArchive);

        RegisterObjectType((ArchiveObjectType)ThriveArchiveObjectType.LocalizedString, typeof(LocalizedString),
            LocalizedString.WriteToArchive);
        RegisterObjectType((ArchiveObjectType)ThriveArchiveObjectType.LocalizedStringBuilder,
            typeof(LocalizedStringBuilder), LocalizedStringBuilder.WriteToArchive);

        RegisterObjectType((ArchiveObjectType)ThriveArchiveObjectType.BiomeCompoundProperties,
            typeof(BiomeCompoundProperties), BiomeCompoundProperties.WriteToArchive);
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

        RegisterObjectType((ArchiveObjectType)ThriveArchiveObjectType.SpeciesInfo,
            typeof(SpeciesInfo), SpeciesInfo.WriteToArchive);

        RegisterObjectType((ArchiveObjectType)ThriveArchiveObjectType.Species,
            typeof(Species), Species.WriteToArchive);

        RegisterObjectType((ArchiveObjectType)ThriveArchiveObjectType.OrganelleTemplate,
            typeof(OrganelleTemplate), OrganelleTemplate.WriteToArchive);

        RegisterObjectType((ArchiveObjectType)ThriveArchiveObjectType.GameEventDescription,
            typeof(GameEventDescription), GameEventDescription.WriteToArchive);

        RegisterObjectType((ArchiveObjectType)ThriveArchiveObjectType.ChunkConfiguration,
            typeof(ChunkConfiguration), ChunkConfiguration.WriteToArchive);

        RegisterObjectType((ArchiveObjectType)ThriveArchiveObjectType.ChunkScene,
            typeof(ChunkConfiguration.ChunkScene), ChunkConfiguration.ChunkScene.WriteToArchive);

        RegisterObjectType((ArchiveObjectType)ThriveArchiveObjectType.ChunkCompound,
            typeof(ChunkConfiguration.ChunkCompound), ChunkConfiguration.ChunkCompound.WriteToArchive);

        RegisterObjectType((ArchiveObjectType)ThriveArchiveObjectType.Patch,
            typeof(Patch), Patch.WriteToArchive);

        RegisterObjectType((ArchiveObjectType)ThriveArchiveObjectType.PatchRegion,
            typeof(PatchRegion), PatchRegion.WriteToArchive);

        RegisterObjectType((ArchiveObjectType)ThriveArchiveObjectType.PatchSnapshot,
            typeof(PatchSnapshot), PatchSnapshot.WriteToArchive);

        RegisterObjectType((ArchiveObjectType)ThriveArchiveObjectType.BaseWorldEffect,
            typeof(IWorldEffect), IWorldEffect.WriteToArchive);

        RegisterObjectType((ArchiveObjectType)ThriveArchiveObjectType.ExternalEffect,
            typeof(ExternalEffect), ExternalEffect.WriteToArchive);
    }
}
