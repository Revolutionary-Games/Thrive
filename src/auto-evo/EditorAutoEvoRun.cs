﻿using System.Collections.Generic;
using AutoEvo;

/// <summary>
///   Custom auto-evo run for use in editor simulations to predict population numbers given specified changes
/// </summary>
public class EditorAutoEvoRun : AutoEvoRun
{
    public EditorAutoEvoRun(GameWorld world, AutoEvoGlobalCache globalCache, Species originalEditedSpecies,
        Species modifiedProperties) : base(world, globalCache)
    {
        OriginalEditedSpecies = originalEditedSpecies;
        ModifiedProperties = modifiedProperties;
    }

    public Species OriginalEditedSpecies { get; }
    public Species ModifiedProperties { get; }

    public bool CollectEnergyInfo { get; set; } = true;

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

        steps.Enqueue(new CalculatePopulation(configuration, worldSettings, map,
            new Dictionary<Species, Species>
                { { OriginalEditedSpecies, ModifiedProperties } }, CollectEnergyInfo));

        AddPlayerSpeciesPopulationChangeClampStep(steps, map,
            OriginalEditedSpecies.PlayerSpecies ? ModifiedProperties : null, OriginalEditedSpecies);
    }
}
