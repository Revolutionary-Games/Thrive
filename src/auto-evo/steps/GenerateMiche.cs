namespace AutoEvo;

using System.Collections;
using System.Collections.Generic;
using System.Linq;
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
        var generatedMiche =
            new Miche(new RootPressure(Patch, 1),
                new Miche(new MetabolicStabilityPressure(Patch, 100.0f)));

        // Special Miches

        /*if (PlayerPatch)
        {
            generatedMiche.AddChild(new Miche(new BePlayerSelectionPressure(1.0f)));
        } */

        // Autotrophic Miches

        // Glucose
        if (Patch.GetCompoundAmount(Glucose) > 0)
        {
            generatedMiche.AddChild(
                new Miche(new AutotrophEnergyEfficiencyPressure(Patch, Glucose, ATP, 10.0f),
                    new Miche(new ReachCompoundCloudPressure(2.0f))));
        }

        // Hydrogen Sulfide
        if (Patch.GetCompoundAmount(HydrogenSulfide) > 0)
        {
            generatedMiche.AddChild(
                new Miche(new AutotrophEnergyEfficiencyPressure(Patch, HydrogenSulfide, Glucose, 5.0f),
                    new Miche(new ReachCompoundCloudPressure(2.0f))));
        }

        // Sunlight
        if (Patch.GetCompoundAmount(Sunlight) > 0)
        {
            generatedMiche.AddChild(
                new Miche(new AutotrophEnergyEfficiencyPressure(Patch, Sunlight, Glucose, 5.0f)));
        }

        // Heat
        if (Patch.GetCompoundAmount(Temperature) > 0)
        {
            generatedMiche.AddChild(
                new Miche(new AutotrophEnergyEfficiencyPressure(Patch, Temperature, ATP, 5.0f)));
        }

        // Iron
        if (Patch.GetCompoundAmount(Iron) > 0)
        {
            generatedMiche.AddChild(
                new Miche(new AutotrophEnergyEfficiencyPressure(Patch, Iron, ATP, 5.0f),
                    new Miche(new ReachCompoundCloudPressure(2.0f))));
        }

        // Heterotrophic Miches
        foreach (var possiblePrey in Patch.SpeciesInPatch)
        {
            generatedMiche.AddChild(
                new Miche(
                    new PredationEffectivenessPressure((MicrobeSpecies)possiblePrey.Key, Patch, 10.0f, Cache)));
        }

        return generatedMiche;
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
