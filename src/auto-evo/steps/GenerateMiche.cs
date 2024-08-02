namespace AutoEvo;

/// <summary>
///   Dynamically generates the Miche for each Patch
/// </summary>
public class GenerateMiche : IRunStep
{
    private static readonly Compound Glucose = SimulationParameters.Instance.GetCompound("glucose");
    private static readonly Compound ATP = SimulationParameters.Instance.GetCompound("atp");

    private static readonly Compound HydrogenSulfide =
        SimulationParameters.Instance.GetCompound("hydrogensulfide");

    private static readonly Compound Iron = SimulationParameters.Instance.GetCompound("iron");
    private static readonly Compound Sunlight = SimulationParameters.Instance.GetCompound("sunlight");
    private static readonly Compound Temperature = SimulationParameters.Instance.GetCompound("temperature");
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
        if (patch.Biome.TryGetCompound(Glucose, CompoundAmountType.Biome, out var glucose) && glucose.Amount > 0)
        {
            var glucoseMiche = new Miche(new CompoundConversionEfficiencyPressure(patch, Glucose, ATP, 1.5f));
            glucoseMiche.AddChild(new Miche(
                new CompoundCloudPressure(patch, 1, Glucose, worldSettings.DayNightCycleEnabled)));

            generatedMiche.AddChild(glucoseMiche);
        }

        // Iron
        if (patch.Biome.Chunks.TryGetValue("ironSmallChunk", out var smallChunk) && smallChunk.Density > 0)
        {
            var ironMiche = new Miche(new CompoundConversionEfficiencyPressure(patch, Iron, ATP, 1.0f));

            ironMiche.AddChild(new Miche(new ChunkCompoundPressure(patch, 1, "ironSmallChunk", Iron)));

            if (patch.Biome.Chunks.TryGetValue("ironBigChunk", out var bigChunk) && bigChunk.Density > 0)
                ironMiche.AddChild(new Miche(new ChunkCompoundPressure(patch, 1, "ironBigChunk", Iron)));

            generatedMiche.AddChild(ironMiche);
        }

        // Hydrogen Sulfide
        if (patch.Biome.TryGetCompound(HydrogenSulfide, CompoundAmountType.Biome, out var hydrogenSulfide) &&
            hydrogenSulfide.Amount > 0)
        {
            var hydrogenSulfideMiche = new Miche(
                new CompoundConversionEfficiencyPressure(patch, HydrogenSulfide, Glucose, 1.0f));
            var generateATP = new Miche(new CompoundConversionEfficiencyPressure(patch, Glucose, ATP, 0.5f));
            var maintainGlucose = new Miche(new MaintainCompound(patch, 1, Glucose));
            var envPressure = new Miche(new CompoundCloudPressure(patch, 1.0f, HydrogenSulfide,
                worldSettings.DayNightCycleEnabled));

            maintainGlucose.AddChild(envPressure);
            generateATP.AddChild(maintainGlucose);
            hydrogenSulfideMiche.AddChild(generateATP);
            generatedMiche.AddChild(hydrogenSulfideMiche);
        }

        // Sunlight
        if (patch.Biome.TryGetCompound(Sunlight, CompoundAmountType.Biome, out var sunlight) &&
            sunlight.Ambient >= 0.25f)
        {
            var sunlightMiche = new Miche(new CompoundConversionEfficiencyPressure(patch, Sunlight, Glucose, 1.0f));
            var generateATP = new Miche(new CompoundConversionEfficiencyPressure(patch, Glucose, ATP, 0.5f));
            var maintainGlucose = new Miche(new MaintainCompound(patch, 1, Glucose));
            var envPressure = new Miche(new EnvironmentalCompoundPressure(patch, 1, Sunlight, Glucose, 10000));

            maintainGlucose.AddChild(envPressure);
            generateATP.AddChild(maintainGlucose);
            sunlightMiche.AddChild(generateATP);
            generatedMiche.AddChild(sunlightMiche);
        }

        // Heat
        if (patch.Biome.TryGetCompound(Temperature, CompoundAmountType.Biome, out var temperature) &&
            temperature.Ambient > 60)
        {
            var tempMiche = new Miche(new CompoundConversionEfficiencyPressure(patch, Temperature, ATP, 1.0f));
            tempMiche.AddChild(new Miche(new EnvironmentalCompoundPressure(patch, 1, Temperature, ATP, 100)));

            generatedMiche.AddChild(tempMiche);
        }

        var predationRoot = new Miche(new PredatorRoot(patch, 5));
        var predationGlucose = new Miche(new CompoundConversionEfficiencyPressure(patch, Glucose, ATP, 1.0f));

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
