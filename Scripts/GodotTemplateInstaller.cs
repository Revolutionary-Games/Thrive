namespace Scripts;

using System;
using System.IO;
using System.IO.Compression;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using ScriptsBase.Utilities;

/// <summary>
///   This script downloads and installs godot export templates for current version
/// </summary>
public class GodotTemplateInstaller
{
    public static async Task<bool> Run(CancellationToken cancellationToken)
    {
        var targetPath = GetTemplateInstallPath();

        var downloadUrl =
            $"https://downloads.tuxfamily.org/godotengine/{GodotVersion.GODOT_VERSION}/mono/Godot_v" +
            $"{GodotVersion.GODOT_VERSION}-stable_mono_export_templates.tpz";

        ColourConsole.WriteInfoLine($"Installing templates to {targetPath} from {downloadUrl}");
        Directory.CreateDirectory(targetPath);

        var tempFile = Path.GetTempFileName();

        try
        {
            await Download(downloadUrl, tempFile, cancellationToken);

            ExtractTemplates(tempFile, targetPath);
        }
        finally
        {
            if (File.Exists(tempFile))
                File.Delete(tempFile);
        }

        ColourConsole.WriteSuccessLine("Done installing templates");

        return true;
    }

    private static async Task Download(string url, string target, CancellationToken cancellationToken)
    {
        ColourConsole.WriteNormalLine($"Downloading {url}");
        ColourConsole.WriteNormalLine("This may take many minutes as the download is large");

        using var client = new HttpClient();

        bool success = false;

        try
        {
            await using var writer = File.OpenWrite(target);

            var response = await client.GetAsync(url, cancellationToken);

            response.EnsureSuccessStatusCode();

            await response.Content.CopyToAsync(writer, cancellationToken);

            success = true;
        }
        finally
        {
            if (!success && File.Exists(target))
            {
                File.Delete(target);
            }
        }

        if (!success)
            throw new Exception("Download failed");

        ColourConsole.WriteSuccessLine("Download finished");
    }

    private static void ExtractTemplates(string templateFile, string targetFolder, string removePrefix = "templates/")
    {
        bool prefixRemoved = false;

        using var reader = File.OpenRead(templateFile);

        using var archive = new ZipArchive(reader, ZipArchiveMode.Read);

        ColourConsole.WriteNormalLine("Extracting templates...");

        foreach (var entry in archive.Entries)
        {
            var relative = entry.FullName;

            if (relative.StartsWith(removePrefix))
            {
                prefixRemoved = true;
                relative = relative.Substring(removePrefix.Length);
            }

            var finalPath = Path.Join(targetFolder, relative);
            var folder = Path.GetDirectoryName(finalPath);

            if (folder != null)
                Directory.CreateDirectory(folder);

            entry.ExtractToFile(finalPath, true);
        }

        ColourConsole.WriteNormalLine("Extraction complete");

        if (!prefixRemoved)
        {
            ColourConsole.WriteWarningLine("No items in zip matched prefix. Template install is likely incorrect!");
        }
    }

    private static string GetTemplateInstallPath()
    {
        if (!OperatingSystem.IsLinux())
            throw new NotImplementedException("Currently only implemented for Linux");

        return Path.Join(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
            $".local/share/godot/templates/{GodotVersion.GODOT_VERSION_FULL}");
    }
}
