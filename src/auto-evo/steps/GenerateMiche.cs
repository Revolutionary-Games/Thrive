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
        var metabolicStabilityMiche = new Miche(globalCache.MetabolicStabilityPressure);
        var avoidPredationMiche = new Miche(globalCache.GeneralAvoidPredationSelectionPressure);
        var energyConsumptionMiche = new Miche(globalCache.EnergyConsumptionPressure);
        var environmentalToleranceMiche = new Miche(globalCache.EnvironmentalTolerancesPressure);

        rootMiche.AddChild(metabolicStabilityMiche);
        metabolicStabilityMiche.AddChild(avoidPredationMiche);
        avoidPredationMiche.AddChild(energyConsumptionMiche);
        energyConsumptionMiche.AddChild(environmentalToleranceMiche);

        // "Autotrophic" Miches
        var phosphateMiche = new Miche(globalCache.PhosphatePressure);
        var ammoniaMiche = new Miche(globalCache.AmmoniaPressure);

        environmentalToleranceMiche.AddChild(phosphateMiche);
        phosphateMiche.AddChild(ammoniaMiche);

        AddGlucoseBranch(ammoniaMiche, globalCache);
        AddIronBranch(ammoniaMiche, globalCache);
        AddHydrogenSulfideBranch(ammoniaMiche, globalCache);
        AddRadiationBranch(ammoniaMiche, globalCache);
        AddSunlightBranch(ammoniaMiche, globalCache);
        AddTemperatureBranch(ammoniaMiche, globalCache);

        AddSpeciesBranches(environmentalToleranceMiche, globalCache);

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
            if (species is not MicrobeSpecies and not MulticellularSpecies)
                continue;

            miche.InsertSpecies(species, patch, null, cache, false, insertWorkMemory);
        }

        return miche;
    }

    private void AddGlucoseBranch(Miche parentMiche, AutoEvoGlobalCache globalCache)
    {
        if (patch.Biome.TryGetCompound(Compound.Glucose, CompoundAmountType.Biome, out var glucoseAmount) &&
            glucoseAmount.Amount > 0)
        {
            var glucoseMiche = new Miche(globalCache.GlucoseConversionEfficiencyPressure);
            glucoseMiche.AddChild(new Miche(globalCache.GlucoseCloudPressure));

            parentMiche.AddChild(glucoseMiche);
        }
    }

    private void AddIronBranch(Miche parentMiche, AutoEvoGlobalCache globalCache)
    {
        var hasSmallIronChunk =
            patch.Biome.Chunks.TryGetValue("ironSmallChunk", out var smallChunk) && smallChunk.Density > 0;

        var hasBigIronChunk = patch.Biome.Chunks.TryGetValue("ironBigChunk", out var bigChunk) && bigChunk.Density > 0;

        if (hasSmallIronChunk || hasBigIronChunk)
        {
            var ironMiche = new Miche(globalCache.IronConversionEfficiencyPressure);

            if (hasSmallIronChunk)
                ironMiche.AddChild(new Miche(globalCache.SmallIronChunkPressure));

            if (hasBigIronChunk)
                ironMiche.AddChild(new Miche(globalCache.BigIronChunkPressure));

            // TODO: maybe allowing direct iron in a patch should also be considered (though not currently used by
            // any biome in the game)?

            parentMiche.AddChild(ironMiche);
        }
    }

    private void AddHydrogenSulfideBranch(Miche parentMiche, AutoEvoGlobalCache globalCache)
    {
        var hasSmallSulfurChunk =
            patch.Biome.Chunks.TryGetValue("sulfurSmallChunk", out var smallSulfurChunk) &&
            smallSulfurChunk.Density > 0;

        var hasMediumSulfurChunk = patch.Biome.Chunks.TryGetValue("sulfurMediumChunk", out var mediumSulfurChunk) &&
            mediumSulfurChunk.Density > 0;

        var hasLargeSulfurChunk = patch.Biome.Chunks.TryGetValue("sulfurLargeChunk", out var largeSulfurChunk) &&
            largeSulfurChunk.Density > 0;

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
            parentMiche.AddChild(hydrogenSulfideMiche);
        }
    }

    private void AddRadiationBranch(Miche parentMiche, AutoEvoGlobalCache globalCache)
    {
        var hasRadioactiveChunk =
            patch.Biome.Chunks.TryGetValue("radioactiveChunk", out var radioactiveChunk) &&
            radioactiveChunk.Density > 0;

        if (hasRadioactiveChunk)
        {
            var radiationMiche = new Miche(globalCache.RadiationConversionEfficiencyPressure);
            radiationMiche.AddChild(new Miche(globalCache.RadioactiveChunkPressure));

            parentMiche.AddChild(radiationMiche);
        }
    }

    private void AddSunlightBranch(Miche parentMiche, AutoEvoGlobalCache globalCache)
    {
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
            parentMiche.AddChild(sunlightMiche);
        }
    }

    private void AddTemperatureBranch(Miche parentMiche, AutoEvoGlobalCache globalCache)
    {
        // TODO: the 60 here should be a constant or explained some other way what the threshold is
        // As only non-LAWK organelles can use heat, we don't add the temperature miches when LAWK is on
        if (patch.Biome.TryGetCompound(Compound.Temperature, CompoundAmountType.Biome, out var temperatureAmount) &&
            temperatureAmount.Ambient > 60 && globalCache.HasTemperature)
        {
            var tempMiche = new Miche(globalCache.TemperatureConversionEfficiencyPressure);
            var tempSessilityMiche = new Miche(globalCache.TemperatureSessilityPressure);
            var generateATP = new Miche(globalCache.MinorGlucoseConversionEfficiencyPressure);
            var maintainGlucose = new Miche(globalCache.MaintainGlucose);
            var tempCompPressure = new Miche(globalCache.TemperatureCompoundPressure);

            tempSessilityMiche.AddChild(tempCompPressure);
            maintainGlucose.AddChild(tempSessilityMiche);
            generateATP.AddChild(maintainGlucose);
            tempMiche.AddChild(generateATP);
            parentMiche.AddChild(tempMiche);
        }
    }

    private void AddSpeciesBranches(Miche parentMiche, AutoEvoGlobalCache globalCache)
    {
        var predationRoot = new Miche(globalCache.PredatorRoot);
        var predationGlucose = new Miche(globalCache.MinorGlucoseConversionEfficiencyPressure);
        var supportedSpeciesCount = 0;

        // Per Target-Species Miches
        foreach (var targetSpecies in patch.SpeciesInPatch)
        {
            if (targetSpecies.Key is not MicrobeSpecies and not MulticellularSpecies)
                continue;

            ++supportedSpeciesCount;

            // Predation Miches
            predationGlucose.AddChild(new Miche(new PredationEffectivenessPressure(targetSpecies.Key, 8.0f)));

            // Endosymbiosis Miches
            if (targetSpecies.Key.PlayerSpecies && targetSpecies.Key.Endosymbiosis.StartedEndosymbiosis != null)
            {
                var endosymbiont = targetSpecies.Key.Endosymbiosis.StartedEndosymbiosis.Species;
                var endosymbiosisPressure = new Miche(new EndosymbiosisPressure(endosymbiont, targetSpecies.Key, 1.0f));
                parentMiche.AddChild(endosymbiosisPressure);
            }
        }

        if (supportedSpeciesCount > 1)
            predationRoot.AddChild(predationGlucose);

        parentMiche.AddChild(predationRoot);
    }
}
