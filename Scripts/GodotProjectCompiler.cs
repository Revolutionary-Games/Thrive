namespace Scripts;

using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using ScriptsBase.Utilities;
using SharedBase.Utilities;

/// <summary>
///   A simple tool to import Godot assets
/// </summary>
public class GodotProjectCompiler
{
    private const string AssetsOriginalFolder = "assets";
    private const string MovedAssetsFolder = ".skip-import-assets";

    private readonly Program.GodotProjectValidMakerOptions options;

    public GodotProjectCompiler(Program.GodotProjectValidMakerOptions options)
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
            ColourConsole.WriteNormalLine("Running Godot C# compile (with no assets enabled)...");
            await RunGodot(cancellationToken);
        }
        finally
        {
            ColourConsole.WriteNormalLine("Restoring assets");
            RestoreAssetsFolder();
        }

        ColourConsole.WriteNormalLine($"Project compile  took: {stopwatch.Elapsed}");
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
        var startInfo = new ProcessStartInfo("godot");
        startInfo.ArgumentList.Add(PackageTool.GODOT_HEADLESS_FLAG);
        startInfo.ArgumentList.Add("--build-solutions");

        // This is needed to workaround Godot bug with it not exiting after the build otherwise
        startInfo.ArgumentList.Add("--quit-after");
        startInfo.ArgumentList.Add("2");

        var result = await ProcessRunHelpers.RunProcessAsync(startInfo, cancellationToken, true);

        // Only print the output when it didn't include the right stuff in it
        // TODO: proper detection for this
        bool printOutput = false;

        if (result.ExitCode != 0)
        {
            ColourConsole.WriteNormalLine("Building with Godot failed, this is probably not a critical failure");

            printOutput = true;
        }
        else
        {
            ColourConsole.WriteNormalLine("Godot build exited with code 0");
        }

        var fullOutput = result.FullOutput;

        if (printOutput)
        {
            ColourConsole.WriteNormalLine($"Godot output (likely had a problem): {fullOutput}");
        }
        else if (options.Verbose)
        {
            ColourConsole.WriteDebugLine(fullOutput);
        }
    }
}
