namespace Scripts;

using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using ScriptsBase.Utilities;
using SharedBase.Utilities;

/// <summary>
///   A simple tool to make a dummy export with Godot to make
/// </summary>
public class GodotProjectValidMaker
{
    private const string DummyTargetName = "dummy";
    private const string DummyBuildsFolder = "builds";
    private const string DummyBuildRelativePath = "builds/a.x86_64";
    private const string DummyBuildPckFile = "builds/a.pck";

    private const string AssetsOriginalFolder = "assets";
    private const string MovedAssetsFolder = ".skip-import-assets";

    private readonly Program.GodotProjectValidMakerOptions options;

    public GodotProjectValidMaker(Program.GodotProjectValidMakerOptions options)
    {
        this.options = options;
    }

    public async Task<int> Run(CancellationToken cancellationToken)
    {
        var stopwatch = new Stopwatch();
        stopwatch.Start();

        cancellationToken.ThrowIfCancellationRequested();

        ColourConsole.WriteNormalLine("Moving assets around to skip them being exported");
        await MoveAssetsAway();

        try
        {
            ColourConsole.WriteNormalLine("Running Godot dummy export...");
            await RunGodot(cancellationToken);
        }
        finally
        {
            ColourConsole.WriteNormalLine("Restoring assets");
            RestoreAssetsFolder();
        }

        ColourConsole.WriteNormalLine($"Making project valid took: {stopwatch.Elapsed}");
        return 0;
    }

    private async Task MoveAssetsAway()
    {
        if (Directory.Exists(MovedAssetsFolder))
        {
            ColourConsole.WriteErrorLine("Moved asset folder already exists! Not moving anything");
        }
        else
        {
            Directory.Move(AssetsOriginalFolder, MovedAssetsFolder);
        }

        await PackageTool.EnsureGodotIgnoreFileExistsInFolder(MovedAssetsFolder);
    }

    private void RestoreAssetsFolder()
    {
        if (Directory.Exists(AssetsOriginalFolder))
        {
            ColourConsole.WriteNormalLine("Deleting the original named assets folder to make space for the restore");
            Directory.Delete(AssetsOriginalFolder, true);
        }

        Directory.Move(MovedAssetsFolder, AssetsOriginalFolder);

        PackageTool.RemoveGodotIgnoreFileIfExistsInFolder(AssetsOriginalFolder);
    }

    private async Task RunGodot(CancellationToken cancellationToken)
    {
        Directory.CreateDirectory(DummyBuildsFolder);
        await PackageTool.EnsureGodotIgnoreFileExistsInFolder(DummyBuildsFolder);

        var startInfo = new ProcessStartInfo("godot");
        startInfo.ArgumentList.Add(PackageTool.GODOT_HEADLESS_FLAG);
        startInfo.ArgumentList.Add("--export");
        startInfo.ArgumentList.Add(DummyTargetName);
        startInfo.ArgumentList.Add(DummyBuildRelativePath);

        var result = await ProcessRunHelpers.RunProcessAsync(startInfo, cancellationToken, true);

        if (result.ExitCode != 0)
        {
            ColourConsole.WriteNormalLine(
                "Exporting with Godot failed, this should not be a critical failure at this point");
        }
        else
        {
            ColourConsole.WriteNormalLine("Godot dummy export exited with code 0");
        }

        // Only print the output when it didn't include the right stuff in it
        bool printOutput = false;

        var fullOutput = result.FullOutput;

        if (!fullOutput.Contains("mono_project_debug_build"))
            printOutput = true;

        if (!fullOutput.Contains("Building project solution"))
            printOutput = true;

        if (!fullOutput.Contains("build: end"))
            printOutput = true;

        if (printOutput)
        {
            ColourConsole.WriteNormalLine($"Godot output (likely had a problem): {fullOutput}");
        }
        else if (options.Verbose)
        {
            ColourConsole.WriteDebugLine(fullOutput);
        }

        DeleteIfExists(DummyBuildRelativePath);
        DeleteIfExists(DummyBuildPckFile);
    }

    private void DeleteIfExists(string file)
    {
        if (File.Exists(file))
        {
            ColourConsole.WriteDebugLine($"Deleting dummy file: {file}");
            File.Delete(file);
        }
    }
}
