using System.Collections.Generic;
using AutoEvo;

/// <summary>
///   Custom auto-evo run for use in editor simulations to predict population numbers given specified changes
/// </summary>
public class EditorAutoEvoRun : AutoEvoRun
{
    public EditorAutoEvoRun(GameWorld world, AutoEvoGlobalCache globalCache, Species originalEditedSpecies,
        Species modifiedProperties, Patch? editorTargetPatch) : base(world, globalCache)
    {
        OriginalEditedSpecies = originalEditedSpecies;
        ModifiedProperties = modifiedProperties;
        EditorTargetPatch = editorTargetPatch;
    }

    public Species OriginalEditedSpecies { get; }
    public Species ModifiedProperties { get; }

    /// <summary>
    ///   Ensures the <see cref="ModifiedProperties"/> is always set to be in this target patch it wants to move to
    ///   so that the move target also affects the editor auto-evo run.
    /// </summary>
    public Patch? EditorTargetPatch { get; }

    public bool CollectEnergyInfo { get; set; } = true;

    /// <summary>
    ///   Set to false in order to disable the player species population change cap, which can skew the results
    ///   depending on the previous population
    /// </summary>
    public bool ApplyPlayerPopulationChangeClamp { get; set; } = true;

    protected override void GatherInfo(Queue<IRunStep> steps)
    {
        // Custom run setup for editor's use
        var map = Parameters.World.Map;
        var worldSettings = Parameters.World.WorldSettings;

        var generateMicheCache = new SimulationCache(worldSettings);

        foreach (var entry in map.Patches)
        {
            steps.Enqueue(new GenerateMiche(entry.Value, generateMicheCache, globalCache));
        }

        var populationCalculation = new CalculatePopulation(configuration, worldSettings, map,
            new Dictionary<Species, Species>
                { { OriginalEditedSpecies, ModifiedProperties } }, CollectEnergyInfo);

        if (EditorTargetPatch != null)
        {
            populationCalculation.EnsurePatchesHaveSpecies = new Dictionary<Patch, Species>
            {
                { EditorTargetPatch, ModifiedProperties },
            };
        }

        steps.Enqueue(populationCalculation);

        if (ApplyPlayerPopulationChangeClamp)
        {
            AddPlayerSpeciesPopulationChangeClampStep(steps, map, worldSettings,
                OriginalEditedSpecies.PlayerSpecies ? ModifiedProperties : null, OriginalEditedSpecies);
        }
    }
}
