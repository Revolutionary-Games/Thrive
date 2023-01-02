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

    private static List<string>? currentWorldSpeciesHeader;

    private static List<string>? currentWorldPatchHeader;

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
        currentWorldSpeciesHeader = null;
        currentWorldPatchHeader = null;

        if (currentWorldExportSettings.HasFlag(CurrentWorldExportSettings.CurrentSpeciesDetails))
        {
            ExportCurrentWorldCurrentSpeciesDetails(basePath);
        }

        if (currentWorldExportSettings.HasFlag(CurrentWorldExportSettings.CurrentPatchDetails))
        {
            ExportCurrentWorldCurrentPatchDetails(basePath);
        }

        if (currentWorldExportSettings.HasFlag(CurrentWorldExportSettings.PerSpeciesDetailedHistory))
        {
            ExportCurrentWorldPerSpeciesDetailedHistory(basePath);
        }

        if (currentWorldExportSettings.HasFlag(CurrentWorldExportSettings.PerPatchHistory))
        {
            ExportCurrentWorldPerPatchHistory(basePath);
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

    private IEnumerable<string> GetSpeciesHeader()
    {
        if (currentWorldSpeciesHeader != null)
            return currentWorldSpeciesHeader;

        currentWorldSpeciesHeader = new List<string> { "Population", "Color" };
        currentWorldSpeciesHeader.AddRange(Enum.GetNames(typeof(BehaviouralValueType))
            .OrderBy(n => Enum.Parse(typeof(BehaviouralValueType), n)));

        currentWorldSpeciesHeader.AddRange(new[]
        {
            "Membrane type", "Membrane rigidity", "Base speed", "Base rotation speed", "Storage capacity", "Bacteria",
            "Organelle count",
        });

        currentWorldSpeciesHeader.AddRange(AllOrganelles.Select(o => o.Name));

        currentWorldSpeciesHeader.AddRange(
            world.GameProperties.GameWorld.Map.Patches.Values.Select(p => p.Name.ToString()));

        return currentWorldSpeciesHeader;
    }

    private IEnumerable<string> GetSpeciesData(uint speciesId, int generation)
    {
        if (!world.SpeciesHistoryList[generation].TryGetValue(speciesId, out var species))
        {
            var emptyData = new string[2 + Enum.GetNames(typeof(BehaviouralValueType)).Length + 7 + AllOrganelles.Count +
                world.GameProperties.GameWorld.Map.Patches.Count];

            emptyData[0] = "Species doesn't exist";

            return emptyData;
        }

        var data = new List<string> { species.Population.ToString(), "#" + species.Colour.ToHtml() };
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

        data.AddRange(world.GameProperties.GameWorld.Map.Patches.Values
            .Select(p => p.History[world.CurrentGeneration - generation].SpeciesInPatch
                .FirstOrDefault(s => s.Key.ID == speciesId).Value.ToString()));

        return data;
    }

    private void ExportCurrentWorldCurrentSpeciesDetails(string basePath)
    {
        var path = Path.Combine(basePath, nameof(CurrentWorldExportSettings.CurrentSpeciesDetails) + ".csv");
        var file = new File();
        file.Open(path, File.ModeFlags.Write);

        var header = new List<string> { "Name" };
        header.AddRange(GetSpeciesHeader());

        file.StoreCsvLine(header.ToArray());

        foreach (var species in world.GameProperties.GameWorld.Species.Values)
        {
            var data = new List<string> { species.FormattedName };
            data.AddRange(GetSpeciesData(species.ID, world.CurrentGeneration));
            file.StoreCsvLine(data.ToArray());
        }

        file.Close();
    }

    private void ExportCurrentWorldPerSpeciesDetailedHistory(string basePath)
    {
        basePath = Path.Combine(basePath, nameof(CurrentWorldExportSettings.PerSpeciesDetailedHistory));
        FileHelpers.MakeSureDirectoryExists(basePath);

        var headerList = new List<string> { "Generation" };
        headerList.AddRange(GetSpeciesHeader());
        var header = headerList.ToArray();

        var maxSpeciesId = world.SpeciesHistoryList.Last().Max(s => s.Key);

        for (uint speciesId = 1; speciesId <= maxSpeciesId; ++speciesId)
        {
            var path = Path.Combine(basePath, CurrentWorldSpecies[speciesId] + ".csv");
            var file = new File();
            file.Open(path, File.ModeFlags.Write);

            file.StoreCsvLine(header);

            for (int generation = 1; generation <= world.CurrentGeneration; ++generation)
            {
                var data = new List<string> { generation.ToString() };
                data.AddRange(GetSpeciesData(speciesId, generation));
                file.StoreCsvLine(data.ToArray());
            }

            file.Close();
        }
    }

    private IEnumerable<string> GetPatchHeader()
    {
        if (currentWorldPatchHeader != null)
            return currentWorldPatchHeader;

        currentWorldPatchHeader = new List<string> { "Type" };
        currentWorldPatchHeader.AddRange(world.GameProperties.GameWorld.Map.Patches.First().Value.Biome.Compounds.Keys
            .Select(c => c.Name).OrderBy(s => s));

        currentWorldPatchHeader.AddRange(CurrentWorldSpecies.Select(p => p.Value));

        return currentWorldPatchHeader;
    }

    private IEnumerable<string> GetPatchData(Patch patch, PatchSnapshot snapshot)
    {
        var data = new List<string> { patch.BiomeType.ToString() };
        data.AddRange(snapshot.Biome.CurrentCompoundAmounts.OrderBy(p => p.Key.Name).Select(p =>
            $"Amount = {p.Value.Amount}; Ambient = {p.Value.Ambient}; Density = {p.Value.Density}"));

        data.AddRange(CurrentWorldSpecies.Select(p =>
            snapshot.SpeciesInPatch.FirstOrDefault(s => s.Key.ID == p.Key).Value.ToString()));

        return data;
    }

    private void ExportCurrentWorldCurrentPatchDetails(string basePath)
    {
        var path = Path.Combine(basePath, nameof(CurrentWorldExportSettings.CurrentPatchDetails) + ".csv");
        var file = new File();
        file.Open(path, File.ModeFlags.Write);

        var header = new List<string> { "Name" };
        header.AddRange(GetPatchHeader());

        file.StoreCsvLine(header.ToArray());

        foreach (var patch in world.GameProperties.GameWorld.Map.Patches.Values)
        {
            var data = new List<string> { patch.Name.ToString() };
            data.AddRange(GetPatchData(patch, patch.CurrentSnapshot));

            file.StoreCsvLine(data.ToArray());
        }

        file.Close();
    }

    private void ExportCurrentWorldPerPatchHistory(string basePath)
    {
        basePath = Path.Combine(basePath, nameof(CurrentWorldExportSettings.PerPatchHistory));
        FileHelpers.MakeSureDirectoryExists(basePath);

        var headerList = new List<string> { "Generation" };
        headerList.AddRange(GetPatchHeader());
        var header = headerList.ToArray();

        foreach (var patch in world.GameProperties.GameWorld.Map.Patches)
        {
            var path = Path.Combine(basePath, patch.Value.Name + ".csv");
            var file = new File();
            file.Open(path, File.ModeFlags.Write);
            file.StoreCsvLine(header);

            for (int generation = 1; generation <= world.CurrentGeneration; ++generation)
            {
                var data = new List<string> { generation.ToString() };
                data.AddRange(GetPatchData(patch.Value,
                    world.GameProperties.GameWorld.Map.Patches[patch.Key]
                        .History[world.CurrentGeneration - generation]));
                file.StoreCsvLine(data.ToArray());
            }

            file.Close();
        }
    }
}
