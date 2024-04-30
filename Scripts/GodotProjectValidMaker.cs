namespace Scripts;

using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using ScriptsBase.Utilities;
using SharedBase.Utilities;

/// <summary>
///   A basic script to import Godot assets
/// </summary>
public class GodotProjectAssetImporter
{
    private readonly Program.GodotProjectValidMakerOptions options;

    public GodotProjectAssetImporter(Program.GodotProjectValidMakerOptions options)
    {
        this.options = options;
    }

    public async Task<int> Run(CancellationToken cancellationToken)
    {
        var stopwatch = new Stopwatch();
        stopwatch.Start();

        cancellationToken.ThrowIfCancellationRequested();

        ColourConsole.WriteNormalLine("Running Godot asset import...");
        await RunGodot(cancellationToken);

        ColourConsole.WriteNormalLine($"Asset import took: {stopwatch.Elapsed}");
        return 0;
    }

    private async Task RunGodot(CancellationToken cancellationToken)
    {
        var startInfo = new ProcessStartInfo("godot");
        startInfo.ArgumentList.Add(PackageTool.GODOT_HEADLESS_FLAG);
        startInfo.ArgumentList.Add("--import");

        var result = await ProcessRunHelpers.RunProcessAsync(startInfo, cancellationToken, true);

        if (result.ExitCode != 0)
        {
            ColourConsole.WriteNormalLine(
                "Importing assets with Godot failed, this should not be a critical failure at this point");

            // TODO: detection if there was a problem with the output and printing only then
            ColourConsole.WriteNormalLine($"Godot output (likely had a problem): {result.FullOutput}");
        }
        else
        {
            ColourConsole.WriteNormalLine("Godot asset import exited with code 0");

            if (options.Verbose)
            {
                ColourConsole.WriteDebugLine(result.FullOutput);
            }
        }
    }
}
