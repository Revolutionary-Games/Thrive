namespace AutoEvo;

/// <summary>
///   Dynamically generates the Miche for each Patch
/// </summary>
public class GenerateMiche : IRunStep
{
    private readonly Compound glucose = SimulationParameters.Instance.GetCompound("glucose");
    private readonly Compound atp = SimulationParameters.Instance.GetCompound("atp");

    private readonly Compound hydrogenSulfide =
        SimulationParameters.Instance.GetCompound("hydrogensulfide");

    private readonly Compound iron = SimulationParameters.Instance.GetCompound("iron");
    private readonly Compound sunlight = SimulationParameters.Instance.GetCompound("sunlight");
    private readonly Compound temperature = SimulationParameters.Instance.GetCompound("temperature");
    private readonly Patch patch;
    private readonly SimulationCache cache;

    private readonly WorldGenerationSettings worldSettings;

    public GenerateMiche(Patch patch, SimulationCache cache, WorldGenerationSettings worldSettings)
    {
        this.patch = patch;
        this.cache = cache;
        this.worldSettings = worldSettings;
    }

    public int TotalSteps => 1;

    public bool CanRunConcurrently => false;

    public bool RunStep(RunResults results)
    {
        var generatedMiche = GenerateMicheTree();
        results.AddNewMicheForPatch(patch, PopulateMiche(generatedMiche));

        return true;
    }

    public Miche GenerateMicheTree()
    {
        var rootMiche = new Miche(new RootPressure());
        var generatedMiche = new Miche(new MetabolicStabilityPressure(patch, 10.0f));

        rootMiche.AddChild(generatedMiche);

        // Autotrophic Miches

        // Glucose
        if (patch.Biome.TryGetCompound(glucose, CompoundAmountType.Biome, out var glucoseAmount) &&
            glucoseAmount.Amount > 0)
        {
            var glucoseMiche = new Miche(new CompoundConversionEfficiencyPressure(patch, glucose, atp, 1.5f));
            glucoseMiche.AddChild(new Miche(
                new CompoundCloudPressure(patch, 1, glucose, worldSettings.DayNightCycleEnabled)));

            generatedMiche.AddChild(glucoseMiche);
        }

        var hasSmallIronChunk =
            patch.Biome.Chunks.TryGetValue("ironSmallChunk", out var smallChunk) && smallChunk.Density > 0;

        var hasBigIronChunk = patch.Biome.Chunks.TryGetValue("ironBigChunk", out var bigChunk) && bigChunk.Density > 0;

        // Iron
        if (hasSmallIronChunk || hasBigIronChunk)
        {
            var ironMiche = new Miche(new CompoundConversionEfficiencyPressure(patch, iron, atp, 1.0f));

            if (hasSmallIronChunk)
                ironMiche.AddChild(new Miche(new ChunkCompoundPressure(patch, 1, "ironSmallChunk", iron)));

            if (hasBigIronChunk)
                ironMiche.AddChild(new Miche(new ChunkCompoundPressure(patch, 1, "ironBigChunk", iron)));

            generatedMiche.AddChild(ironMiche);
        }

        // Hydrogen Sulfide
        if (patch.Biome.TryGetCompound(hydrogenSulfide, CompoundAmountType.Biome, out var hydrogenSulfideAmount) &&
            hydrogenSulfideAmount.Amount > 0)
        {
            var hydrogenSulfideMiche = new Miche(
                new CompoundConversionEfficiencyPressure(patch, hydrogenSulfide, glucose, 1.0f));
            var generateATP = new Miche(new CompoundConversionEfficiencyPressure(patch, glucose, atp, 0.5f));
            var maintainGlucose = new Miche(new MaintainCompound(patch, 1, glucose));
            var envPressure = new Miche(new CompoundCloudPressure(patch, 1.0f, hydrogenSulfide,
                worldSettings.DayNightCycleEnabled));

            maintainGlucose.AddChild(envPressure);
            generateATP.AddChild(maintainGlucose);
            hydrogenSulfideMiche.AddChild(generateATP);
            generatedMiche.AddChild(hydrogenSulfideMiche);
        }

        // Sunlight
        if (patch.Biome.TryGetCompound(sunlight, CompoundAmountType.Biome, out var sunlightAmount) &&
            sunlightAmount.Ambient >= 0.25f)
        {
            var sunlightMiche = new Miche(new CompoundConversionEfficiencyPressure(patch, sunlight, glucose, 1.0f));
            var generateATP = new Miche(new CompoundConversionEfficiencyPressure(patch, glucose, atp, 0.5f));
            var maintainGlucose = new Miche(new MaintainCompound(patch, 1, glucose));
            var envPressure = new Miche(new EnvironmentalCompoundPressure(patch, 1, sunlight, glucose, 10000));

            maintainGlucose.AddChild(envPressure);
            generateATP.AddChild(maintainGlucose);
            sunlightMiche.AddChild(generateATP);
            generatedMiche.AddChild(sunlightMiche);
        }

        // Heat
        if (patch.Biome.TryGetCompound(temperature, CompoundAmountType.Biome, out var temperatureAmount) &&
            temperatureAmount.Ambient > 60)
        {
            var tempMiche = new Miche(new CompoundConversionEfficiencyPressure(patch, temperature, atp, 1.0f));
            tempMiche.AddChild(new Miche(new EnvironmentalCompoundPressure(patch, 1, temperature, atp, 100)));

            generatedMiche.AddChild(tempMiche);
        }

        var predationRoot = new Miche(new PredatorRoot(patch, 5));
        var predationGlucose = new Miche(new CompoundConversionEfficiencyPressure(patch, glucose, atp, 1.0f));

        // Heterotrophic Miches
        foreach (var possiblePrey in patch.SpeciesInPatch)
        {
            predationGlucose.AddChild(new Miche(new PredationEffectivenessPressure(possiblePrey.Key, patch, 1.0f)));
        }

        predationRoot.AddChild(predationGlucose);
        generatedMiche.AddChild(predationRoot);

        return rootMiche;
    }

    public Miche PopulateMiche(Miche miche)
    {
        foreach (var species in patch.SpeciesInPatch.Keys)
        {
            miche.InsertSpecies(species, cache);
        }

        return miche;
    }
}
