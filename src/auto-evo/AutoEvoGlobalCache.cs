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
        var atp = SimulationParameters.Instance.GetCompound("atp");
        var glucose = SimulationParameters.Instance.GetCompound("glucose");
        var iron = SimulationParameters.Instance.GetCompound("iron");
        var hydrogenSulfide = SimulationParameters.Instance.GetCompound("hydrogensulfide");
        var sunlight = SimulationParameters.Instance.GetCompound("sunlight");
        var temperature = SimulationParameters.Instance.GetCompound("temperature");

        RootPressure = new RootPressure();
        MetabolicStabilityPressure = new MetabolicStabilityPressure(10.0f);

        MinorGlucoseConversionEfficiencyPressure = new CompoundConversionEfficiencyPressure(glucose, atp, 0.75f);
        MaintainGlucose = new MaintainCompoundPressure(glucose, 1.0f);

        GlucoseConversionEfficiencyPressure = new CompoundConversionEfficiencyPressure(glucose, atp, 1.5f);
        GlucoseCloudPressure = new CompoundCloudPressure(glucose, worldSettings.DayNightCycleEnabled, 1.0f);

        IronConversionEfficiencyPressure = new CompoundConversionEfficiencyPressure(iron, atp, 1.5f);
        SmallIronChunkPressure = new ChunkCompoundPressure("ironSmallChunk", iron, 1.0f);
        BigIronChunkPressure = new ChunkCompoundPressure("ironBigChunk", iron, 1.0f);

        HydrogenSulfideConversionEfficiencyPressure = new CompoundConversionEfficiencyPressure(hydrogenSulfide,
            glucose, 1.0f);
        HydrogenSulfideCloudPressure = new CompoundCloudPressure(hydrogenSulfide, worldSettings.DayNightCycleEnabled,
            1.0f);

        SunlightConversionEfficiencyPressure = new CompoundConversionEfficiencyPressure(sunlight, glucose, 1.0f);
        SunlightCompoundPressure = new EnvironmentalCompoundPressure(sunlight, glucose, 20000, 1.0f);

        TemperatureConversionEfficiencyPressure = new CompoundConversionEfficiencyPressure(temperature, atp, 1.0f);
        TemperatureCompoundPressure = new EnvironmentalCompoundPressure(temperature, atp, 100, 1.0f);

        PredatorRoot = new PredatorRoot(1.0f);
    }
}
