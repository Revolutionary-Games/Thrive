namespace AutoEvo;

/// <summary>
///   Caches objects to be used in multiple auto-evo runs
/// </summary>
public class AutoEvoGlobalCache
{
    public readonly RootPressure RootPressure;
    public readonly MetabolicStabilityPressure MetabolicStabilityPressure;
    public readonly GeneralAvoidPredationSelectionPressure GeneralAvoidPredationSelectionPressure;
    public readonly EnergyConsumptionPressure EnergyConsumptionPressure;
    public readonly EnvironmentalTolerancePressure EnvironmentalTolerancesPressure;

    public readonly ReproductionCompoundPressure PhosphatePressure;
    public readonly ReproductionCompoundPressure AmmoniaPressure;

    public readonly CompoundConversionEfficiencyPressure MinorGlucoseConversionEfficiencyPressure;
    public readonly MaintainCompoundPressure MaintainGlucose;

    public readonly CompoundConversionEfficiencyPressure GlucoseConversionEfficiencyPressure;
    public readonly CompoundCloudPressure GlucoseCloudPressure;
    public readonly CompoundCloudEnergy GlucoseCloudEnergy;

    public readonly CompoundConversionEfficiencyPressure IronConversionEfficiencyPressure;
    public readonly ChunkCompoundPressure SmallIronChunkPressure;
    public readonly ChunkCompoundEnergy SmallIronChunkEnergy;
    public readonly ChunkCompoundPressure BigIronChunkPressure;
    public readonly ChunkCompoundEnergy BigIronChunkEnergy;

    public readonly CompoundConversionEfficiencyPressure HydrogenSulfideConversionEfficiencyPressure;
    public readonly CompoundCloudPressure HydrogenSulfideCloudPressure;
    public readonly CompoundCloudEnergy HydrogenSulfideCloudEnergy;
    public readonly ChunkCompoundPressure SmallSulfurChunkPressure;
    public readonly ChunkCompoundEnergy SmallSulfurChunkEnergy;
    public readonly ChunkCompoundPressure MediumSulfurChunkPressure;
    public readonly ChunkCompoundEnergy MediumSulfurChunkEnergy;
    public readonly ChunkCompoundPressure LargeSulfurChunkPressure;
    public readonly ChunkCompoundEnergy LargeSulfurChunkEnergy;

    public readonly CompoundConversionEfficiencyPressure SunlightConversionEfficiencyPressure;
    public readonly EnvironmentalCompoundPressure SunlightCompoundPressure;
    public readonly EnvironmentalCompoundEnergy SunlightCompoundEnergy;

    public readonly CompoundConversionEfficiencyPressure TemperatureConversionEfficiencyPressure;
    public readonly EnvironmentalCompoundPressure TemperatureCompoundPressure;
    public readonly EnvironmentalCompoundEnergy TemperatureCompoundEnergy;

    public readonly CompoundConversionEfficiencyPressure RadiationConversionEfficiencyPressure;
    public readonly ChunkCompoundPressure RadioactiveChunkPressure;
    public readonly ChunkCompoundEnergy RadioactiveChunkEnergy;

    public readonly PredatorRoot PredatorRoot;

    public readonly bool HasTemperature;

    public readonly TemperatureSessilityPressure TemperatureSessilityPressure;

