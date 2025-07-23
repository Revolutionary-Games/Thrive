namespace AutoEvo;

/// <summary>
///   Dynamically generates the <see cref="Miche"/> tree for each <see cref="Patch"/>
/// </summary>
public class GenerateMiche : IRunStep
{
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
        var metabolicRoot = new Miche(globalCache.MetabolicStabilityPressure);
        var generatedMiche = new Miche(globalCache.EnvironmentalTolerancesPressure);

        rootMiche.AddChild(metabolicRoot);
        metabolicRoot.AddChild(generatedMiche);

        // Autotrophic Miches

        // Glucose
        if (patch.Biome.TryGetCompound(Compound.Glucose, CompoundAmountType.Biome, out var glucoseAmount) &&
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

        var hasSmallSulfurChunk =
            patch.Biome.Chunks.TryGetValue("sulfurSmallChunk", out var smallSulfurChunk) &&
            smallSulfurChunk.Density > 0;

        var hasMediumSulfurChunk = patch.Biome.Chunks.TryGetValue("sulfurMediumChunk", out var mediumSulfurChunk) &&
            mediumSulfurChunk.Density > 0;

        var hasLargeSulfurChunk = patch.Biome.Chunks.TryGetValue("sulfurLargeChunk", out var largeSulfurChunk) &&
            largeSulfurChunk.Density > 0;

        // Hydrogen Sulfide
        if ((patch.Biome.TryGetCompound(Compound.Hydrogensulfide, CompoundAmountType.Biome,
                    out var hydrogenSulfideAmount) &&
                hydrogenSulfideAmount.Amount > 0) || hasSmallSulfurChunk || hasMediumSulfurChunk || hasLargeSulfurChunk)
        {
            var hydrogenSulfideMiche = new Miche(globalCache.HydrogenSulfideConversionEfficiencyPressure);
            var generateATP = new Miche(globalCache.MinorGlucoseConversionEfficiencyPressure);
            var maintainGlucose = new Miche(globalCache.MaintainGlucose);

            if (hydrogenSulfideAmount.Amount > 0)
                maintainGlucose.AddChild(new Miche(globalCache.HydrogenSulfideCloudPressure));

            if (hasSmallSulfurChunk)
                maintainGlucose.AddChild(new Miche(globalCache.SmallSulfurChunkPressure));

            if (hasMediumSulfurChunk)
                maintainGlucose.AddChild(new Miche(globalCache.MediumSulfurChunkPressure));

            if (hasLargeSulfurChunk)
                maintainGlucose.AddChild(new Miche(globalCache.LargeSulfurChunkPressure));

            generateATP.AddChild(maintainGlucose);
            hydrogenSulfideMiche.AddChild(generateATP);
            generatedMiche.AddChild(hydrogenSulfideMiche);
        }

        var hasRadioactiveChunk =
            patch.Biome.Chunks.TryGetValue("radioactiveChunk", out var radioactiveChunk) &&
            radioactiveChunk.Density > 0;

        // Radioactive Chunk
        if (hasRadioactiveChunk)
        {
            var radiationMiche = new Miche(globalCache.RadiationConversionEfficiencyPressure);
            radiationMiche.AddChild(new Miche(globalCache.RadioactiveChunkPressure));

            generatedMiche.AddChild(radiationMiche);
        }

        // Sunlight
        // TODO: should there be a dynamic energy level requirement rather than an absolute value?
        if (patch.Biome.TryGetCompound(Compound.Sunlight, CompoundAmountType.Biome, out var sunlightAmount) &&
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
        // As only non-LAWK organelles can use heat, we don't add the temperature miches when LAWK is on
        if (patch.Biome.TryGetCompound(Compound.Temperature, CompoundAmountType.Biome,
                out BiomeCompoundProperties temperatureAmount) &&
            temperatureAmount.Ambient > 60 && globalCache.HasTemperature)
        {
            var tempMiche = new Miche(globalCache.TemperatureConversionEfficiencyPressure);
            var tempSessilityMiche = new Miche(globalCache.TemperatureSessilityPressure);
            var tempCompPressure = new Miche(globalCache.TemperatureCompoundPressure);

            tempSessilityMiche.AddChild(tempCompPressure);
            tempMiche.AddChild(tempSessilityMiche);
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
        // If no species, don't need to do anything
        if (patch.SpeciesInPatch.Count < 1)
            return miche;

        var insertWorkMemory = new Miche.InsertWorkingMemory();

        foreach (var species in patch.SpeciesInPatch.Keys)
        {
            miche.InsertSpecies(species, patch, null, cache, false, insertWorkMemory);
        }

        return miche;
    }
}
