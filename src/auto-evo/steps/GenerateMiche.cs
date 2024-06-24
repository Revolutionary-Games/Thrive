namespace AutoEvo;

using Godot;

public class GenerateMiche : IRunStep
{
    public Patch Patch;
    public SimulationCache Cache;
    public bool PlayerPatch;

    private static readonly Compound Glucose = SimulationParameters.Instance.GetCompound("glucose");
    private static readonly Compound ATP = SimulationParameters.Instance.GetCompound("atp");

    private static readonly Compound HydrogenSulfide =
        SimulationParameters.Instance.GetCompound("hydrogensulfide");

    private static readonly Compound Iron = SimulationParameters.Instance.GetCompound("iron");
    private static readonly Compound Sunlight = SimulationParameters.Instance.GetCompound("sunlight");
    private static readonly Compound Temperature = SimulationParameters.Instance.GetCompound("temperature");

    public GenerateMiche(Patch patch, SimulationCache cache, bool playerPatch)
    {
        Patch = patch;
        Cache = cache;
        PlayerPatch = playerPatch;
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
            glucoseMiche.AddChild(new Miche(new CompoundCloudPressure(Patch, 1, Glucose)));

            generatedMiche.AddChild(glucoseMiche);
        }

        // Iron
        if (Patch.Biome.TryGetCompound(Iron, CompoundAmountType.Biome, out var iron) && iron.Amount > 0)
        {
            var ironMiche = new Miche(new AutotrophEnergyEfficiencyPressure(Patch, Iron, ATP, 5.0f));
            ironMiche.AddChild(new Miche(new CompoundCloudPressure(Patch, 1, Iron)));

            if (Patch.Biome.Chunks.ContainsKey("ironSmallChunk"))
                ironMiche.AddChild(new Miche(new ChunkCompoundPressure(Patch, 1, "ironSmallChunk", Iron)));

            if (Patch.Biome.Chunks.ContainsKey("ironBigChunk"))
                ironMiche.AddChild(new Miche(new ChunkCompoundPressure(Patch, 1, "ironBigChunk", Iron)));

            GD.Print(Patch.Biome.Chunks.Keys);

            generatedMiche.AddChild(ironMiche);
        }

        // Hydrogen Sulfide
        if (Patch.Biome.TryGetCompound(HydrogenSulfide, CompoundAmountType.Biome, out var hydrogenSulfide) &&
            hydrogenSulfide.Amount > 0)
        {
            var hydrogenSulfideMiche = new Miche(new AutotrophEnergyEfficiencyPressure(Patch, HydrogenSulfide, Glucose, 5.0f));
            var generateATP = new Miche(new AutotrophEnergyEfficiencyPressure(Patch, Glucose, ATP, 1.0f));

            generateATP.AddChild(new Miche(new CompoundCloudPressure(Patch, 2.0f, HydrogenSulfide)));
            hydrogenSulfideMiche.AddChild(generateATP);
            generatedMiche.AddChild(hydrogenSulfideMiche);
        }

        // Sunlight
        if (Patch.Biome.TryGetCompound(Sunlight, CompoundAmountType.Biome, out var sunlight) && sunlight.Ambient > 0)
        {
            generatedMiche.AddChild(new Miche(new AutotrophEnergyEfficiencyPressure(Patch, Sunlight, Glucose, 5.0f)));
        }

        // Heat
        // This check probably should be more than 0
        if (Patch.Biome.TryGetCompound(Temperature, CompoundAmountType.Biome, out var temperature) &&
            temperature.Ambient > 1)
        {
            generatedMiche.AddChild(new Miche(new AutotrophEnergyEfficiencyPressure(Patch, Temperature, ATP, 5.0f)));
        }

        // Heterotrophic Miches
        foreach (var possiblePrey in Patch.SpeciesInPatch)
        {
            generatedMiche.AddChild(
                new Miche(new PredationEffectivenessPressure((MicrobeSpecies)possiblePrey.Key, Patch, 10.0f, Cache)));
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
