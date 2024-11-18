namespace AutoEvo;

/// <summary>
///   Caches objects to be used in multiple auto-evo runs
/// </summary>
public class AutoEvoGlobalCache
{
    public readonly RootPressure RootPressure;
    public readonly MetabolicStabilityPressure MetabolicStabilityPressure;

    public readonly CompoundConversionEfficiencyPressure MinorGlucoseConversionEfficiencyPressure;
    public readonly MaintainCompoundPressure MaintainGlucose;

    public readonly CompoundConversionEfficiencyPressure GlucoseConversionEfficiencyPressure;
    public readonly CompoundCloudPressure GlucoseCloudPressure;

    public readonly CompoundConversionEfficiencyPressure IronConversionEfficiencyPressure;
    public readonly ChunkCompoundPressure SmallIronChunkPressure;
    public readonly ChunkCompoundPressure BigIronChunkPressure;

    public readonly CompoundConversionEfficiencyPressure HydrogenSulfideConversionEfficiencyPressure;
    public readonly CompoundCloudPressure HydrogenSulfideCloudPressure;

    public readonly CompoundConversionEfficiencyPressure SunlightConversionEfficiencyPressure;
    public readonly EnvironmentalCompoundPressure SunlightCompoundPressure;

    public readonly CompoundConversionEfficiencyPressure TemperatureConversionEfficiencyPressure;
    public readonly EnvironmentalCompoundPressure TemperatureCompoundPressure;

    public readonly PredatorRoot PredatorRoot;

    public AutoEvoGlobalCache(WorldGenerationSettings worldSettings)
    {
        RootPressure = new RootPressure();
        MetabolicStabilityPressure = new MetabolicStabilityPressure(10.0f);

        MinorGlucoseConversionEfficiencyPressure =
            new CompoundConversionEfficiencyPressure(Compound.Glucose, Compound.ATP, 0.75f);
        MaintainGlucose = new MaintainCompoundPressure(Compound.Glucose, 1.5f);

        GlucoseConversionEfficiencyPressure =
            new CompoundConversionEfficiencyPressure(Compound.Glucose, Compound.ATP, 1.5f);
        GlucoseCloudPressure = new CompoundCloudPressure(Compound.Glucose, worldSettings.DayNightCycleEnabled, 1.0f);

        IronConversionEfficiencyPressure = new CompoundConversionEfficiencyPressure(Compound.Iron, Compound.ATP, 1.5f);
        SmallIronChunkPressure = new ChunkCompoundPressure("ironSmallChunk", new LocalizedString("SMALL_IRON_CHUNK"),
            Compound.Iron, 1.0f);
        BigIronChunkPressure = new ChunkCompoundPressure("ironBigChunk", new LocalizedString("BIG_IRON_CHUNK"),
            Compound.Iron, 1.0f);

        HydrogenSulfideConversionEfficiencyPressure = new CompoundConversionEfficiencyPressure(Compound.Hydrogensulfide,
            Compound.Glucose, 1.0f);
        HydrogenSulfideCloudPressure = new CompoundCloudPressure(Compound.Hydrogensulfide,
            worldSettings.DayNightCycleEnabled, 1.0f);

        SunlightConversionEfficiencyPressure =
            new CompoundConversionEfficiencyPressure(Compound.Sunlight, Compound.Glucose, 1.0f);
        SunlightCompoundPressure = new EnvironmentalCompoundPressure(Compound.Sunlight, Compound.Glucose, 400000, 1.0f);

        TemperatureConversionEfficiencyPressure =
            new CompoundConversionEfficiencyPressure(Compound.Temperature, Compound.ATP, 1.0f);
        TemperatureCompoundPressure = new EnvironmentalCompoundPressure(Compound.Temperature, Compound.ATP, 100, 1.0f);

        PredatorRoot = new PredatorRoot(1.0f);
    }
}
