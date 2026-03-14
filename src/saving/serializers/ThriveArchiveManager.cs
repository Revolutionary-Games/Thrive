namespace Saving.Serializers;

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Arch.Core;
using AutoEvo;
using Components;
using Godot;
using SharedBase.Archive;
using Systems;
using ThriveScriptsShared;
using Tutorial;
using Xoshiro.PRNG64;
using PatchMap = PatchMap;

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
        RegisterTutorial();
        RegisterAutoEvoStuff();
        RegisterEditor();
        RegisterFossils();
    }

    /// <summary>
    ///   List of entities to not save when writing a world to a save
    /// </summary>
    public HashSet<Entity> UnsavedEntities { get; } = new();

    public World? ProcessedEntityWorld { get; set; }

    // TODO: should game stages be allowed to keep their player references with this? This is currently cleared after
    // an entity world is finished loading
    public Dictionary<Entity, Entity> OldToNewEntityMapping { get; } = new();

    public int ActiveProcessedOldWorldId { get; set; } = -1;

    public override void OnFinishWrite(ISArchiveWriter writer)
    {
        base.OnFinishWrite(writer);

        UnsavedEntities.Clear();

        if (ProcessedEntityWorld != null)
            throw new FormatException("Some archive writer forgot to deactivate current entity world");

        OldToNewEntityMapping.Clear();
        ActiveProcessedOldWorldId = -1;
    }

    public override void OnFinishRead(ISArchiveReader reader)
    {
        base.OnFinishRead(reader);

        if (ProcessedEntityWorld != null)
            throw new FormatException("Some reader forgot to deactivate current entity world");

        OldToNewEntityMapping.Clear();
        ActiveProcessedOldWorldId = -1;
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
            ArchiveValueTypeHelpers.WriteVector2IBoxed);
        RegisterObjectType((ArchiveObjectType)ThriveArchiveObjectType.Vector2I, typeof(Vector2I),
            ArchiveValueTypeHelpers.ReadVector2IBoxed);
    }

    private void RegisterEnums()
    {
        RegisterEnumType((ArchiveObjectType)ThriveArchiveObjectType.CompoundEnum, ArchiveEnumType.UInt16,
            typeof(Compound));

        RegisterEnumType((ArchiveObjectType)ThriveArchiveObjectType.PatchEventTypes, ArchiveEnumType.Int32,
            typeof(PatchEventTypes));

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
        RegisterObjectType((ArchiveObjectType)ThriveArchiveObjectType.AutoEvoConfiguration,
            typeof(AutoEvoConfiguration), AutoEvoConfiguration.ReadFromArchive);
        RegisterObjectType((ArchiveObjectType)ThriveArchiveObjectType.TerrainConfiguration,
            typeof(TerrainConfiguration), TerrainConfiguration.ReadFromArchive);
        RegisterObjectType((ArchiveObjectType)ThriveArchiveObjectType.Meteor, typeof(Meteor),
            Meteor.ReadFromArchive);
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

        RegisterObjectType((ArchiveObjectType)ThriveArchiveObjectType.OrganelleTemplate,
            typeof(OrganelleTemplate), RegistryType.WriteToArchive);
        RegisterObjectType((ArchiveObjectType)ThriveArchiveObjectType.OrganelleTemplate,
            typeof(OrganelleTemplate), OrganelleTemplate.ReadFromArchive);

        RegisterObjectType((ArchiveObjectType)ThriveArchiveObjectType.CellTemplate,
            typeof(CellTemplate), CellTemplate.WriteToArchive);
        RegisterObjectType((ArchiveObjectType)ThriveArchiveObjectType.CellTemplate,
            typeof(CellTemplate), CellTemplate.ReadFromArchive);
        RegisterObjectType((ArchiveObjectType)ThriveArchiveObjectType.CellType,
            typeof(CellType), CellType.WriteToArchive);
        RegisterObjectType((ArchiveObjectType)ThriveArchiveObjectType.CellType,
            typeof(CellType), CellType.ReadFromArchive);

        RegisterObjectType((ArchiveObjectType)ThriveArchiveObjectType.OrganelleUpgrades, typeof(OrganelleUpgrades),
            OrganelleUpgrades.ReadFromArchive);
        RegisterObjectType((ArchiveObjectType)ThriveArchiveObjectType.LysosomeUpgrades, typeof(LysosomeUpgrades),
            LysosomeUpgrades.ReadFromArchive);
        RegisterObjectType((ArchiveObjectType)ThriveArchiveObjectType.FlagellumUpgrades, typeof(FlagellumUpgrades),
            FlagellumUpgrades.ReadFromArchive);
        RegisterObjectType((ArchiveObjectType)ThriveArchiveObjectType.StorageComponentUpgrades,
            typeof(StorageComponentUpgrades),
            StorageComponentUpgrades.ReadFromArchive);
        RegisterObjectType((ArchiveObjectType)ThriveArchiveObjectType.ToxinUpgrades, typeof(ToxinUpgrades),
            ToxinUpgrades.ReadFromArchive);
        RegisterObjectType((ArchiveObjectType)ThriveArchiveObjectType.ChemoreceptorUpgrades,
            typeof(ChemoreceptorUpgrades),
            ChemoreceptorUpgrades.ReadFromArchive);

        RegisterObjectType((ArchiveObjectType)ThriveArchiveObjectType.ExtendedOrganelleLayout,
            typeof(OrganelleLayout<>), HexLayoutSerializer.ReadFromArchive);
        RegisterObjectType((ArchiveObjectType)ThriveArchiveObjectType.OrganelleLayout,
            typeof(OrganelleLayout<>), HexLayoutSerializer.ReadFromArchive);
        RegisterObjectType((ArchiveObjectType)ThriveArchiveObjectType.ExtendedCellLayout,
            typeof(CellLayout<>), HexLayoutSerializer.ReadFromArchive);
        RegisterObjectType((ArchiveObjectType)ThriveArchiveObjectType.CellLayout,
            typeof(CellLayout<>), HexLayoutSerializer.ReadFromArchive);
        RegisterObjectType((ArchiveObjectType)ThriveArchiveObjectType.ExtendedIndividualHexLayout,
            typeof(IndividualHexLayout<>), HexLayoutSerializer.ReadIndividualHexLayoutFromArchive);
        RegisterObjectType((ArchiveObjectType)ThriveArchiveObjectType.IndividualHexLayout,
            typeof(IndividualHexLayout<>), HexLayoutSerializer.ReadIndividualHexLayoutFromArchive);

        RegisterObjectType((ArchiveObjectType)ThriveArchiveObjectType.ExtendedHexWithData,
            typeof(HexWithData<>), HexLayoutSerializer.WriteHexWithDataToArchive);
        RegisterObjectType((ArchiveObjectType)ThriveArchiveObjectType.ExtendedHexWithData,
            typeof(HexWithData<>), HexLayoutSerializer.ReadHexWithDataFromArchive);
        RegisterObjectType((ArchiveObjectType)ThriveArchiveObjectType.HexWithData,
            typeof(HexWithData<>), HexLayoutSerializer.ReadHexWithDataFromArchive);

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
        RegisterObjectType((ArchiveObjectType)ThriveArchiveObjectType.ExternalEffect,
            typeof(ExternalEffect), ExternalEffect.ReadFromArchive);

        RegisterObjectType((ArchiveObjectType)ThriveArchiveObjectType.PlacedOrganelle,
            typeof(PlacedOrganelle), PlacedOrganelle.WriteToArchive);
        RegisterObjectType((ArchiveObjectType)ThriveArchiveObjectType.PlacedOrganelle,
            typeof(PlacedOrganelle), PlacedOrganelle.ReadFromArchive);

        RegisterObjectType((ArchiveObjectType)ThriveArchiveObjectType.TweakedProcess,
            typeof(TweakedProcess), TweakedProcess.WriteToArchive);
        RegisterObjectType((ArchiveObjectType)ThriveArchiveObjectType.TweakedProcess,
            typeof(TweakedProcess), TweakedProcess.ReadFromArchiveBoxed);

        RegisterObjectType((ArchiveObjectType)ThriveArchiveObjectType.MicrobeStage,
            typeof(MicrobeStage), MicrobeStage.WriteToArchive);
        RegisterObjectType((ArchiveObjectType)ThriveArchiveObjectType.MicrobeStage,
            typeof(MicrobeStage), MicrobeStage.ReadFromArchive);

        RegisterObjectType((ArchiveObjectType)ThriveArchiveObjectType.MicrobeWorldSimulation,
            typeof(MicrobeWorldSimulation), MicrobeWorldSimulation.ReadFromArchive);

        RegisterObjectType((ArchiveObjectType)ThriveArchiveObjectType.CompoundBag,
            typeof(CompoundBag), CompoundBag.ReadFromArchive);

        RegisterObjectType((ArchiveObjectType)ThriveArchiveObjectType.CustomDifficulty,
            typeof(CustomDifficulty), CustomDifficulty.ReadFromArchive);

        RegisterBaseClass((ArchiveObjectType)ThriveArchiveObjectType.PlayerReadableName, typeof(IPlayerReadableName));

        RegisterObjectType((ArchiveObjectType)ThriveArchiveObjectType.Endosymbiont,
            typeof(Endosymbiont), Endosymbiont.WriteToArchive);
        RegisterObjectType((ArchiveObjectType)ThriveArchiveObjectType.Endosymbiont,
            typeof(Endosymbiont), Endosymbiont.ReadFromArchive);

        RegisterObjectType((ArchiveObjectType)ThriveArchiveObjectType.InProgressEndosymbiosis,
            typeof(EndosymbiosisData.InProgressEndosymbiosis),
            EndosymbiosisData.InProgressEndosymbiosis.ReadFromArchive);

        RegisterObjectType((ArchiveObjectType)ThriveArchiveObjectType.AgentProperties,
            typeof(AgentProperties), AgentProperties.ReadFromArchive);

        RegisterObjectType((ArchiveObjectType)ThriveArchiveObjectType.PatchEventProperties,
            typeof(PatchEventProperties), PatchEventProperties.WriteToArchive);
        RegisterObjectType((ArchiveObjectType)ThriveArchiveObjectType.PatchEventProperties,
            typeof(PatchEventProperties), PatchEventProperties.ReadFromArchive);
    }

    private void RegisterComponentParts()
    {
        RegisterObjectType((ArchiveObjectType)ThriveArchiveObjectType.SoundEffectSlot,
            typeof(SoundEffectSlot), SoundEffectSlot.WriteToArchive);
        RegisterObjectType((ArchiveObjectType)ThriveArchiveObjectType.SoundEffectSlot,
            typeof(SoundEffectSlot), SoundEffectSlot.ReadFromArchiveBoxed);

        RegisterObjectType((ArchiveObjectType)ThriveArchiveObjectType.DamageEventNotice,
            typeof(DamageEventNotice), DamageEventNotice.WriteToArchive);
        RegisterObjectType((ArchiveObjectType)ThriveArchiveObjectType.DamageEventNotice,
            typeof(DamageEventNotice), DamageEventNotice.ReadFromArchive);

        RegisterObjectType((ArchiveObjectType)ThriveArchiveObjectType.MicrobeTerrainSystem,
            typeof(MicrobeTerrainSystem), MicrobeTerrainSystem.WriteToArchive);
        RegisterObjectType((ArchiveObjectType)ThriveArchiveObjectType.MicrobeTerrainSystem,
            typeof(MicrobeTerrainSystem), MicrobeTerrainSystem.ReadFromArchive);

        RegisterObjectType((ArchiveObjectType)ThriveArchiveObjectType.SpawnSystem,
            typeof(SpawnSystem), SpawnSystem.ReadFromArchive);

        RegisterObjectType((ArchiveObjectType)ThriveArchiveObjectType.SpawnedTerrainCluster,
            typeof(MicrobeTerrainSystem.SpawnedTerrainCluster),
            MicrobeTerrainSystem.SpawnedTerrainCluster.WriteToArchive);
        RegisterObjectType((ArchiveObjectType)ThriveArchiveObjectType.SpawnedTerrainCluster,
            typeof(MicrobeTerrainSystem.SpawnedTerrainCluster),
            MicrobeTerrainSystem.SpawnedTerrainCluster.ReadFromArchiveBoxed);

        RegisterObjectType((ArchiveObjectType)ThriveArchiveObjectType.SpawnedTerrainGroup,
            typeof(SpawnedTerrainGroup), SpawnedTerrainGroup.WriteToArchive);
        RegisterObjectType((ArchiveObjectType)ThriveArchiveObjectType.SpawnedTerrainGroup,
            typeof(SpawnedTerrainGroup), SpawnedTerrainGroup.ReadFromArchive);

        RegisterObjectType((ArchiveObjectType)ThriveArchiveObjectType.CompoundCloudPlane,
            typeof(CompoundCloudPlane), CompoundCloudPlane.WriteToArchive);
        RegisterObjectType((ArchiveObjectType)ThriveArchiveObjectType.CompoundCloudPlane,
            typeof(CompoundCloudPlane), CompoundCloudPlane.ReadFromArchive);

        RegisterObjectType((ArchiveObjectType)ThriveArchiveObjectType.BulkTransportAnimation,
            typeof(Engulfable.BulkTransportAnimation), Engulfable.BulkTransportAnimation.ReadFromArchive);

        RegisterObjectType((ArchiveObjectType)ThriveArchiveObjectType.EntityLightConfig,
            typeof(EntityLight.Light), EntityLight.Light.WriteToArchive);
        RegisterObjectType((ArchiveObjectType)ThriveArchiveObjectType.EntityLightConfig,
            typeof(EntityLight.Light), EntityLight.Light.ReadFromArchiveBoxed);
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
        RegisterObjectType((ArchiveObjectType)ThriveArchiveObjectType.UnderwaterVentEruptionEvent,
            typeof(UnderwaterVentEruptionEvent), UnderwaterVentEruptionEvent.ReadFromArchive);
        RegisterObjectType((ArchiveObjectType)ThriveArchiveObjectType.RunoffEvent,
            typeof(RunoffEvent), RunoffEvent.ReadFromArchive);
        RegisterObjectType((ArchiveObjectType)ThriveArchiveObjectType.UpwellingEvent,
            typeof(UpwellingEvent), UpwellingEvent.ReadFromArchive);
        RegisterObjectType((ArchiveObjectType)ThriveArchiveObjectType.CurrentDilutionEvent,
            typeof(CurrentDilutionEvent), CurrentDilutionEvent.ReadFromArchive);
        RegisterObjectType((ArchiveObjectType)ThriveArchiveObjectType.PatchEventsManager,
            typeof(PatchEventsManager), PatchEventsManager.ReadFromArchive);

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
        RegisterObjectType((ArchiveObjectType)ThriveArchiveObjectType.AmmoniaProductionEffect,
            typeof(AmmoniaProductionEffect), AmmoniaProductionEffect.ReadFromArchive);
    }

    private void RegisterTutorial()
    {
        RegisterObjectType((ArchiveObjectType)ThriveArchiveObjectType.TutorialState,
            typeof(TutorialState), TutorialState.ReadFromArchive);

        RegisterObjectType((ArchiveObjectType)ThriveArchiveObjectType.TutorialMicrobeReproduction,
            typeof(MicrobeReproduction), MicrobeReproduction.ReadFromArchive);
    }

    private void RegisterAutoEvoStuff()
    {
        RegisterObjectType((ArchiveObjectType)ThriveArchiveObjectType.RunResults,
            typeof(RunResults), RunResults.WriteToArchive);
        RegisterObjectType((ArchiveObjectType)ThriveArchiveObjectType.RunResults,
            typeof(RunResults), RunResults.ReadFromArchive);

        RegisterObjectType((ArchiveObjectType)ThriveArchiveObjectType.SpeciesPatchEnergyResults,
            typeof(RunResults.SpeciesPatchEnergyResults), RunResults.SpeciesPatchEnergyResults.WriteToArchive);
        RegisterObjectType((ArchiveObjectType)ThriveArchiveObjectType.SpeciesPatchEnergyResults,
            typeof(RunResults.SpeciesPatchEnergyResults), RunResults.SpeciesPatchEnergyResults.ReadFromArchive);

        RegisterObjectType((ArchiveObjectType)ThriveArchiveObjectType.NicheInfo,
            typeof(RunResults.SpeciesPatchEnergyResults.NicheInfo),
            RunResults.SpeciesPatchEnergyResults.NicheInfo.WriteToArchive);
        RegisterObjectType((ArchiveObjectType)ThriveArchiveObjectType.NicheInfo,
            typeof(RunResults.SpeciesPatchEnergyResults.NicheInfo),
            RunResults.SpeciesPatchEnergyResults.NicheInfo.ReadFromArchive);

        RegisterObjectType((ArchiveObjectType)ThriveArchiveObjectType.SpeciesResult,
            typeof(RunResults.SpeciesResult), RunResults.SpeciesResult.WriteToArchive);
        RegisterObjectType((ArchiveObjectType)ThriveArchiveObjectType.SpeciesResult,
            typeof(RunResults.SpeciesResult), RunResults.SpeciesResult.ReadFromArchive);

        RegisterObjectType((ArchiveObjectType)ThriveArchiveObjectType.SpeciesMigration,
            typeof(SpeciesMigration), SpeciesMigration.WriteToArchive);
        RegisterObjectType((ArchiveObjectType)ThriveArchiveObjectType.SpeciesMigration,
            typeof(SpeciesMigration), SpeciesMigration.ReadFromArchive);

        RegisterObjectType((ArchiveObjectType)ThriveArchiveObjectType.Miche,
            typeof(Miche), Miche.WriteToArchive);
        RegisterObjectType((ArchiveObjectType)ThriveArchiveObjectType.Miche,
            typeof(Miche), Miche.ReadFromArchive);

        // Pressures
        RegisterObjectType((ArchiveObjectType)ThriveArchiveObjectType.AvoidPredationSelectionPressure,
            typeof(AvoidPredationSelectionPressure), AvoidPredationSelectionPressure.ReadFromArchive);
        RegisterObjectType((ArchiveObjectType)ThriveArchiveObjectType.ChunkCompoundPressure,
            typeof(ChunkCompoundPressure), ChunkCompoundPressure.ReadFromArchive);
        RegisterObjectType((ArchiveObjectType)ThriveArchiveObjectType.CompoundCloudPressure,
            typeof(CompoundCloudPressure), CompoundCloudPressure.ReadFromArchive);
        RegisterObjectType((ArchiveObjectType)ThriveArchiveObjectType.CompoundConversionEfficiencyPressure,
            typeof(CompoundConversionEfficiencyPressure), CompoundConversionEfficiencyPressure.ReadFromArchive);
        RegisterObjectType((ArchiveObjectType)ThriveArchiveObjectType.EndosymbiosisPressure,
            typeof(EndosymbiosisPressure), EndosymbiosisPressure.ReadFromArchive);
        RegisterObjectType((ArchiveObjectType)ThriveArchiveObjectType.EnvironmentalCompoundPressure,
            typeof(EnvironmentalCompoundPressure), EnvironmentalCompoundPressure.ReadFromArchive);
        RegisterObjectType((ArchiveObjectType)ThriveArchiveObjectType.EnvironmentalTolerancePressure,
            typeof(EnvironmentalTolerancePressure), EnvironmentalTolerancePressure.ReadFromArchive);
        RegisterObjectType((ArchiveObjectType)ThriveArchiveObjectType.MaintainCompoundPressure,
            typeof(MaintainCompoundPressure), MaintainCompoundPressure.ReadFromArchive);
        RegisterObjectType((ArchiveObjectType)ThriveArchiveObjectType.MetabolicStabilityPressure,
            typeof(MetabolicStabilityPressure), MetabolicStabilityPressure.ReadFromArchive);
        RegisterObjectType((ArchiveObjectType)ThriveArchiveObjectType.NoOpPressure,
            typeof(NoOpPressure), NoOpPressure.ReadFromArchive);
        RegisterObjectType((ArchiveObjectType)ThriveArchiveObjectType.PredationEffectivenessPressure,
            typeof(PredationEffectivenessPressure), PredationEffectivenessPressure.ReadFromArchive);
        RegisterObjectType((ArchiveObjectType)ThriveArchiveObjectType.PredatorRoot,
            typeof(PredatorRoot), PredatorRoot.ReadFromArchive);
        RegisterObjectType((ArchiveObjectType)ThriveArchiveObjectType.RootPressure,
            typeof(RootPressure), RootPressure.ReadFromArchive);
        RegisterObjectType((ArchiveObjectType)ThriveArchiveObjectType.TemperatureSessilityPressure,
            typeof(TemperatureSessilityPressure), TemperatureSessilityPressure.ReadFromArchive);
        RegisterObjectType((ArchiveObjectType)ThriveArchiveObjectType.ReproductionCompoundPressure,
            typeof(ReproductionCompoundPressure), ReproductionCompoundPressure.ReadFromArchive);
        RegisterObjectType((ArchiveObjectType)ThriveArchiveObjectType.GeneralAvoidPredationSelectionPressure,
            typeof(GeneralAvoidPredationSelectionPressure), GeneralAvoidPredationSelectionPressure.ReadFromArchive);
        RegisterObjectType((ArchiveObjectType)ThriveArchiveObjectType.EnergyConsumptionPressure,
            typeof(EnergyConsumptionPressure), EnergyConsumptionPressure.ReadFromArchive);
    }

    private void RegisterEditor()
    {
        // Need to be registered to work as a base class
        RegisterObjectType((ArchiveObjectType)ThriveArchiveObjectType.EditorAction,
            typeof(EditorAction), ReversibleAction.WriteToArchive);
        RegisterBaseClass((ArchiveObjectType)ThriveArchiveObjectType.EditorAction, typeof(EditorAction));

        RegisterObjectType((ArchiveObjectType)ThriveArchiveObjectType.ExtendedActionHistory,
            typeof(ActionHistory<>), ActionHistorySerializer.WriteToArchive);
        RegisterObjectType((ArchiveObjectType)ThriveArchiveObjectType.ExtendedActionHistory,
            typeof(ActionHistory<>), ActionHistorySerializer.ReadFromArchive);

        RegisterObjectType((ArchiveObjectType)ThriveArchiveObjectType.ExtendedEditorActionHistory,
            typeof(EditorActionHistory<>), ActionHistorySerializer.WriteToArchive);
        RegisterObjectType((ArchiveObjectType)ThriveArchiveObjectType.ExtendedEditorActionHistory,
            typeof(EditorActionHistory<>), ActionHistorySerializer.ReadFromArchive);
        RegisterExtendedBase((ArchiveObjectType)ThriveArchiveObjectType.ExtendedEditorActionHistory,
            (ArchiveObjectType)ThriveArchiveObjectType.EditorActionHistory, typeof(EditorActionHistory<>));

        RegisterObjectType((ArchiveObjectType)ThriveArchiveObjectType.BehaviourActionData,
            typeof(BehaviourActionData), BehaviourActionData.WriteToArchive);
        RegisterObjectType((ArchiveObjectType)ThriveArchiveObjectType.BehaviourActionData,
            typeof(BehaviourActionData), BehaviourActionData.ReadFromArchive);

        RegisterObjectType((ArchiveObjectType)ThriveArchiveObjectType.ColourActionData,
            typeof(ColourActionData), ColourActionData.WriteToArchive);
        RegisterObjectType((ArchiveObjectType)ThriveArchiveObjectType.ColourActionData,
            typeof(ColourActionData), ColourActionData.ReadFromArchive);

        RegisterObjectType((ArchiveObjectType)ThriveArchiveObjectType.MembraneActionData,
            typeof(MembraneActionData), MembraneActionData.WriteToArchive);
        RegisterObjectType((ArchiveObjectType)ThriveArchiveObjectType.MembraneActionData,
            typeof(MembraneActionData), MembraneActionData.ReadFromArchive);

        RegisterObjectType((ArchiveObjectType)ThriveArchiveObjectType.NewMicrobeActionData,
            typeof(NewMicrobeActionData), NewMicrobeActionData.WriteToArchive);
        RegisterObjectType((ArchiveObjectType)ThriveArchiveObjectType.NewMicrobeActionData,
            typeof(NewMicrobeActionData), NewMicrobeActionData.ReadFromArchive);

        RegisterObjectType((ArchiveObjectType)ThriveArchiveObjectType.OrganelleMoveActionData,
            typeof(OrganelleMoveActionData), OrganelleMoveActionData.WriteToArchive);
        RegisterObjectType((ArchiveObjectType)ThriveArchiveObjectType.OrganelleMoveActionData,
            typeof(OrganelleMoveActionData), OrganelleMoveActionData.ReadFromArchive);

        RegisterObjectType((ArchiveObjectType)ThriveArchiveObjectType.OrganelleUpgradeActionData,
            typeof(OrganelleUpgradeActionData), OrganelleUpgradeActionData.WriteToArchive);
        RegisterObjectType((ArchiveObjectType)ThriveArchiveObjectType.OrganelleUpgradeActionData,
            typeof(OrganelleUpgradeActionData), OrganelleUpgradeActionData.ReadFromArchive);

        RegisterObjectType((ArchiveObjectType)ThriveArchiveObjectType.RigidityActionData,
            typeof(RigidityActionData), RigidityActionData.WriteToArchive);
        RegisterObjectType((ArchiveObjectType)ThriveArchiveObjectType.RigidityActionData,
            typeof(RigidityActionData), RigidityActionData.ReadFromArchive);

        RegisterObjectType((ArchiveObjectType)ThriveArchiveObjectType.ToleranceActionData,
            typeof(ToleranceActionData), ToleranceActionData.WriteToArchive);
        RegisterObjectType((ArchiveObjectType)ThriveArchiveObjectType.ToleranceActionData,
            typeof(ToleranceActionData), ToleranceActionData.ReadFromArchive);

        RegisterObjectType((ArchiveObjectType)ThriveArchiveObjectType.MetaballMoveActionData,
            typeof(MetaballMoveActionData<>), MetaballActionDataSerializer.WriteToArchive);
        RegisterObjectType((ArchiveObjectType)ThriveArchiveObjectType.MetaballMoveActionData,
            typeof(MetaballMoveActionData<>), MetaballActionDataSerializer.ReadMoveFromArchive);

        RegisterObjectType((ArchiveObjectType)ThriveArchiveObjectType.MetaballPlacementActionData,
            typeof(MetaballPlacementActionData<>), MetaballActionDataSerializer.WriteToArchive);
        RegisterObjectType((ArchiveObjectType)ThriveArchiveObjectType.MetaballPlacementActionData,
            typeof(MetaballPlacementActionData<>), MetaballActionDataSerializer.ReadPlacementFromArchive);

        RegisterObjectType((ArchiveObjectType)ThriveArchiveObjectType.MetaballRemoveActionData,
            typeof(MetaballRemoveActionData<>), MetaballActionDataSerializer.WriteToArchive);
        RegisterObjectType((ArchiveObjectType)ThriveArchiveObjectType.MetaballRemoveActionData,
            typeof(MetaballRemoveActionData<>), MetaballActionDataSerializer.ReadRemoveFromArchive);

        RegisterObjectType((ArchiveObjectType)ThriveArchiveObjectType.MetaballResizeActionData,
            typeof(MetaballResizeActionData<>), MetaballActionDataSerializer.WriteToArchive);
        RegisterObjectType((ArchiveObjectType)ThriveArchiveObjectType.MetaballResizeActionData,
            typeof(MetaballResizeActionData<>), MetaballActionDataSerializer.ReadResizeFromArchive);

        RegisterObjectType((ArchiveObjectType)ThriveArchiveObjectType.PatchDetailsMigration,
            typeof(PatchDetailsPanel.Migration), PatchDetailsPanel.Migration.WriteToArchive);
        RegisterObjectType((ArchiveObjectType)ThriveArchiveObjectType.PatchDetailsMigration,
            typeof(PatchDetailsPanel.Migration), PatchDetailsPanel.Migration.ReadFromArchive);

        RegisterObjectType((ArchiveObjectType)ThriveArchiveObjectType.OrganellePlacementActionData,
            typeof(OrganellePlacementActionData), OrganellePlacementActionData.WriteToArchive);
        RegisterObjectType((ArchiveObjectType)ThriveArchiveObjectType.OrganellePlacementActionData,
            typeof(OrganellePlacementActionData), OrganellePlacementActionData.ReadFromArchive);

        RegisterObjectType((ArchiveObjectType)ThriveArchiveObjectType.EndosymbiontPlaceActionData,
            typeof(EndosymbiontPlaceActionData), EndosymbiontPlaceActionData.WriteToArchive);
        RegisterObjectType((ArchiveObjectType)ThriveArchiveObjectType.EndosymbiontPlaceActionData,
            typeof(EndosymbiontPlaceActionData), EndosymbiontPlaceActionData.ReadFromArchive);

        RegisterObjectType((ArchiveObjectType)ThriveArchiveObjectType.OrganelleRemoveActionData,
            typeof(OrganelleRemoveActionData), OrganelleRemoveActionData.WriteToArchive);
        RegisterObjectType((ArchiveObjectType)ThriveArchiveObjectType.OrganelleRemoveActionData,
            typeof(OrganelleRemoveActionData), OrganelleRemoveActionData.ReadFromArchive);

        RegisterLimitedObjectType((ArchiveObjectType)ThriveArchiveObjectType.CellEditorComponent,
            typeof(CellEditorComponent));
        RegisterLimitedObjectType((ArchiveObjectType)ThriveArchiveObjectType.TolerancesEditorSubComponent,
            typeof(TolerancesEditorSubComponent));
        RegisterLimitedObjectType((ArchiveObjectType)ThriveArchiveObjectType.BehaviourEditorSubComponent,
            typeof(BehaviourEditorSubComponent));
        RegisterLimitedObjectType((ArchiveObjectType)ThriveArchiveObjectType.CellBodyPlanEditorComponent,
            typeof(CellBodyPlanEditorComponent));

        RegisterObjectType((ArchiveObjectType)ThriveArchiveObjectType.MicrobeEditor,
            typeof(MicrobeEditor), MicrobeEditor.ReadFromArchive);

        RegisterObjectType((ArchiveObjectType)ThriveArchiveObjectType.MulticellularEditor,
            typeof(MulticellularEditor), MulticellularEditor.ReadFromArchive);

        RegisterObjectType((ArchiveObjectType)ThriveArchiveObjectType.CombinedEditorAction,
            typeof(CombinedEditorAction), CombinedEditorAction.ReadFromArchive);

        RegisterObjectType((ArchiveObjectType)ThriveArchiveObjectType.ExtendedSingleEditorAction,
            typeof(SingleEditorAction<>), EditorActionSerializer.ReadFromArchive);
        RegisterObjectType((ArchiveObjectType)ThriveArchiveObjectType.SingleEditorAction,
            typeof(SingleEditorAction<>), EditorActionSerializer.ReadFromArchive);
        RegisterObjectType((ArchiveObjectType)ThriveArchiveObjectType.SingleEditorAction,
            typeof(SingleEditorAction<>), EditorActionSerializer.WriteToArchive);

        RegisterObjectType((ArchiveObjectType)ThriveArchiveObjectType.CellPlacementActionData,
            typeof(CellPlacementActionData), CellPlacementActionData.WriteToArchive);
        RegisterObjectType((ArchiveObjectType)ThriveArchiveObjectType.CellPlacementActionData,
            typeof(CellPlacementActionData), CellPlacementActionData.ReadFromArchive);
        RegisterObjectType((ArchiveObjectType)ThriveArchiveObjectType.CellMoveActionData,
            typeof(CellMoveActionData), CellMoveActionData.WriteToArchive);
        RegisterObjectType((ArchiveObjectType)ThriveArchiveObjectType.CellMoveActionData,
            typeof(CellMoveActionData), CellMoveActionData.ReadFromArchive);
        RegisterObjectType((ArchiveObjectType)ThriveArchiveObjectType.CellRemoveActionData,
            typeof(CellRemoveActionData), CellRemoveActionData.WriteToArchive);
        RegisterObjectType((ArchiveObjectType)ThriveArchiveObjectType.CellRemoveActionData,
            typeof(CellRemoveActionData), CellRemoveActionData.ReadFromArchive);
        RegisterObjectType((ArchiveObjectType)ThriveArchiveObjectType.DuplicateDeleteCellTypeData,
            typeof(DuplicateDeleteCellTypeData), DuplicateDeleteCellTypeData.WriteToArchive);
        RegisterObjectType((ArchiveObjectType)ThriveArchiveObjectType.DuplicateDeleteCellTypeData,
            typeof(DuplicateDeleteCellTypeData), DuplicateDeleteCellTypeData.ReadFromArchive);
    }

    private void RegisterFossils()
    {
        RegisterObjectType((ArchiveObjectType)ThriveArchiveObjectType.FossilisedSpecies,
            typeof(FossilisedSpecies), FossilisedSpecies.ReadFromArchive);

        RegisterObjectType((ArchiveObjectType)ThriveArchiveObjectType.FossilisedSpeciesInformation,
            typeof(FossilisedSpeciesInformation), FossilisedSpeciesInformation.ReadFromArchive);
    }
}
