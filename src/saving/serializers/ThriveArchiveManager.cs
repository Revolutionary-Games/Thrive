namespace Saving.Serializers;

using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Arch.Core;
using AutoEvo;
using Components;
using Godot;
using SharedBase.Archive;
using Systems;
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
        RegisterEngineTypes();

        // Register custom types for Thrive
        RegisterEnums();
        RegisterBaseObjects();
        RegisterRegistryTypes();
        RegisterOtherObjects();
        RegisterComponentParts();
        RegisterWorldEffects();
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
        RegisterObjectType((ArchiveObjectType)ThriveArchiveObjectType.XoShiRo256StarStar, typeof(XoShiRo256starstar),
            ThirdPartyTypeHelpers.ReadXoShiRo256StarStarFromArchive);

        RegisterObjectType((ArchiveObjectType)ThriveArchiveObjectType.EntityWorld, typeof(World),
            EntityWorldSerializers.WriteEntityWorldToArchive);
        RegisterObjectType((ArchiveObjectType)ThriveArchiveObjectType.EntityWorld, typeof(World),
            EntityWorldSerializers.ReadEntityWorldFromArchive);

        RegisterObjectType((ArchiveObjectType)ThriveArchiveObjectType.Entity, typeof(Entity),
            EntityWorldSerializers.WriteEntityReferenceToArchive);
        RegisterObjectType((ArchiveObjectType)ThriveArchiveObjectType.Entity, typeof(Entity),
            EntityWorldSerializers.ReadEntityReferenceFromArchiveBoxed);
    }

    private void RegisterEngineTypes()
    {
        RegisterObjectType((ArchiveObjectType)ThriveArchiveObjectType.Vector2I, typeof(Vector2I),
            ArchiveValueTypeHelpers.WriteVector2I);
    }

    private void RegisterEnums()
    {
        RegisterEnumType((ArchiveObjectType)ThriveArchiveObjectType.CompoundEnum, ArchiveEnumType.UInt16,
            typeof(Compound));

        RegisterEnumType((ArchiveObjectType)ThriveArchiveObjectType.WorldEffectTypes, ArchiveEnumType.Int32,
            typeof(WorldEffectTypes));

        RegisterEnumType((ArchiveObjectType)ThriveArchiveObjectType.ToxinType, ArchiveEnumType.Int32,
            typeof(ToxinType));
    }

    private void RegisterBaseObjects()
    {
        RegisterObjectType((ArchiveObjectType)ThriveArchiveObjectType.Save, typeof(Save), Save.ReadFromArchive);
        RegisterObjectType((ArchiveObjectType)ThriveArchiveObjectType.SaveInformation, typeof(SaveInformation),
            SaveInformation.ReadFromArchive);

        RegisterObjectType((ArchiveObjectType)ThriveArchiveObjectType.LocalizedString, typeof(LocalizedString),
            LocalizedString.WriteToArchive);
        RegisterObjectType((ArchiveObjectType)ThriveArchiveObjectType.LocalizedString, typeof(LocalizedString),
            LocalizedString.ReadFromArchive);
        RegisterObjectType((ArchiveObjectType)ThriveArchiveObjectType.LocalizedStringBuilder,
            typeof(LocalizedStringBuilder), LocalizedStringBuilder.WriteToArchive);
        RegisterObjectType((ArchiveObjectType)ThriveArchiveObjectType.LocalizedStringBuilder,
            typeof(LocalizedStringBuilder), LocalizedStringBuilder.ReadFromArchive);

        RegisterObjectType((ArchiveObjectType)ThriveArchiveObjectType.BiomeConditions,
            typeof(BiomeConditions), BiomeConditions.ReadFromArchive);

        RegisterObjectType((ArchiveObjectType)ThriveArchiveObjectType.BiomeCompoundProperties,
            typeof(BiomeCompoundProperties), BiomeCompoundProperties.WriteToArchive);
        RegisterObjectType((ArchiveObjectType)ThriveArchiveObjectType.BiomeCompoundProperties,
            typeof(BiomeCompoundProperties), BiomeCompoundProperties.ReadFromArchiveBoxed);
    }

    private void RegisterRegistryTypes()
    {
        // These work with their internal names rather than actual objects
        RegisterObjectType((ArchiveObjectType)ThriveArchiveObjectType.OrganelleDefinition, typeof(OrganelleDefinition),
            RegistryType.WriteToArchive);
        RegisterObjectType((ArchiveObjectType)ThriveArchiveObjectType.OrganelleDefinition, typeof(OrganelleDefinition),
            OrganelleDefinition.ReadFromArchive);
        RegisterObjectType((ArchiveObjectType)ThriveArchiveObjectType.Biome, typeof(Biome),
            RegistryType.WriteToArchive);
        RegisterObjectType((ArchiveObjectType)ThriveArchiveObjectType.Biome, typeof(Biome),
            Biome.ReadFromArchive);
        RegisterObjectType((ArchiveObjectType)ThriveArchiveObjectType.Enzyme, typeof(Enzyme),
            RegistryType.WriteToArchive);
        RegisterObjectType((ArchiveObjectType)ThriveArchiveObjectType.Enzyme, typeof(Enzyme),
            Enzyme.ReadFromArchive);
        RegisterObjectType((ArchiveObjectType)ThriveArchiveObjectType.BioProcess, typeof(BioProcess),
            RegistryType.WriteToArchive);
        RegisterObjectType((ArchiveObjectType)ThriveArchiveObjectType.BioProcess, typeof(BioProcess),
            BioProcess.ReadFromArchive);

        RegisterObjectType((ArchiveObjectType)ThriveArchiveObjectType.DifficultyPreset, typeof(DifficultyPreset),
            DifficultyPreset.ReadFromArchive);
        RegisterObjectType((ArchiveObjectType)ThriveArchiveObjectType.MembraneType, typeof(MembraneType),
            MembraneType.ReadFromArchive);
        RegisterObjectType((ArchiveObjectType)ThriveArchiveObjectType.PredefinedAutoEvoConfiguration,
            typeof(PredefinedAutoEvoConfiguration),
            PredefinedAutoEvoConfiguration.ReadFromArchive);

        RegisterObjectType((ArchiveObjectType)ThriveArchiveObjectType.OrganelleTemplate,
            typeof(OrganelleTemplate), RegistryType.WriteToArchive);
        RegisterObjectType((ArchiveObjectType)ThriveArchiveObjectType.OrganelleTemplate,
            typeof(OrganelleTemplate), OrganelleTemplate.ReadFromArchive);
    }

    private void RegisterOtherObjects()
    {
        RegisterObjectType((ArchiveObjectType)ThriveArchiveObjectType.GameProperties, typeof(GameProperties),
            GameProperties.ReadFromArchive);
        RegisterObjectType((ArchiveObjectType)ThriveArchiveObjectType.GameWorld, typeof(GameWorld),
            GameWorld.ReadFromArchive);

        RegisterObjectType((ArchiveObjectType)ThriveArchiveObjectType.WorldGenerationSettings,
            typeof(WorldGenerationSettings),
            WorldGenerationSettings.ReadFromArchive);

        RegisterObjectType((ArchiveObjectType)ThriveArchiveObjectType.DayNightCycle, typeof(DayNightCycle),
            DayNightCycle.ReadFromArchive);

        RegisterObjectType((ArchiveObjectType)ThriveArchiveObjectType.ReproductionOrganelleData,
            typeof(ReproductionStatistic.ReproductionOrganelleData),
            ReproductionStatistic.ReproductionOrganelleData.WriteToArchive);
        RegisterObjectType((ArchiveObjectType)ThriveArchiveObjectType.ReproductionOrganelleData,
            typeof(ReproductionStatistic.ReproductionOrganelleData),
            ReproductionStatistic.ReproductionOrganelleData.ReadFromArchive);

        RegisterObjectType((ArchiveObjectType)ThriveArchiveObjectType.GenerationRecord,
            typeof(GenerationRecord), GenerationRecord.WriteToArchive);
        RegisterObjectType((ArchiveObjectType)ThriveArchiveObjectType.GenerationRecord,
            typeof(GenerationRecord), GenerationRecord.ReadFromArchive);
        RegisterObjectType((ArchiveObjectType)ThriveArchiveObjectType.SpeciesRecordLite,
            typeof(SpeciesRecordLite), SpeciesRecordLite.WriteToArchive);
        RegisterObjectType((ArchiveObjectType)ThriveArchiveObjectType.SpeciesRecordLite,
            typeof(SpeciesRecordLite), SpeciesRecordLite.ReadFromArchive);

        RegisterObjectType((ArchiveObjectType)ThriveArchiveObjectType.SpeciesInfo,
            typeof(SpeciesInfo), SpeciesInfo.WriteToArchive);
        RegisterObjectType((ArchiveObjectType)ThriveArchiveObjectType.SpeciesInfo,
            typeof(SpeciesInfo), SpeciesInfo.ReadFromArchive);

        RegisterObjectType((ArchiveObjectType)ThriveArchiveObjectType.Species,
            typeof(Species), Species.WriteToArchive);
        RegisterObjectType((ArchiveObjectType)ThriveArchiveObjectType.Species,
            typeof(Species), Species.ReadFromArchive);
        RegisterObjectType((ArchiveObjectType)ThriveArchiveObjectType.MicrobeSpecies,
            typeof(MicrobeSpecies), (IArchiveReadManager.RestoreObjectDelegate)MicrobeSpecies.ReadFromArchive);
        RegisterObjectType((ArchiveObjectType)ThriveArchiveObjectType.MulticellularSpecies,
            typeof(MulticellularSpecies),
            (IArchiveReadManager.RestoreObjectDelegate)MulticellularSpecies.ReadFromArchive);

        RegisterObjectType((ArchiveObjectType)ThriveArchiveObjectType.ExtendedOrganelleLayout,
            typeof(OrganelleLayout<>), HexLayoutSerializer.ReadFromArchive);
        RegisterObjectType((ArchiveObjectType)ThriveArchiveObjectType.OrganelleLayout,
            typeof(OrganelleLayout<>), HexLayoutSerializer.ReadFromArchive);
        RegisterObjectType((ArchiveObjectType)ThriveArchiveObjectType.ExtendedCellLayout,
            typeof(CellLayout<>), HexLayoutSerializer.ReadFromArchive);
        RegisterObjectType((ArchiveObjectType)ThriveArchiveObjectType.CellLayout,
            typeof(CellLayout<>), HexLayoutSerializer.ReadFromArchive);
        RegisterObjectType((ArchiveObjectType)ThriveArchiveObjectType.ExtendedIndividualHexLayout,
            typeof(IndividualHexLayout<>), HexLayoutSerializer.ReadFromArchive);
        RegisterObjectType((ArchiveObjectType)ThriveArchiveObjectType.IndividualHexLayout,
            typeof(IndividualHexLayout<>), HexLayoutSerializer.ReadFromArchive);

        RegisterObjectType((ArchiveObjectType)ThriveArchiveObjectType.BehaviourDictionary,
            typeof(BehaviourDictionary), BehaviourDictionary.ReadFromArchive);

        RegisterObjectType((ArchiveObjectType)ThriveArchiveObjectType.GameEventDescription,
            typeof(GameEventDescription), GameEventDescription.WriteToArchive);
        RegisterObjectType((ArchiveObjectType)ThriveArchiveObjectType.GameEventDescription,
            typeof(GameEventDescription), GameEventDescription.ReadFromArchive);

        RegisterObjectType((ArchiveObjectType)ThriveArchiveObjectType.ChunkConfiguration,
            typeof(ChunkConfiguration), ChunkConfiguration.WriteToArchive);
        RegisterObjectType((ArchiveObjectType)ThriveArchiveObjectType.ChunkConfiguration,
            typeof(ChunkConfiguration), ChunkConfiguration.ReadFromArchiveBoxed);

        RegisterObjectType((ArchiveObjectType)ThriveArchiveObjectType.ChunkScene,
            typeof(ChunkConfiguration.ChunkScene), ChunkConfiguration.ChunkScene.WriteToArchive);
        RegisterObjectType((ArchiveObjectType)ThriveArchiveObjectType.ChunkScene,
            typeof(ChunkConfiguration.ChunkScene), ChunkConfiguration.ChunkScene.ReadFromArchive);

        RegisterObjectType((ArchiveObjectType)ThriveArchiveObjectType.ChunkCompound,
            typeof(ChunkConfiguration.ChunkCompound), ChunkConfiguration.ChunkCompound.WriteToArchive);
        RegisterObjectType((ArchiveObjectType)ThriveArchiveObjectType.ChunkCompound,
            typeof(ChunkConfiguration.ChunkCompound), ChunkConfiguration.ChunkCompound.ReadFromArchiveBoxed);

        RegisterObjectType((ArchiveObjectType)ThriveArchiveObjectType.Patch,
            typeof(Patch), Patch.WriteToArchive);
        RegisterObjectType((ArchiveObjectType)ThriveArchiveObjectType.Patch,
            typeof(Patch), Patch.ReadFromArchive);

        RegisterObjectType((ArchiveObjectType)ThriveArchiveObjectType.PatchRegion,
            typeof(PatchRegion), PatchRegion.WriteToArchive);
        RegisterObjectType((ArchiveObjectType)ThriveArchiveObjectType.PatchRegion,
            typeof(PatchRegion), PatchRegion.ReadFromArchive);

        RegisterObjectType((ArchiveObjectType)ThriveArchiveObjectType.PatchSnapshot,
            typeof(PatchSnapshot), PatchSnapshot.WriteToArchive);
        RegisterObjectType((ArchiveObjectType)ThriveArchiveObjectType.PatchSnapshot,
            typeof(PatchSnapshot), PatchSnapshot.ReadFromArchive);

        RegisterObjectType((ArchiveObjectType)ThriveArchiveObjectType.PatchMap,
            typeof(PatchMap), PatchMap.ReadFromArchive);

        RegisterObjectType((ArchiveObjectType)ThriveArchiveObjectType.ExternalEffect,
            typeof(ExternalEffect), ExternalEffect.WriteToArchive);

        RegisterObjectType((ArchiveObjectType)ThriveArchiveObjectType.PlacedOrganelle,
            typeof(PlacedOrganelle), PlacedOrganelle.WriteToArchive);
        RegisterObjectType((ArchiveObjectType)ThriveArchiveObjectType.PlacedOrganelle,
            typeof(PlacedOrganelle), PlacedOrganelle.ReadFromArchive);

        RegisterObjectType((ArchiveObjectType)ThriveArchiveObjectType.TweakedProcess,
            typeof(TweakedProcess), TweakedProcess.WriteToArchive);

        RegisterObjectType((ArchiveObjectType)ThriveArchiveObjectType.MicrobeStage,
            typeof(MicrobeStage), MicrobeStage.WriteToArchive);
    }

    private void RegisterComponentParts()
    {
        RegisterObjectType((ArchiveObjectType)ThriveArchiveObjectType.SoundEffectSlot,
            typeof(SoundEffectSlot), SoundEffectSlot.WriteToArchive);

        RegisterObjectType((ArchiveObjectType)ThriveArchiveObjectType.DamageEventNotice,
            typeof(DamageEventNotice), DamageEventNotice.WriteToArchive);

        RegisterObjectType((ArchiveObjectType)ThriveArchiveObjectType.MicrobeTerrainSystem,
            typeof(MicrobeTerrainSystem), MicrobeTerrainSystem.WriteToArchive);

        RegisterObjectType((ArchiveObjectType)ThriveArchiveObjectType.SpawnedTerrainCluster,
            typeof(MicrobeTerrainSystem.SpawnedTerrainCluster),
            MicrobeTerrainSystem.SpawnedTerrainCluster.WriteToArchive);

        RegisterObjectType((ArchiveObjectType)ThriveArchiveObjectType.SpawnedTerrainGroup,
            typeof(SpawnedTerrainGroup), SpawnedTerrainGroup.WriteToArchive);

        RegisterObjectType((ArchiveObjectType)ThriveArchiveObjectType.CompoundCloudPlane,
            typeof(CompoundCloudPlane), CompoundCloudPlane.WriteToArchive);
        RegisterObjectType((ArchiveObjectType)ThriveArchiveObjectType.CompoundCloudPlane,
            typeof(CompoundCloudPlane), CompoundCloudPlane.ReadFromArchive);
    }

    private void RegisterWorldEffects()
    {
        RegisterObjectType((ArchiveObjectType)ThriveArchiveObjectType.TimedWorldOperations,
            typeof(TimedWorldOperations), TimedWorldOperations.ReadFromArchive);

        RegisterObjectType((ArchiveObjectType)ThriveArchiveObjectType.BaseWorldEffect,
            typeof(IWorldEffect), IWorldEffect.WriteToArchive);
        RegisterObjectType((ArchiveObjectType)ThriveArchiveObjectType.BaseWorldEffect,
            typeof(IWorldEffect), IWorldEffect.ReadFromArchive);

        RegisterObjectType((ArchiveObjectType)ThriveArchiveObjectType.GlobalGlaciationEvent,
            typeof(GlobalGlaciationEvent), GlobalGlaciationEvent.ReadFromArchive);
        RegisterObjectType((ArchiveObjectType)ThriveArchiveObjectType.MeteorImpactEvent,
            typeof(MeteorImpactEvent), MeteorImpactEvent.ReadFromArchive);
        RegisterObjectType((ArchiveObjectType)ThriveArchiveObjectType.UnderwaterVentEruptionEffect,
            typeof(UnderwaterVentEruptionEffect), UnderwaterVentEruptionEffect.ReadFromArchive);
        RegisterObjectType((ArchiveObjectType)ThriveArchiveObjectType.GlucoseReductionEffect,
            typeof(GlucoseReductionEffect), GlucoseReductionEffect.ReadFromArchive);
        RegisterObjectType((ArchiveObjectType)ThriveArchiveObjectType.CompoundDiffusionEffect,
            typeof(CompoundDiffusionEffect), CompoundDiffusionEffect.ReadFromArchive);
        RegisterObjectType((ArchiveObjectType)ThriveArchiveObjectType.HydrogenSulfideConsumptionEffect,
            typeof(HydrogenSulfideConsumptionEffect), HydrogenSulfideConsumptionEffect.ReadFromArchive);
        RegisterObjectType((ArchiveObjectType)ThriveArchiveObjectType.NitrogenControlEffect,
            typeof(NitrogenControlEffect), NitrogenControlEffect.ReadFromArchive);
        RegisterObjectType((ArchiveObjectType)ThriveArchiveObjectType.IronOxidationEffect,
            typeof(IronOxidationEffect), IronOxidationEffect.ReadFromArchive);
        RegisterObjectType((ArchiveObjectType)ThriveArchiveObjectType.MarineSnowDensityEffect,
            typeof(MarineSnowDensityEffect), MarineSnowDensityEffect.ReadFromArchive);
        RegisterObjectType((ArchiveObjectType)ThriveArchiveObjectType.PhotosynthesisProductionEffect,
            typeof(PhotosynthesisProductionEffect), PhotosynthesisProductionEffect.ReadFromArchive);
        RegisterObjectType((ArchiveObjectType)ThriveArchiveObjectType.VolcanismEffect,
            typeof(VolcanismEffect), VolcanismEffect.ReadFromArchive);
    }
}
