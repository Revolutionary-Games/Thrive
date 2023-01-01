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
    private void ExportCurrentWorld()
    {
        if (currentWorldExportSettings == 0)
        {
            exportSuccessNotificationDialog.DialogText = TranslationServer.Translate("NOTHING_TO_EXPORT");
            exportSuccessNotificationDialog.PopupCenteredShrink();
            return;
        }

        currentWorldExportButton.Disabled = true;
        string basePath = Path.Combine(Constants.AUTO_EVO_EXPORT_FOLDER,
            System.DateTime.Now.ToString("yy_MM_dd_hh_mm_ss"));

        FileHelpers.MakeSureDirectoryExists(basePath);

        if (currentWorldExportSettings.HasFlag(CurrentWorldExportSettings.CurrentPatchDetails))
        {
            ExportCurrentWorldCurrentPatchDetails(basePath);
        }

        currentWorldExportButton.Disabled = false;
        exportSuccessNotificationDialog.DialogText =
            TranslationServer.Translate("CURRENT_WORLD_EXPORTATION_SUCCESS").FormatSafe(basePath);
        exportSuccessNotificationDialog.PopupCenteredShrink();
    }

    private void ExportCurrentWorldCurrentPatchDetails(string basePath)
    {
        var path = Path.Combine(basePath, nameof(CurrentWorldExportSettings.CurrentPatchDetails) + ".csv");

        var file = new File();
        file.Open(path, File.ModeFlags.Write);

        var header = new List<string>();
        header.AddRange(new[] { "Name", "Type" });
        header.AddRange(world.GameProperties.GameWorld.Map.Patches.First().Value.Biome.Compounds.Keys
            .Select(c => $"Compound - {c.Name}")
            .OrderBy(s => s));

        header.AddRange(world.GameProperties.GameWorld.Species.Values.Select(s => s.FormattedName)
            .OrderBy(s => s));

        file.StoreCsvLine(header.ToArray());

        foreach (var patch in world.GameProperties.GameWorld.Map.Patches.Values)
        {
            var data = new List<string>();
            data.AddRange(new[] { patch.Name.ToString(), patch.BiomeType.ToString() });
            data.AddRange(patch.Biome.CurrentCompoundAmounts.OrderBy(p => p.Key.Name).Select(p =>
                $"Amount = {p.Value.Amount}; Ambient = {p.Value.Ambient}; Density = {p.Value.Density}"));

            data.AddRange(world.GameProperties.GameWorld.Species.Values.OrderBy(s => s.FormattedName)
                .Select(s => patch.SpeciesInPatch.ContainsKey(s) ? patch.SpeciesInPatch[s].ToString() : "0"));

            file.StoreCsvLine(data.ToArray());
        }

        file.Close();
    }
}
