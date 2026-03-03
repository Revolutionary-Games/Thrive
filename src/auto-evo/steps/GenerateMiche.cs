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

        var lastGeneralMiche = generatedMiche;

        // Glucose
        if (patch.Biome.TryGetCompound(Compound.Glucose, CompoundAmountType.Biome, out var glucoseAmount) &&
            glucoseAmount.Amount > 0)
        {
            var glucoseMiche = new Miche(globalCache.GlucoseConversionEfficiencyPressure);
            var glucoseCloudMiche = new Miche(globalCache.GlucoseCloudPressure);
            var avoidPredationMiche = new Miche(globalCache.GeneralAvoidPredationSelectionPressure);
            var phosphateMiche = new Miche(globalCache.PhosphatePressure);
            var ammoniaMiche = new Miche(globalCache.AmmoniaPressure);
            var energyConsumptionMiche = new Miche(globalCache.EnergyConsumptionPressure);

            lastGeneralMiche.AddChild(glucoseMiche);
            glucoseMiche.AddChild(glucoseCloudMiche);
            glucoseCloudMiche.AddChild(avoidPredationMiche);
            avoidPredationMiche.AddChild(phosphateMiche);
            phosphateMiche.AddChild(ammoniaMiche);
            ammoniaMiche.AddChild(energyConsumptionMiche);
            energyConsumptionMiche.AddChild(new Miche(globalCache.GlucoseCloudEnergy));
        }

        // Iron
        var hasSmallIronChunk =
            patch.Biome.Chunks.TryGetValue("ironSmallChunk", out var smallChunk) && smallChunk.Density > 0;

        var hasBigIronChunk = patch.Biome.Chunks.TryGetValue("ironBigChunk", out var bigChunk) && bigChunk.Density > 0;

        if (hasSmallIronChunk || hasBigIronChunk)
        {
            var ironMiche = new Miche(globalCache.IronConversionEfficiencyPressure);

            lastGeneralMiche.AddChild(ironMiche);

            if (hasSmallIronChunk)
            {
                var smallIronChunkMiche = new Miche(globalCache.SmallIronChunkPressure);
                var avoidPredationMiche = new Miche(globalCache.GeneralAvoidPredationSelectionPressure);
                var phosphateMiche = new Miche(globalCache.PhosphatePressure);
                var ammoniaMiche = new Miche(globalCache.AmmoniaPressure);
                var energyConsumptionMiche = new Miche(globalCache.EnergyConsumptionPressure);

                ironMiche.AddChild(smallIronChunkMiche);
                smallIronChunkMiche.AddChild(avoidPredationMiche);
                avoidPredationMiche.AddChild(phosphateMiche);
                phosphateMiche.AddChild(ammoniaMiche);
                ammoniaMiche.AddChild(energyConsumptionMiche);
                energyConsumptionMiche.AddChild(new Miche(globalCache.SmallIronChunkEnergy));
            }

            if (hasBigIronChunk)
            {
                var bigIronChunkMiche = new Miche(globalCache.BigIronChunkPressure);
                var avoidPredationMiche = new Miche(globalCache.GeneralAvoidPredationSelectionPressure);
                var phosphateMiche = new Miche(globalCache.PhosphatePressure);
                var ammoniaMiche = new Miche(globalCache.AmmoniaPressure);
                var energyConsumptionMiche = new Miche(globalCache.EnergyConsumptionPressure);

                ironMiche.AddChild(bigIronChunkMiche);
                bigIronChunkMiche.AddChild(avoidPredationMiche);
                avoidPredationMiche.AddChild(phosphateMiche);
                phosphateMiche.AddChild(ammoniaMiche);
                ammoniaMiche.AddChild(energyConsumptionMiche);
                energyConsumptionMiche.AddChild(new Miche(globalCache.BigIronChunkEnergy));
            }

            // TODO: maybe allowing direct iron in a patch should also be considered (though not currently used by
            // any biome in the game)?
        }

        // Hydrogen Sulfide
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

            lastGeneralMiche.AddChild(hydrogenSulfideMiche);

            if (hydrogenSulfideAmount.Amount > 0)
            {
                var hydrogenSulfideCloudPressure = new Miche(globalCache.HydrogenSulfideCloudPressure);
                var avoidPredationMiche = new Miche(globalCache.GeneralAvoidPredationSelectionPressure);
                var phosphateMiche = new Miche(globalCache.PhosphatePressure);
                var ammoniaMiche = new Miche(globalCache.AmmoniaPressure);
                var generateATP = new Miche(globalCache.MinorGlucoseConversionEfficiencyPressure);
                var maintainGlucose = new Miche(globalCache.MaintainGlucose);
                var energyConsumptionMiche = new Miche(globalCache.EnergyConsumptionPressure);

                hydrogenSulfideMiche.AddChild(hydrogenSulfideCloudPressure);
                hydrogenSulfideCloudPressure.AddChild(avoidPredationMiche);
                avoidPredationMiche.AddChild(phosphateMiche);
                phosphateMiche.AddChild(ammoniaMiche);
                ammoniaMiche.AddChild(generateATP);
                generateATP.AddChild(maintainGlucose);
                maintainGlucose.AddChild(energyConsumptionMiche);
                energyConsumptionMiche.AddChild(new Miche(globalCache.HydrogenSulfideCloudEnergy));
            }

            if (hasSmallSulfurChunk)
            {
                var smallSulfurChunkPressure = new Miche(globalCache.SmallSulfurChunkPressure);
                var avoidPredationMiche = new Miche(globalCache.GeneralAvoidPredationSelectionPressure);
                var phosphateMiche = new Miche(globalCache.PhosphatePressure);
                var ammoniaMiche = new Miche(globalCache.AmmoniaPressure);
                var generateATP = new Miche(globalCache.MinorGlucoseConversionEfficiencyPressure);
                var maintainGlucose = new Miche(globalCache.MaintainGlucose);
                var energyConsumptionMiche = new Miche(globalCache.EnergyConsumptionPressure);

                hydrogenSulfideMiche.AddChild(smallSulfurChunkPressure);
                smallSulfurChunkPressure.AddChild(avoidPredationMiche);
                avoidPredationMiche.AddChild(phosphateMiche);
                phosphateMiche.AddChild(ammoniaMiche);
                ammoniaMiche.AddChild(generateATP);
                generateATP.AddChild(maintainGlucose);
                maintainGlucose.AddChild(energyConsumptionMiche);
                energyConsumptionMiche.AddChild(new Miche(globalCache.SmallSulfurChunkEnergy));
            }

            if (hasMediumSulfurChunk)
            {
                var mediumSulfurChunkPressure = new Miche(globalCache.MediumSulfurChunkPressure);
                var avoidPredationMiche = new Miche(globalCache.GeneralAvoidPredationSelectionPressure);
                var phosphateMiche = new Miche(globalCache.PhosphatePressure);
                var ammoniaMiche = new Miche(globalCache.AmmoniaPressure);
                var generateATP = new Miche(globalCache.MinorGlucoseConversionEfficiencyPressure);
                var maintainGlucose = new Miche(globalCache.MaintainGlucose);
                var energyConsumptionMiche = new Miche(globalCache.EnergyConsumptionPressure);

                hydrogenSulfideMiche.AddChild(mediumSulfurChunkPressure);
                mediumSulfurChunkPressure.AddChild(avoidPredationMiche);
                avoidPredationMiche.AddChild(phosphateMiche);
                phosphateMiche.AddChild(ammoniaMiche);
                ammoniaMiche.AddChild(generateATP);
                generateATP.AddChild(maintainGlucose);
                maintainGlucose.AddChild(energyConsumptionMiche);
                energyConsumptionMiche.AddChild(new Miche(globalCache.MediumSulfurChunkEnergy));
            }

            if (hasLargeSulfurChunk)
            {
                var largeSulfurChunkPressure = new Miche(globalCache.LargeSulfurChunkPressure);
                var avoidPredationMiche = new Miche(globalCache.GeneralAvoidPredationSelectionPressure);
                var phosphateMiche = new Miche(globalCache.PhosphatePressure);
                var ammoniaMiche = new Miche(globalCache.AmmoniaPressure);
                var generateATP = new Miche(globalCache.MinorGlucoseConversionEfficiencyPressure);
                var maintainGlucose = new Miche(globalCache.MaintainGlucose);
                var energyConsumptionMiche = new Miche(globalCache.EnergyConsumptionPressure);

                hydrogenSulfideMiche.AddChild(largeSulfurChunkPressure);
                largeSulfurChunkPressure.AddChild(avoidPredationMiche);
                avoidPredationMiche.AddChild(phosphateMiche);
                phosphateMiche.AddChild(ammoniaMiche);
                ammoniaMiche.AddChild(generateATP);
                generateATP.AddChild(maintainGlucose);
                maintainGlucose.AddChild(energyConsumptionMiche);
                energyConsumptionMiche.AddChild(new Miche(globalCache.LargeSulfurChunkEnergy));
            }
        }

        // Radioactive Chunk
        var hasRadioactiveChunk =
            patch.Biome.Chunks.TryGetValue("radioactiveChunk", out var radioactiveChunk) &&
            radioactiveChunk.Density > 0;

        if (hasRadioactiveChunk)
        {
            var radiationMiche = new Miche(globalCache.RadiationConversionEfficiencyPressure);
            var radoactiveChunkMiche = new Miche(globalCache.RadioactiveChunkPressure);
            var avoidPredationMiche = new Miche(globalCache.GeneralAvoidPredationSelectionPressure);
            var phosphateMiche = new Miche(globalCache.PhosphatePressure);
            var ammoniaMiche = new Miche(globalCache.AmmoniaPressure);
            var energyConsumptionMiche = new Miche(globalCache.EnergyConsumptionPressure);

            lastGeneralMiche.AddChild(radiationMiche);
            radiationMiche.AddChild(radoactiveChunkMiche);
            radoactiveChunkMiche.AddChild(avoidPredationMiche);
            avoidPredationMiche.AddChild(phosphateMiche);
            phosphateMiche.AddChild(ammoniaMiche);
            ammoniaMiche.AddChild(energyConsumptionMiche);
            energyConsumptionMiche.AddChild(new Miche(globalCache.RadioactiveChunkEnergy));
        }

        // Sunlight
        // TODO: should there be a dynamic energy level requirement rather than an absolute value?
        if (patch.Biome.TryGetCompound(Compound.Sunlight, CompoundAmountType.Biome, out var sunlightAmount) &&
            sunlightAmount.Ambient >= 0.25f)
        {
            var sunlightMiche = new Miche(globalCache.SunlightConversionEfficiencyPressure);
            var envPressure = new Miche(globalCache.SunlightCompoundPressure);
            var avoidPredationMiche = new Miche(globalCache.GeneralAvoidPredationSelectionPressure);
            var phosphateMiche = new Miche(globalCache.PhosphatePressure);
            var ammoniaMiche = new Miche(globalCache.AmmoniaPressure);
            var generateATP = new Miche(globalCache.MinorGlucoseConversionEfficiencyPressure);
            var maintainGlucose = new Miche(globalCache.MaintainGlucose);
            var energyConsumptionMiche = new Miche(globalCache.EnergyConsumptionPressure);

            lastGeneralMiche.AddChild(sunlightMiche);
            sunlightMiche.AddChild(envPressure);
            envPressure.AddChild(avoidPredationMiche);
            avoidPredationMiche.AddChild(phosphateMiche);
            phosphateMiche.AddChild(ammoniaMiche);
            ammoniaMiche.AddChild(generateATP);
            generateATP.AddChild(maintainGlucose);
            maintainGlucose.AddChild(energyConsumptionMiche);
            energyConsumptionMiche.AddChild(new Miche(globalCache.SunlightCompoundEnergy));
        }

        // Heat
        // TODO: the 60 here should be a constant or explained some other way what the threshold is
        // As only non-LAWK organelles can use heat, we don't add the temperature miches when LAWK is on
        if (patch.Biome.TryGetCompound(Compound.Temperature, CompoundAmountType.Biome,
                out BiomeCompoundProperties temperatureAmount) &&
            temperatureAmount.Ambient > 60 && globalCache.HasTemperature)
        {
            var tempMiche = new Miche(globalCache.TemperatureConversionEfficiencyPressure);
            var tempCompPressure = new Miche(globalCache.TemperatureCompoundPressure);
            var avoidPredationMiche = new Miche(globalCache.GeneralAvoidPredationSelectionPressure);
            var phosphateMiche = new Miche(globalCache.PhosphatePressure);
            var ammoniaMiche = new Miche(globalCache.AmmoniaPressure);
            var generateATP = new Miche(globalCache.MinorGlucoseConversionEfficiencyPressure);
            var maintainGlucose = new Miche(globalCache.MaintainGlucose);
            var tempSessilityMiche = new Miche(globalCache.TemperatureSessilityPressure);
            var energyConsumptionMiche = new Miche(globalCache.EnergyConsumptionPressure);

            lastGeneralMiche.AddChild(tempMiche);
            tempMiche.AddChild(tempCompPressure);
            tempCompPressure.AddChild(avoidPredationMiche);
            avoidPredationMiche.AddChild(phosphateMiche);
            phosphateMiche.AddChild(ammoniaMiche);
            ammoniaMiche.AddChild(generateATP);
            generateATP.AddChild(maintainGlucose);
            maintainGlucose.AddChild(tempSessilityMiche);
            tempSessilityMiche.AddChild(energyConsumptionMiche);
            energyConsumptionMiche.AddChild(new Miche(globalCache.TemperatureCompoundEnergy));
        }

        var predationRoot = new Miche(globalCache.PredatorRoot);

        // Per Target-Species Miches
        foreach (var targetSpecies in patch.SpeciesInPatch)
        {
            // Predation Miches
            var predationEffectiveness = new Miche(new PredationEffectivenessPressure(targetSpecies.Key, 7.0f));
            var avoidPredationMiche = new Miche(globalCache.GeneralAvoidPredationSelectionPressure);
            var predationGlucose = new Miche(globalCache.MinorGlucoseConversionEfficiencyPressure);
            var energyConsumptionMiche = new Miche(globalCache.EnergyConsumptionPressure);

            predationRoot.AddChild(predationEffectiveness);
            predationEffectiveness.AddChild(avoidPredationMiche);
            avoidPredationMiche.AddChild(predationGlucose);
            predationGlucose.AddChild(energyConsumptionMiche);
            energyConsumptionMiche.AddChild(new Miche(new PredationEnergy(targetSpecies.Key, 0.1f)));

            // Endosymbiosis Miches
            if (targetSpecies.Key.PlayerSpecies && targetSpecies.Key.Endosymbiosis.StartedEndosymbiosis != null)
            {
                var endosymbiont = targetSpecies.Key.Endosymbiosis.StartedEndosymbiosis.Species;
                var endosymbiosisPressure = new Miche(new EndosymbiosisPressure(endosymbiont, targetSpecies.Key, 1.0f));
                generatedMiche.AddChild(endosymbiosisPressure);
            }
        }

        if (patch.SpeciesInPatch.Count > 1)
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