    public AutoEvoGlobalCache(WorldGenerationSettings worldSettings)
    {
        RootPressure = new RootPressure();
        MetabolicStabilityPressure = new MetabolicStabilityPressure(1.0f);
        GeneralAvoidPredationSelectionPressure = new GeneralAvoidPredationSelectionPressure(1.0f);
        EnergyConsumptionPressure = new EnergyConsumptionPressure(0.3f);
        EnvironmentalTolerancesPressure = new EnvironmentalTolerancePressure(1.0f);

        PhosphatePressure = new ReproductionCompoundPressure(
            Compound.Phosphates, worldSettings.DayNightCycleEnabled, 0.03f);
        AmmoniaPressure = new ReproductionCompoundPressure(Compound.Ammonia, worldSettings.DayNightCycleEnabled, 0.03f);

        MinorGlucoseConversionEfficiencyPressure =
            new CompoundConversionEfficiencyPressure(Compound.Glucose, Compound.ATP, true, 0.45f);
        MaintainGlucose = new MaintainCompoundPressure(Compound.Glucose, 1.5f);

        GlucoseConversionEfficiencyPressure =
            new CompoundConversionEfficiencyPressure(Compound.Glucose, Compound.ATP, true, 1.5f);
        GlucoseCloudPressure = new CompoundCloudPressure(Compound.Glucose, worldSettings.DayNightCycleEnabled, 1.0f);
        GlucoseCloudEnergy = new CompoundCloudEnergy(Compound.Glucose, 0.1f);

        IronConversionEfficiencyPressure =
            new CompoundConversionEfficiencyPressure(Compound.Iron, Compound.ATP, true, 1.5f);
        SmallIronChunkPressure = new ChunkCompoundPressure("ironSmallChunk", new LocalizedString("SMALL_IRON_CHUNK"),
            Compound.Iron, Compound.ATP, 1.0f);
        SmallIronChunkEnergy = new ChunkCompoundEnergy("ironSmallChunk",
            new LocalizedString("SMALL_IRON_CHUNK_ENERGY"),
            Compound.Iron, 0.1f);
        BigIronChunkPressure = new ChunkCompoundPressure("ironBigChunk", new LocalizedString("BIG_IRON_CHUNK"),
            Compound.Iron, Compound.ATP, 1.0f);
        BigIronChunkEnergy = new ChunkCompoundEnergy("ironBigChunk", new LocalizedString("BIG_IRON_CHUNK_ENERGY"),
            Compound.Iron, 0.1f);

        HydrogenSulfideConversionEfficiencyPressure = new CompoundConversionEfficiencyPressure(Compound.Hydrogensulfide,
            Compound.Glucose, true, 2.0f);
        HydrogenSulfideCloudPressure = new CompoundCloudPressure(Compound.Hydrogensulfide,
            worldSettings.DayNightCycleEnabled, 1.0f);
        HydrogenSulfideCloudEnergy = new CompoundCloudEnergy(Compound.Hydrogensulfide, 0.1f);
        SmallSulfurChunkPressure = new ChunkCompoundPressure("sulfurSmallChunk",
            new LocalizedString("SMALL_SULFUR_CHUNK"), Compound.Hydrogensulfide, Compound.Glucose, 1.0f);
        SmallSulfurChunkEnergy = new ChunkCompoundEnergy("sulfurSmallChunk",
            new LocalizedString("SMALL_SULFUR_CHUNK_ENERGY"), Compound.Hydrogensulfide, 0.1f);
        MediumSulfurChunkPressure = new ChunkCompoundPressure("sulfurMediumChunk",
            new LocalizedString("MEDIUM_SULFUR_CHUNK"), Compound.Hydrogensulfide, Compound.Glucose, 1.0f);
        MediumSulfurChunkEnergy = new ChunkCompoundEnergy("sulfurMediumChunk",
            new LocalizedString("MEDIUM_SULFUR_CHUNK_ENERGY"), Compound.Hydrogensulfide, 0.1f);
        LargeSulfurChunkPressure = new ChunkCompoundPressure("sulfurLargeChunk",
            new LocalizedString("LARGE_SULFUR_CHUNK"), Compound.Hydrogensulfide, Compound.Glucose, 1.0f);
        LargeSulfurChunkEnergy = new ChunkCompoundEnergy("sulfurLargeChunk",
            new LocalizedString("LARGE_SULFUR_CHUNK_ENERGY"), Compound.Hydrogensulfide, 0.1f);

        SunlightConversionEfficiencyPressure =
            new CompoundConversionEfficiencyPressure(Compound.Sunlight, Compound.Glucose, true, 1.5f);
        SunlightCompoundPressure = new EnvironmentalCompoundPressure(Compound.Sunlight, Compound.Glucose, 20000, 1.0f);
        SunlightCompoundEnergy = new EnvironmentalCompoundEnergy(Compound.Sunlight, 20000, 0.1f);

        RadiationConversionEfficiencyPressure =
            new CompoundConversionEfficiencyPressure(Compound.Radiation, Compound.ATP, true, 2.0f);
        RadioactiveChunkPressure = new ChunkCompoundPressure("radioactiveChunk",
            new LocalizedString("RADIOACTIVE_CHUNK"), Compound.Radiation, Compound.ATP, 1.0f);
        RadioactiveChunkEnergy = new ChunkCompoundEnergy("radioactiveChunk",
            new LocalizedString("RADIOACTIVE_CHUNK_ENERGY"), Compound.Radiation, 0.1f);

        TemperatureConversionEfficiencyPressure =
            new CompoundConversionEfficiencyPressure(Compound.Temperature, Compound.Glucose, true, 2.0f);
        TemperatureCompoundPressure = new EnvironmentalCompoundPressure(Compound.Temperature, Compound.Glucose,
            100, 1.0f);
        TemperatureCompoundEnergy = new EnvironmentalCompoundEnergy(Compound.Temperature, 100, 0.1f);
        HasTemperature = !worldSettings.LAWK;

        PredatorRoot = new PredatorRoot(1.0f);

        TemperatureSessilityPressure = new TemperatureSessilityPressure(1.0f);
    }
}
