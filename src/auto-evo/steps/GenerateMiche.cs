namespace AutoEvo;

public class GenerateMiche : IRunStep
{
    public Patch Patch;
    public SimulationCache Cache;

    private static readonly Compound Glucose = SimulationParameters.Instance.GetCompound("glucose");
    private static readonly Compound ATP = SimulationParameters.Instance.GetCompound("atp");

    private static readonly Compound HydrogenSulfide =
        SimulationParameters.Instance.GetCompound("hydrogensulfide");

    private static readonly Compound Iron = SimulationParameters.Instance.GetCompound("iron");
    private static readonly Compound Sunlight = SimulationParameters.Instance.GetCompound("sunlight");
    private static readonly Compound Temperature = SimulationParameters.Instance.GetCompound("temperature");
    private WorldGenerationSettings worldSettings;

    public GenerateMiche(Patch patch, SimulationCache cache, WorldGenerationSettings worldSettings)
    {
        Patch = patch;
        Cache = cache;
        this.worldSettings = worldSettings;
    }

    public int TotalSteps => 1;

    public bool CanRunConcurrently => false;

    public bool RunStep(RunResults results)
    {
        var generatedMiche = GenerateMicheTree();
        results.MicheByPatch[Patch] = PopulateMiche(generatedMiche);

        return true;
    }

    private Miche GenerateMicheTree()
    {
        var rootMiche = new Miche(new RootPressure());
        var generatedMiche = new Miche(new MetabolicStabilityPressure(Patch, 10.0f));

        rootMiche.AddChild(generatedMiche);

        // Autotrophic Miches

        // Glucose
        if (Patch.Biome.TryGetCompound(Glucose, CompoundAmountType.Biome, out var glucose) && glucose.Amount > 0)
        {
            var glucoseMiche = new Miche(new AutotrophEnergyEfficiencyPressure(Patch, Glucose, ATP, 1));
            glucoseMiche.AddChild(new Miche(
                new CompoundCloudPressure(Patch, 1, Glucose, worldSettings.DayNightCycleEnabled)));

            generatedMiche.AddChild(glucoseMiche);
        }

        // Iron
        if (Patch.Biome.Chunks.TryGetValue("ironSmallChunk", out var smallChunk) && smallChunk.Density > 0)
        {
            var ironMiche = new Miche(new AutotrophEnergyEfficiencyPressure(Patch, Iron, ATP, 1.0f));

            ironMiche.AddChild(new Miche(new ChunkCompoundPressure(Patch, 1, "ironSmallChunk", Iron)));

            if (Patch.Biome.Chunks.TryGetValue("ironBigChunk", out var bigChunk) && bigChunk.Density > 0)
                ironMiche.AddChild(new Miche(new ChunkCompoundPressure(Patch, 1, "ironBigChunk", Iron)));

            generatedMiche.AddChild(ironMiche);
        }

        // Hydrogen Sulfide
        if (Patch.Biome.TryGetCompound(HydrogenSulfide, CompoundAmountType.Biome, out var hydrogenSulfide) &&
            hydrogenSulfide.Amount > 0)
        {
            var hydrogenSulfideMiche = new Miche(
                new AutotrophEnergyEfficiencyPressure(Patch, HydrogenSulfide, Glucose, 1.0f));
            var generateATP = new Miche(new AutotrophEnergyEfficiencyPressure(Patch, Glucose, ATP, 0.25f));

            generateATP.AddChild(new Miche(new CompoundCloudPressure(Patch, 1.0f, HydrogenSulfide,
                worldSettings.DayNightCycleEnabled)));

            hydrogenSulfideMiche.AddChild(generateATP);
            generatedMiche.AddChild(hydrogenSulfideMiche);
        }

        // Sunlight
        if (Patch.Biome.TryGetCompound(Sunlight, CompoundAmountType.Biome, out var sunlight) && sunlight.Ambient > 0)
        {
            var sunlightMiche = new Miche(new AutotrophEnergyEfficiencyPressure(Patch, Sunlight, Glucose, 1.0f));
            sunlightMiche.AddChild(new Miche(new EnvironmentalCompoundPressure(Patch, 1, Sunlight)));

            generatedMiche.AddChild(sunlightMiche);
        }

        // Heat
        // This check probably should be more than 0
        if (Patch.Biome.TryGetCompound(Temperature, CompoundAmountType.Biome, out var temperature) &&
            temperature.Ambient > 1)
        {
            var tempMiche = new Miche(new AutotrophEnergyEfficiencyPressure(Patch, Temperature, ATP, 1.0f));
            tempMiche.AddChild(new Miche(new EnvironmentalCompoundPressure(Patch, 1, Temperature)));

            generatedMiche.AddChild(tempMiche);
        }

        // Heterotrophic Miches
        foreach (var possiblePrey in Patch.SpeciesInPatch)
        {
            generatedMiche.AddChild(
                new Miche(new PredationEffectivenessPressure((MicrobeSpecies)possiblePrey.Key, Patch, 1.0f, Cache)));
        }

        return rootMiche;
    }

    private Miche PopulateMiche(Miche miche)
    {
        foreach (var species in Patch.SpeciesInPatch.Keys)
        {
            miche.InsertSpecies((MicrobeSpecies)species, Cache);
        }

        return miche;
    }
}
