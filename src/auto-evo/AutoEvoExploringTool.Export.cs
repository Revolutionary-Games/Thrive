using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using Godot;

/// <summary>
///   Partial class: Data export functionality of the auto-evo exploring tool
/// </summary>
public partial class AutoEvoExploringTool
{
    private void ExportWorlds()
    {
        var previousWorld = worldsList.FindIndex(w => w == world);
        worldExportButton.Disabled = true;
        var exportPath = Path.Combine(Constants.AUTO_EVO_EXPORT_FOLDER, DateTime.Now.ToString("yyyyMMdd_hh_mm_ss"));

        for (int worldToExport = 0; worldToExport < worldsList.Count; ++worldToExport)
        {
            // Init the world (we need evolutionary tree data so tree needs to be built)
            WorldsListMenuIndexChanged(worldToExport);

            var basePath = Path.Combine(exportPath, worldToExport.ToString(CultureInfo.InvariantCulture));
            FileHelpers.MakeSureDirectoryExists(basePath);

            ExportCurrentWorldSpeciesHistory(basePath);
            ExportCurrentWorldPatchHistory(basePath);
            ExportCurrentWorldPopulationHistory(basePath);
        }

        worldExportButton.Disabled = false;
        exportSuccessNotificationDialog.DialogText = Localization.Translate("WORLD_EXPORT_SUCCESS_MESSAGE")
            .FormatSafe(ProjectSettings.GlobalizePath(exportPath));

        exportSuccessNotificationDialog.PopupCenteredShrink();

        WorldsListMenuIndexChanged(previousWorld);
    }

    private void ExportCurrentWorldSpeciesHistory(string basePath)
    {
        var path = Path.Combine(basePath, "species_history.csv");

        using var file = Godot.FileAccess.Open(path, Godot.FileAccess.ModeFlags.Write);
        if (file == null)
        {
            GD.PrintErr("Couldn't open target file for world history writing");
            return;
        }

        // Generate headers
        var header = new List<string> { "Name", "Generation", "Split from", "Population", "Color" };

        header.AddRange(Enum.GetNames(typeof(BehaviouralValueType))
            .OrderBy(n => Enum.Parse(typeof(BehaviouralValueType), n)));

        header.AddRange(new[]
        {
            "Membrane type", "Membrane rigidity", "Base speed", "Base rotation speed", "Storage capacity", "Bacteria",
            "Organelle count",
        });

        header.AddRange(allOrganelles.Select(o => o.UntranslatedName));
        file.StoreCsvLine(header.ToArray());

        // Generate data
        var maxSpeciesId = evolutionaryTree.CurrentWorldSpecies.Count;

        for (uint speciesId = 1; speciesId <= maxSpeciesId; ++speciesId)
        {
            for (int generation = 0; generation <= world.CurrentGeneration; ++generation)
            {
                if (!world.SpeciesHistoryList[generation].TryGetValue(speciesId, out var species))
                    continue;

                var splitFrom = evolutionaryTree.SpeciesOrigin[speciesId].ParentSpeciesId == uint.MaxValue ?
                    string.Empty :
                    evolutionaryTree.CurrentWorldSpecies[evolutionaryTree.SpeciesOrigin[speciesId].ParentSpeciesId];

                var data = new List<string>
                {
                    evolutionaryTree.CurrentWorldSpecies[speciesId], generation.ToString(),
                    splitFrom,
                    species.Population.ToString(),
                    "#" + species.Colour.ToHtml(),
                };

                data.AddRange(species.Behaviour.OrderBy(p => p.Key)
                    .Select(p => p.Value.ToString(CultureInfo.InvariantCulture)));

                if (species is MicrobeSpecies microbeSpecies)
                {
                    data.AddRange(new[]
                    {
                        microbeSpecies.MembraneType.Name,
                        microbeSpecies.MembraneRigidity.ToString(CultureInfo.InvariantCulture),
                        microbeSpecies.BaseSpeed.ToString(CultureInfo.InvariantCulture),
                        microbeSpecies.BaseRotationSpeed.ToString(CultureInfo.InvariantCulture),
                        microbeSpecies.StorageCapacity.ToString(CultureInfo.InvariantCulture),
                        microbeSpecies.IsBacteria.ToString(),
                        microbeSpecies.Organelles.Count.ToString(),
                    });

                    data.AddRange(allOrganelles
                        .Select(d => microbeSpecies.Organelles.Count(t => t.Definition == d).ToString()));
                }
                else
                {
                    data.AddRange(new string[7 + allOrganelles.Count]);
                }

                file.StoreCsvLine(data.ToArray());
            }
        }

        file.Close();
    }

    private void ExportCurrentWorldPopulationHistory(string path)
    {
        path = Path.Combine(path, "population_history.csv");

        using var file = Godot.FileAccess.Open(path, Godot.FileAccess.ModeFlags.Write);
        if (file == null)
        {
            GD.PrintErr("Couldn't open target file for population history writing");
            return;
        }

        var header = new[] { "Generation", "Patch", "Species", "Population" };
        file.StoreCsvLine(header);

        foreach (var patch in world.GameProperties.GameWorld.Map.Patches.Values)
        {
            for (int generation = 0; generation <= world.CurrentGeneration; ++generation)
            {
                var snapshot = world.PatchHistoryList[generation][patch.ID];
                foreach (var speciesPopulation in snapshot.SpeciesInPatch)
                {
                    var data = new[]
                    {
                        generation.ToString(), patch.Name.ToString(), speciesPopulation.Key.FormattedName,
                        speciesPopulation.Value.ToString(),
                    };

                    file.StoreCsvLine(data);
                }
            }
        }

        file.Close();
    }

    private void ExportCurrentWorldPatchHistory(string basePath)
    {
        var path = Path.Combine(basePath, "patch_history.csv");

        using var file = Godot.FileAccess.Open(path, Godot.FileAccess.ModeFlags.Write);
        if (file == null)
        {
            GD.PrintErr("Couldn't open target file for patch history writing");
            return;
        }

        var header = new List<string> { "Name", "Generation", "Type" };

        foreach (var compound in world.GameProperties.GameWorld.Map.Patches.First().Value.Biome.Compounds.Keys
                     .Select(c => c.Name).OrderBy(s => s))
        {
            header.AddRange(new[] { compound + " Amount", compound + " Ambient", compound + " Density" });
        }

        file.StoreCsvLine(header.ToArray());

        foreach (var patch in world.GameProperties.GameWorld.Map.Patches.Values)
        {
            for (int generation = 0; generation <= world.CurrentGeneration; ++generation)
            {
                var data = new List<string>
                    { patch.Name.ToString(), generation.ToString(), patch.BiomeType.ToString() };

                var snapshot = world.PatchHistoryList[generation][patch.ID];

                foreach (var pair in snapshot.Biome.CurrentCompoundAmounts.OrderBy(p => p.Key.Name))
                {
                    data.AddRange(new[]
                    {
                        pair.Value.Amount.ToString(CultureInfo.InvariantCulture),
                        pair.Value.Ambient.ToString(CultureInfo.InvariantCulture),
                        pair.Value.Density.ToString(CultureInfo.InvariantCulture),
                    });
                }

                file.StoreCsvLine(data.ToArray());
            }
        }

        file.Close();
    }
}
