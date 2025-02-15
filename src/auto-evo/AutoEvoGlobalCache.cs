namespace AutoEvo;

/// <summary>
///   Caches objects to be used in multiple auto-evo runs
/// </summary>
public class AutoEvoGlobalCache
{
    public readonly RootPressure RootPressure;
    public readonly MetabolicStabilityPressure MetabolicStabilityPressure;
    public readonly EnvironmentalTolerancePressure EnvironmentalTolerancesPressure;

    public readonly CompoundConversionEfficiencyPressure MinorGlucoseConversionEfficiencyPressure;
    public readonly MaintainCompoundPressure MaintainGlucose;

    public readonly CompoundConversionEfficiencyPressure GlucoseConversionEfficiencyPressure;
    public readonly CompoundCloudPressure GlucoseCloudPressure;

    public readonly CompoundConversionEfficiencyPressure IronConversionEfficiencyPressure;
    public readonly ChunkCompoundPressure SmallIronChunkPressure;
    public readonly ChunkCompoundPressure BigIronChunkPressure;

    public readonly CompoundConversionEfficiencyPressure HydrogenSulfideConversionEfficiencyPressure;
    public readonly CompoundCloudPressure HydrogenSulfideCloudPressure;
    public readonly ChunkCompoundPressure SmallSulfurChunkPressure;
    public readonly ChunkCompoundPressure MediumSulfurChunkPressure;

    public readonly CompoundConversionEfficiencyPressure SunlightConversionEfficiencyPressure;
    public readonly EnvironmentalCompoundPressure SunlightCompoundPressure;

    public readonly CompoundConversionEfficiencyPressure TemperatureConversionEfficiencyPressure;
    public readonly EnvironmentalCompoundPressure TemperatureCompoundPressure;

    public readonly CompoundConversionEfficiencyPressure RadiationConversionEfficiencyPressure;
    public readonly ChunkCompoundPressure RadioactiveChunkPressure;

    public readonly PredatorRoot PredatorRoot;

    public readonly bool HasTemperature;

    public AutoEvoGlobalCache(WorldGenerationSettings worldSettings)
    {
        RootPressure = new RootPressure();
        MetabolicStabilityPressure = new MetabolicStabilityPressure(10.0f);
        EnvironmentalTolerancesPressure = new EnvironmentalTolerancePressure(4);

        MinorGlucoseConversionEfficiencyPressure =
            new CompoundConversionEfficiencyPressure(Compound.Glucose, Compound.ATP, 0.75f);
        MaintainGlucose = new MaintainCompoundPressure(Compound.Glucose, 1.5f);

        GlucoseConversionEfficiencyPressure =
            new CompoundConversionEfficiencyPressure(Compound.Glucose, Compound.ATP, 1.5f);
        GlucoseCloudPressure = new CompoundCloudPressure(Compound.Glucose, worldSettings.DayNightCycleEnabled, 1.0f);

        IronConversionEfficiencyPressure = new CompoundConversionEfficiencyPressure(Compound.Iron, Compound.ATP, 1.5f);
        SmallIronChunkPressure = new ChunkCompoundPressure("ironSmallChunk", new LocalizedString("SMALL_IRON_CHUNK"),
            Compound.Iron, Compound.ATP, 1.0f);
        BigIronChunkPressure = new ChunkCompoundPressure("ironBigChunk", new LocalizedString("BIG_IRON_CHUNK"),
            Compound.Iron, Compound.ATP, 1.0f);

        HydrogenSulfideConversionEfficiencyPressure = new CompoundConversionEfficiencyPressure(Compound.Hydrogensulfide,
            Compound.Glucose, 1.0f);
        HydrogenSulfideCloudPressure = new CompoundCloudPressure(Compound.Hydrogensulfide,
            worldSettings.DayNightCycleEnabled, 1.0f);
        SmallSulfurChunkPressure = new ChunkCompoundPressure("sulfurSmallChunk",
            new LocalizedString("SMALL_SULFUR_CHUNK"), Compound.Hydrogensulfide, Compound.Glucose, 1.0f);
        MediumSulfurChunkPressure = new ChunkCompoundPressure("sulfurMediumChunk",
            new LocalizedString("MEDIUM_SULFUR_CHUNK"), Compound.Hydrogensulfide, Compound.Glucose, 1.0f);

        SunlightConversionEfficiencyPressure =
            new CompoundConversionEfficiencyPressure(Compound.Sunlight, Compound.Glucose, 1.0f);
        SunlightCompoundPressure = new EnvironmentalCompoundPressure(Compound.Sunlight, Compound.Glucose, 20000, 1.0f);

        RadiationConversionEfficiencyPressure =
            new CompoundConversionEfficiencyPressure(Compound.Radiation, Compound.ATP, 1.0f);
        RadioactiveChunkPressure = new ChunkCompoundPressure("radioactiveChunk",
            new LocalizedString("RADIOACTIVE_CHUNK"), Compound.Radiation, Compound.ATP, 1.0f);

        TemperatureConversionEfficiencyPressure =
            new CompoundConversionEfficiencyPressure(Compound.Temperature, Compound.ATP, 1.0f);
        TemperatureCompoundPressure = new EnvironmentalCompoundPressure(Compound.Temperature, Compound.ATP, 100, 1.0f);
        HasTemperature = !worldSettings.LAWK;

        PredatorRoot = new PredatorRoot(1.0f);
    }
}
