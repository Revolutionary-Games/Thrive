using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Godot;
using File = Godot.File;
using Path = System.IO.Path;

/// <summary>
///   Partial class: Export
/// </summary>
public partial class AutoEvoExploringTool
{
    private static readonly List<OrganelleDefinition> AllOrganelles =
        SimulationParameters.Instance.GetAllOrganelles().ToList();

    private static readonly Dictionary<uint, string> CurrentWorldSpecies = new();

    private void ExportCurrentWorld()
    {
        if (currentWorldExportSettings == 0 || world.CurrentGeneration == 0)
        {
            exportSuccessNotificationDialog.DialogText = TranslationServer.Translate("NOTHING_TO_EXPORT");
            exportSuccessNotificationDialog.PopupCenteredShrink();
            return;
        }

        currentWorldExportButton.Disabled = true;
        var basePath = Path.Combine(Constants.AUTO_EVO_EXPORT_FOLDER, DateTime.Now.ToString("yyyyMMdd_hh_mm_ss"));

        FileHelpers.MakeSureDirectoryExists(basePath);

        InitWorldSpecies();

        if (currentWorldExportSettings.HasFlag(CurrentWorldExportSettings.SpeciesHistory))
        {
            ExportCurrentWorldSpeciesHistory(basePath);
        }

        if (currentWorldExportSettings.HasFlag(CurrentWorldExportSettings.PatchHistory))
        {
            ExportCurrentWorldPatchHistory(basePath);
        }

        if (currentWorldExportSettings.HasFlag(CurrentWorldExportSettings.PopulationHistory))
        {
            ExportCurrentWorldPerSpeciesDetailedHistory(basePath);
        }

        currentWorldExportButton.Disabled = false;
        exportSuccessNotificationDialog.DialogText =
            TranslationServer.Translate("CURRENT_WORLD_EXPORTATION_SUCCESS").FormatSafe(basePath);
        exportSuccessNotificationDialog.PopupCenteredShrink();
    }

    private void InitWorldSpecies()
    {
        CurrentWorldSpecies.Clear();

        uint maxSpeciesId = world.SpeciesHistoryList.Last().Max(s => s.Key);
        for (uint speciesId = 1; speciesId <= maxSpeciesId; ++speciesId)
        {
            CurrentWorldSpecies.Add(speciesId,
                world.SpeciesHistoryList.First(d => d.ContainsKey(speciesId))[speciesId].FormattedName);
        }
    }

    private void ExportCurrentWorldSpeciesHistory(string basePath)
    {
        var path = Path.Combine(basePath, nameof(CurrentWorldExportSettings.SpeciesHistory) + ".csv");
        var file = new File();
        file.Open(path, File.ModeFlags.Write);

        // Generate headers
        var header = new List<string> { "Name", "Generation", "Population", "Color" };

        header.AddRange(Enum.GetNames(typeof(BehaviouralValueType))
            .OrderBy(n => Enum.Parse(typeof(BehaviouralValueType), n)));

        header.AddRange(new[]
        {
            "Membrane type", "Membrane rigidity", "Base speed", "Base rotation speed", "Storage capacity", "Bacteria",
            "Organelle count",
        });

        header.AddRange(AllOrganelles.Select(o => o.Name));
        file.StoreCsvLine(header.ToArray());

        // Generate data
        var maxSpeciesId = world.SpeciesHistoryList.Last().Max(s => s.Key);

        for (uint speciesId = 1; speciesId <= maxSpeciesId; ++speciesId)
        {
            for (int generation = 1; generation <= world.CurrentGeneration; ++generation)
            {
                if (!world.SpeciesHistoryList[generation].TryGetValue(speciesId, out var species))
                    continue;

                var data = new List<string>
                {
                    CurrentWorldSpecies[speciesId], generation.ToString(), species.Population.ToString(),
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

                    data.AddRange(AllOrganelles
                        .Select(o => microbeSpecies.Organelles.Count(ot => ot.Definition == o).ToString()));
                }
                else
                {
                    data.AddRange(new string[7 + AllOrganelles.Count]);
                }

                file.StoreCsvLine(data.ToArray());
            }
        }

        file.Close();
    }

    private void ExportCurrentWorldPerSpeciesDetailedHistory(string path)
    {
        path = Path.Combine(path, nameof(CurrentWorldExportSettings.PopulationHistory) + ".csv");
        var file = new File();
        file.Open(path, File.ModeFlags.Write);

        var header = new[] { "Generation", "Patch", "Species", "Population" };
        file.StoreCsvLine(header);

        foreach (var patch in world.GameProperties.GameWorld.Map.Patches.Values)
        {
            for (int generation = 1; generation <= world.CurrentGeneration; ++generation)
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
        var path = Path.Combine(basePath, nameof(CurrentWorldExportSettings.PatchHistory) + ".csv");
        var file = new File();
        file.Open(path, File.ModeFlags.Write);

        var header = new List<string> { "Name", "Generation", "Type" };

        foreach (var compound in world.GameProperties.GameWorld.Map.Patches.First().Value.Biome.Compounds.Keys
                     .Select(c => c.Name).OrderBy(s => s))
        {
            header.AddRange(new[] { compound + " Amount", compound + " Ambient", compound + " Density" });
        }

        file.StoreCsvLine(header.ToArray());

        foreach (var patch in world.GameProperties.GameWorld.Map.Patches.Values)
        {
            for (int generation = 1; generation <= world.CurrentGeneration; ++generation)
            {
                var data = new List<string>
                    { patch.Name.ToString(), generation.ToString(), patch.BiomeType.ToString() };

                var snapshot = world.PatchHistoryList[generation][patch.ID];

                data.AddRange(snapshot.Biome.CurrentCompoundAmounts.OrderBy(p => p.Key.Name).Select(p =>
                    $"Amount = {p.Value.Amount}; Ambient = {p.Value.Ambient}; Density = {p.Value.Density}"));

                file.StoreCsvLine(data.ToArray());
            }
        }

        file.Close();
    }
}
