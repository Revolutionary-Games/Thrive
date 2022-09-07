namespace Scripts;

using System;
using System.Diagnostics;
using System.IO;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using DevCenterCommunication.Utilities;
using ScriptsBase.Utilities;
using SharedBase.Utilities;

public static class Dehydration
{
    public const string DEHYDRATE_CACHE = "builds/dehydrate_cache";
    public const string DEVBUILDS_FOLDER = "builds/devbuilds";

    private const long DEHYDRATE_FILE_SIZE_THRESHOLD = 100000;
    private const string DEHYDRATE_IGNORE_FILE_TYPES = @"\.po,\.pot,\.txt,\.md,\.tscn,Thrive\.dll,Thrive\.pdb";

    public static async Task DehydrateThrivePck(string pck, string extractFolder, DehydrateCache cache,
        CancellationToken cancellationToken)
    {
        Directory.CreateDirectory(extractFolder);

        // Start by extracting the big files to be dehydrated, but ignore
        // a list of well compressing / often changing files

        var pckTool = PckToolName();

        var startInfo = new ProcessStartInfo(pckTool);
        startInfo.ArgumentList.Add("--action");
        startInfo.ArgumentList.Add("extract");
        startInfo.ArgumentList.Add(pck);
        startInfo.ArgumentList.Add("-o");
        startInfo.ArgumentList.Add(extractFolder);
        startInfo.ArgumentList.Add("-q");
        startInfo.ArgumentList.Add("--min-size-filter");
        startInfo.ArgumentList.Add(DEHYDRATE_FILE_SIZE_THRESHOLD.ToString());
        startInfo.ArgumentList.Add("--exclude-regex-filter");
        startInfo.ArgumentList.Add(DEHYDRATE_IGNORE_FILE_TYPES);

        var result = await ProcessRunHelpers.RunProcessAsync(startInfo, cancellationToken, false);

        if (result.ExitCode != 0)
            throw new Exception("Failed to run extract. Do you have the right godotpcktool version?");

        // And remove them from the .pck
        startInfo = new ProcessStartInfo(pckTool);
        startInfo.ArgumentList.Add("--action");
        startInfo.ArgumentList.Add("repack");
        startInfo.ArgumentList.Add(pck);
        startInfo.ArgumentList.Add("-q");
        startInfo.ArgumentList.Add("--max-size-filter");
        startInfo.ArgumentList.Add((DEHYDRATE_FILE_SIZE_THRESHOLD - 1).ToString());
        startInfo.ArgumentList.Add("--include-override-filter");
        startInfo.ArgumentList.Add(DEHYDRATE_IGNORE_FILE_TYPES);

        result = await ProcessRunHelpers.RunProcessAsync(startInfo, cancellationToken, false);

        if (result.ExitCode != 0)
            throw new Exception("Failed to run repack");

        // Dehydrate always all the extracted files
        foreach (var file in Directory.EnumerateFiles(extractFolder, "*.*", SearchOption.AllDirectories))
        {
            await DehydrateFile(file, cache, cancellationToken);
        }

        // No longer need the temp files
        Directory.Delete(extractFolder, true);
    }

    /// <summary>
    ///   Dehydrates a file by moving it to the dehydrate cache (if needed, otherwise just deletes)
    /// </summary>
    /// <param name="file">The file to dehydrate</param>
    /// <param name="cache">Cache structure to add the file to</param>
    /// <param name="cancellationToken">Cancellation</param>
    public static async Task DehydrateFile(string file, DehydrateCache cache, CancellationToken cancellationToken)
    {
        var hash = FileUtilities.HashToHex(await FileUtilities.CalculateSha3OfFile(file, cancellationToken));

        // TODO: should this use the cache here (perhaps with a special parameter as this is a bit unexpected)
        var target = Path.Join(DEHYDRATE_CACHE, $"{hash}.gz");

        // Only copy to the dehydrate cache if hash doesn't exist
        if (!File.Exists(target))
        {
            await Compression.GzipToTarget(file, target, cancellationToken);
        }

        cache.Add(file, hash);

        File.Delete(file);
    }

    /// <summary>
    ///   Checks if a file needs to be dehydrated, and dehydrates if when needed
    /// </summary>
    /// <param name="file">The file to check</param>
    /// <param name="cache">Cache to add to if dehydrated</param>
    /// <param name="cancellationToken">Cancellation</param>
    public static Task PerformDehydrationOnFileIfNeeded(string file, DehydrateCache cache,
        CancellationToken cancellationToken)
    {
        // .pck files are handled separately
        if (file.EndsWith(".pck"))
            return Task.CompletedTask;

        if (new FileInfo(file).Length < DEHYDRATE_FILE_SIZE_THRESHOLD)
            return Task.CompletedTask;

        return DehydrateFile(file, cache, cancellationToken);
    }

    public static async Task WriteMetaFile(string name, DehydrateCache cache, string thriveVersion,
        string godotPlatform, CancellationToken cancellationToken)
    {
        var info = new DehydratedInfo(cache.Hashes(), await GetBranchCICompatible(cancellationToken), thriveVersion,
            godotPlatform);

        var metaName = $"{name}.meta.json";

        await File.WriteAllTextAsync(Path.Join(Dehydration.DEVBUILDS_FOLDER, metaName), JsonSerializer.Serialize(info),
            cancellationToken);
    }

    private static async Task<string> GetBranchCICompatible(CancellationToken cancellationToken)
    {
        var fromEnvironment = Environment.GetEnvironmentVariable("CI_BRANCH");

        if (!string.IsNullOrEmpty(fromEnvironment))
            return fromEnvironment;

        return await GitRunHelpers.GetCurrentBranch("./", cancellationToken);
    }

    private static string PckToolName()
    {
        if (OperatingSystem.IsWindows())
            return "godotpcktool.exe";

        return "godotpcktool";
    }
}
