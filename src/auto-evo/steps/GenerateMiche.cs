namespace AutoEvo;

using System.Collections.Generic;

/// <summary>
///   Dynamically generates the <see cref="Miche"/> tree for each <see cref="Patch"/>
/// </summary>
public class GenerateMiche : IRunStep
{
    private readonly Compound glucose = SimulationParameters.Instance.GetCompound("glucose");

    private readonly Compound hydrogenSulfide =
        SimulationParameters.Instance.GetCompound("hydrogensulfide");

    private readonly Compound sunlight = SimulationParameters.Instance.GetCompound("sunlight");
    private readonly Compound temperature = SimulationParameters.Instance.GetCompound("temperature");

    private readonly Patch patch;
    private readonly SimulationCache cache;
    private readonly AutoEvoGlobalCache globalCache;

    public GenerateMiche(Patch patch, SimulationCache cache, AutoEvoGlobalCache globalCache)
    {
        this.patch = patch;
        this.cache = cache;
        this.globalCache = globalCache;
    }

    public int TotalSteps => 1;

    public bool CanRunConcurrently => false;

    public bool RunStep(RunResults results)
    {
        var generatedMiche = GenerateMicheTree(globalCache);
        results.AddNewMicheForPatch(patch, PopulateMiche(generatedMiche));

        return true;
    }

    public Miche GenerateMicheTree(AutoEvoGlobalCache globalCache)
    {
        var rootMiche = new Miche(globalCache.RootPressure);
        var generatedMiche = new Miche(globalCache.MetabolicStabilityPressure);

        rootMiche.AddChild(generatedMiche);

        // Autotrophic Miches

        // Glucose
        if (patch.Biome.TryGetCompound(glucose, CompoundAmountType.Biome, out var glucoseAmount) &&
            glucoseAmount.Amount > 0)
        {
            var glucoseMiche = new Miche(globalCache.GlucoseConversionEfficiencyPressure);
            glucoseMiche.AddChild(new Miche(globalCache.GlucoseCloudPressure));

            generatedMiche.AddChild(glucoseMiche);
        }

        var hasSmallIronChunk =
            patch.Biome.Chunks.TryGetValue("ironSmallChunk", out var smallChunk) && smallChunk.Density > 0;

        var hasBigIronChunk = patch.Biome.Chunks.TryGetValue("ironBigChunk", out var bigChunk) && bigChunk.Density > 0;

        // Iron
        if (hasSmallIronChunk || hasBigIronChunk)
        {
            var ironMiche = new Miche(globalCache.IronConversionEfficiencyPressure);

            if (hasSmallIronChunk)
                ironMiche.AddChild(new Miche(globalCache.SmallIronChunkPressure));

            if (hasBigIronChunk)
                ironMiche.AddChild(new Miche(globalCache.BigIronChunkPressure));

            // TODO: maybe allowing direct iron in a patch should also be considered (though not currently used by
            // any biome in the game)?

            generatedMiche.AddChild(ironMiche);
        }

        // Hydrogen Sulfide
        if (patch.Biome.TryGetCompound(hydrogenSulfide, CompoundAmountType.Biome, out var hydrogenSulfideAmount) &&
            hydrogenSulfideAmount.Amount > 0)
        {
            var hydrogenSulfideMiche = new Miche(globalCache.HydrogenSulfideConversionEfficiencyPressure);
            var generateATP = new Miche(globalCache.MinorGlucoseConversionEfficiencyPressure);
            var maintainGlucose = new Miche(globalCache.MaintainGlucose);
            var envPressure = new Miche(globalCache.HydrogenSulfideCloudPressure);

            maintainGlucose.AddChild(envPressure);
            generateATP.AddChild(maintainGlucose);
            hydrogenSulfideMiche.AddChild(generateATP);
            generatedMiche.AddChild(hydrogenSulfideMiche);
        }

        // Sunlight
        // TODO: should there be a dynamic energy level requirement rather than an absolute value?
        if (patch.Biome.TryGetCompound(sunlight, CompoundAmountType.Biome, out var sunlightAmount) &&
            sunlightAmount.Ambient >= 0.25f)
        {
            var sunlightMiche = new Miche(globalCache.SunlightConversionEfficiencyPressure);
            var generateATP = new Miche(globalCache.MinorGlucoseConversionEfficiencyPressure);
            var maintainGlucose = new Miche(globalCache.MaintainGlucose);
            var envPressure = new Miche(globalCache.SunlightCompoundPressure);

            maintainGlucose.AddChild(envPressure);
            generateATP.AddChild(maintainGlucose);
            sunlightMiche.AddChild(generateATP);
            generatedMiche.AddChild(sunlightMiche);
        }

        // Heat
        // TODO: the 60 here should be a constant or explained some other way what the threshold is
        if (patch.Biome.TryGetCompound(temperature, CompoundAmountType.Biome, out var temperatureAmount) &&
            temperatureAmount.Ambient > 60)
        {
            var tempMiche = new Miche(globalCache.TemperatureConversionEfficiencyPressure);
            tempMiche.AddChild(new Miche(globalCache.TemperatureCompoundPressure));

            generatedMiche.AddChild(tempMiche);
        }

        var predationRoot = new Miche(globalCache.PredatorRoot);
        var predationGlucose = new Miche(globalCache.MinorGlucoseConversionEfficiencyPressure);

        // Heterotrophic Miches
        foreach (var possiblePrey in patch.SpeciesInPatch)
        {
            predationGlucose.AddChild(new Miche(new PredationEffectivenessPressure(possiblePrey.Key, 1.0f)));
        }

        if (patch.SpeciesInPatch.Count > 1)
            predationRoot.AddChild(predationGlucose);

        generatedMiche.AddChild(predationRoot);

        return rootMiche;
    }

    public Miche PopulateMiche(Miche miche)
    {
        var scores = new Dictionary<Species, float>();
        var workMemory = new HashSet<Species>();
        miche.SetupScores(scores, workMemory);

        foreach (var species in patch.SpeciesInPatch.Keys)
        {
            miche.InsertSpecies(species, patch, scores, cache, false, workMemory);
        }

        return miche;
    }
}
