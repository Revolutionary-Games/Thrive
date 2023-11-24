namespace Scripts;

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using DevCenterCommunication.Utilities;
using ScriptsBase.Models;
using ScriptsBase.ToolBases;
using ScriptsBase.Utilities;
using SharedBase.Models;
using SharedBase.Utilities;

public class PackageTool : PackageToolBase<Program.PackageOptions>
{
    private const string EXPECTED_THRIVE_DATA_FOLDER = "data_Thrive";
    private const string EXPECTED_THRIVE_WASM = "Thrive.wasm";

    private const string STEAM_BUILD_MESSAGE = "This is the Steam build. This can only be distributed by " +
        "Revolutionary Games Studio (under a special license) due to Steam being incompatible with the GPL license!";

    private const string STEAM_README_TEMPLATE = "doc/steam_license_readme.txt";

    private static readonly Regex GodotVersionRegex = new(@"([\d\.]+)\..*mono");

    private static readonly IReadOnlyList<PackagePlatform> ThrivePlatforms = new List<PackagePlatform>
    {
        PackagePlatform.Linux,
        PackagePlatform.Windows,
        PackagePlatform.Windows32,
        PackagePlatform.Mac,
        PackagePlatform.Web,
    };

    /// <summary>
    ///   Files that will never be considered for dehydrating
    /// </summary>
    private static readonly IReadOnlyList<string> DehydrateIgnoreFiles = new List<string>
    {
        "source.7z",
        "revision.txt",
        "ThriveAssetsLICENSE.txt",
        "GodotLicense.txt",
        "runtime_licenses.txt",
        "gpl.txt",
        "LICENSE.txt",
        "README.txt",
    };

    private static readonly IReadOnlyCollection<FileToPackage> LicenseFiles = new List<FileToPackage>
    {
        new("assets/LICENSE.txt", "ThriveAssetsLICENSE.txt"),
        new("assets/README.txt", "ThriveAssetsREADME.txt"),
        new("doc/GodotLicense.txt", "GodotLicense.txt"),
        new("doc/RuntimeLicenses.txt", "RuntimeLicenses.txt"),
    };

    private static readonly IReadOnlyCollection<FileToPackage> NonSteamLicenseFiles = new List<FileToPackage>
    {
        new("LICENSE.txt"),
        new("gpl.txt"),
    };

    private static readonly IReadOnlyCollection<string> SourceItemsToPackage = new List<string>
    {
        "default_bus_layout.tres",
        "default_env.tres",
        "Directory.Build.props",
        "export_presets.cfg",
        "global.json",
        "LICENSE.txt",
        "project.godot",
        "Thrive.csproj",
        "Thrive.sln",
        "Thrive.sln.DotSettings",
        "doc",
        "Properties",
        "shaders",
        "simulation_parameters",
        "src",
        "third_party/Directory.Build.props",
        "third_party/ThirdParty.csproj",
        "third_party/FastNoiseLite.cs",
        "third_party/StyleCop.ruleset",
        "README.md",
        "RevolutionaryGamesCommon",
    };

    /// <summary>
    ///   Base files (non-Steam, no license) that are copied to the exported game folders
    /// </summary>
    private static readonly IReadOnlyCollection<FileToPackage> BaseFilesToPackage = new List<FileToPackage>
    {
        new("assets/misc/Thrive.desktop", "Thrive.desktop", PackagePlatform.Linux),
        new("assets/misc/thrive_logo_big.png", "Thrive.png", PackagePlatform.Linux),
    };

    private string thriveVersion;

    private bool checkedGodot;
    private bool steamMode;

    private DehydrateCache? cacheForNextMetaToWrite;

    public PackageTool(Program.PackageOptions options) : base(options)
    {
        if (options.Dehydrated)
        {
            DefaultPlatforms = new[] { PackagePlatform.Linux, PackagePlatform.Windows };
        }
        else
        {
            // For now our mac builds kind of need to be done on a mac so this reflects that
            if (OperatingSystem.IsMacOS())
            {
                DefaultPlatforms = new[] { PackagePlatform.Mac };
            }
            else
            {
                DefaultPlatforms = ThrivePlatforms.Where(p => p != PackagePlatform.Mac && p != PackagePlatform.Web)
                    .ToList();
            }
        }

        thriveVersion = AssemblyInfoReader.ReadVersionFromAssemblyInfo(true);
    }

    protected override IReadOnlyCollection<PackagePlatform> ValidPlatforms => ThrivePlatforms;

    protected override IEnumerable<PackagePlatform> DefaultPlatforms { get; }

    protected override IEnumerable<string> SourceFilesToPackage => SourceItemsToPackage;

    private string ReadmeFile => Path.Join(options.OutputFolder, "README.txt");
    private string RevisionFile => Path.Join(options.OutputFolder, "revision.txt");
    private string SteamLicenseFile => Path.Join(options.OutputFolder, "LICENSE_steam.txt");

    public static async Task EnsureGodotIgnoreFileExistsInFolder(string folder)
    {
        var ignoreFile = Path.Join(folder, ".gdignore");

        if (!File.Exists(ignoreFile))
        {
            ColourConsole.WriteDebugLine($"Creating .gdignore file in folder: {folder}");
            await using var writer = File.Create(ignoreFile);
        }
    }

    public static void RemoveGodotIgnoreFileIfExistsInFolder(string folder)
    {
        var ignoreFile = Path.Join(folder, ".gdignore");

        if (File.Exists(ignoreFile))
        {
            File.Delete(ignoreFile);
        }
    }

    protected override async Task<bool> OnBeforeStartExport(CancellationToken cancellationToken)
    {
        // For now, by default disable Steam mode to make the script easier to use
        options.Steam ??= false;

        if (options.Steam != null)
        {
            ColourConsole.WriteInfoLine($"Will set Steam mode to {options.Steam.Value} before exporting");
            steamMode = options.Steam.Value;
        }
        else
        {
            steamMode = await SteamBuild.IsSteamBuildEnabled(cancellationToken);
        }

        var currentCommit = await GitRunHelpers.GetCurrentCommit("./", cancellationToken);

        if (options.Dehydrated)
        {
            if (steamMode)
            {
                ColourConsole.WriteErrorLine("Dehydrate option conflicts with Steam mode");
                return false;
            }

            ColourConsole.WriteNormalLine("Making dehydrated devbuilds");

            thriveVersion = currentCommit;
        }

        if (steamMode)
        {
            options.CompressRaw = false;
        }

        // By default source code is included in non-Steam builds
        options.SourceCode ??= !steamMode;

        await CreateDynamicallyGeneratedFiles(cancellationToken);

        // Make sure godot ignores the builds folder in terms of imports
        await EnsureGodotIgnoreFileExistsInFolder(options.OutputFolder);

        if (!await CheckGodotIsAvailable(cancellationToken))
            return false;

        // For CI we need to get the branch from a special variable
        var currentBranch = Environment.GetEnvironmentVariable("CI_BRANCH");

        if (string.IsNullOrWhiteSpace(currentBranch))
        {
            currentBranch = await GitRunHelpers.GetCurrentBranch("./", cancellationToken);
        }

        await BuildInfoWriter.WriteBuildInfo(currentCommit, currentBranch, options.Dehydrated, cancellationToken);

        return true;
    }

    protected override string GetFolderNameForExport(PackagePlatform platform)
    {
        return ThriveProperties.GetFolderNameForPlatform(platform, thriveVersion, steamMode);
    }

    protected override string GetCompressedExtensionForPlatform(PackagePlatform platform)
    {
        if (platform == PackagePlatform.Mac)
            return ".zip";

        return base.GetCompressedExtensionForPlatform(platform);
    }

    protected override async Task<bool> PrepareToExport(PackagePlatform platform, CancellationToken cancellationToken)
    {
        // TODO: mac steam support
        if (options.Steam != null && platform is not PackagePlatform.Mac and not PackagePlatform.Web)
        {
            if (!await SteamBuild.SetBuildMode(options.Steam.Value, true, cancellationToken,
                    SteamBuild.ConvertPackagePlatformToSteam(platform)))
            {
                ColourConsole.WriteErrorLine("Failed to set wanted Steam mode");
                return false;
            }
        }
        else
        {
            // Force disable Steam for unsupported platforms
            if (!await SteamBuild.SetBuildMode(false, true, cancellationToken, SteamBuild.SteamPlatform.Linux))
            {
                ColourConsole.WriteErrorLine("Failed to set Steam to not be used mode");
                return false;
            }
        }

        PrintSteamModeWarning();
        return true;
    }

    protected override async Task<bool> Export(PackagePlatform platform, string folder,
        CancellationToken cancellationToken)
    {
        var target = ThriveProperties.GodotTargetFromPlatform(platform, steamMode);

        ColourConsole.WriteInfoLine($"Starting export for target: {target}");

        Directory.CreateDirectory(folder);

        ColourConsole.WriteNormalLine($"Exporting to folder: {folder}");

        var targetFile = Path.Join(folder, "Thrive" + ThriveProperties.GodotTargetExtension(platform));

        var startInfo = new ProcessStartInfo("godot");
        startInfo.ArgumentList.Add("--no-window");
        startInfo.ArgumentList.Add("--export");
        startInfo.ArgumentList.Add(target);
        startInfo.ArgumentList.Add(targetFile);

        var result = await ProcessRunHelpers.RunProcessAsync(startInfo, cancellationToken, false);

        if (result.ExitCode != 0)
        {
            ColourConsole.WriteWarningLine("Exporting with Godot failed");
            return false;
        }

        if (platform == PackagePlatform.Web)
        {
            var expectedWebFile = Path.Join(folder, EXPECTED_THRIVE_WASM);

            if (!File.Exists(expectedWebFile))
            {
                ColourConsole.WriteErrorLine(
                    $"Expected web file ({expectedWebFile}) was not created on export. " +
                    "Are export templates installed?");
                return false;
            }
        }
        else if (platform != PackagePlatform.Mac)
        {
            var expectedDataFolder = Path.Join(folder, EXPECTED_THRIVE_DATA_FOLDER);

            if (!Directory.Exists(expectedDataFolder))
            {
                ColourConsole.WriteErrorLine(
                    $"Expected data folder ({expectedDataFolder}) was not created on export. " +
                    "Are export templates installed?");
                return false;
            }
        }

        ColourConsole.WriteSuccessLine("Godot export succeeded");
        return true;
    }

    protected override async Task<bool> OnPostProcessExportedFolder(PackagePlatform platform, string folder,
        CancellationToken cancellationToken)
    {
        if (steamMode)
        {
            // Create the steam specific resources

            // Copy the right Steamworks.NET library for the current target
            CopyHelpers.CopyToFolder(SteamBuild
                .PathToSteamAssemblyForPlatform(SteamBuild.ConvertPackagePlatformToSteam(platform))
                .Replace("\\", "/"), folder);
        }
        else if (platform == PackagePlatform.Mac)
        {
            ColourConsole.WriteNormalLine("Including licenses (and other common files) in mac .zip");

            var startInfo = new ProcessStartInfo("zip")
            {
                WorkingDirectory = folder,
            };
            startInfo.ArgumentList.Add("-9");
            startInfo.ArgumentList.Add("-u");
            startInfo.ArgumentList.Add(Path.GetFileName(MacZipInFolder(folder)));

            foreach (var file in GetFilesToPackage().Where(f => f.IsForPlatform(platform)))
            {
                startInfo.ArgumentList.Add(file.PackagePathAndName);
            }

            if (options.SourceCode == true)
            {
                startInfo.ArgumentList.Add(CompressedSourceName);
            }

            var result = await ProcessRunHelpers.RunProcessAsync(startInfo, cancellationToken, true);

            if (result.ExitCode != 0)
            {
                ColourConsole.WriteErrorLine(
                    $"Running zip update failed (exit: {result.ExitCode}): {result.FullOutput}");
                return false;
            }

            ColourConsole.WriteInfoLine("Extra files included in mac zip");
        }

        if (!options.Dehydrated)
        {
            var potentialCache = Path.Join(folder, DehydrateCache.CacheFileName);

            if (File.Exists(potentialCache))
            {
                ColourConsole.WriteWarningLine(
                    $"Deleting leftover dehydrate cache file in normal build: {potentialCache}");
                File.Delete(potentialCache);
            }
        }

        // Copy needed native libraries
        var nativeLibraryTool = new NativeLibs(new Program.NativeLibOptions
        {
            DebugLibrary = false,
            DisableColour = options.DisableColour,
            Verbose = options.Verbose,
        });

        ColourConsole.WriteNormalLine("Copying native libraries (hopefully they were downloaded / compiled already)");

        if (!nativeLibraryTool.CopyToThriveRelease(folder, platform, true))
        {
            bool success = false;

            if (options.FallbackToLocalNative)
            {
                ColourConsole.WriteWarningLine("Falling back to native library versions only meant for local use");
                ColourConsole.WriteNormalLine("Releases made like this are not the best and may not work on " +
                    "all target systems due to system version differences");

                success = nativeLibraryTool.CopyToThriveRelease(folder, platform, false);
            }

            if (!success)
            {
                ColourConsole.WriteErrorLine("Could not copy native libraries for release, this release won't work");
                return false;
            }
        }

        ColourConsole.WriteSuccessLine("Native library operations succeeded");

        return true;
    }

    protected override async Task<bool> Compress(PackagePlatform platform, string folder, string archiveFile,
        CancellationToken cancellationToken)
    {
        if (platform == PackagePlatform.Mac)
        {
            ColourConsole.WriteInfoLine("Mac target is already zipped, moving it instead");

            if (File.Exists(archiveFile))
                File.Delete(archiveFile);

            var sourceZip = MacZipInFolder(folder);

            File.Move(sourceZip, archiveFile);
            return true;
        }

        if (options.Dehydrated)
        {
            ColourConsole.WriteNormalLine($"Performing devbuild package on: {folder}");

            if (!await PrepareDehydratedFolder(platform, folder, cancellationToken))
            {
                ColourConsole.WriteErrorLine("Devbuild package preparing failed");
                return false;
            }
        }

        if (!await base.Compress(platform, folder, archiveFile, cancellationToken))
            return false;

        if (options.Dehydrated)
        {
            ColourConsole.WriteNormalLine($"Deleting folder that was packaged as a devbuild: {folder}");

            Directory.Delete(folder, true);
        }

        return true;
    }

    protected override async Task<bool> OnPostFolderHandled(PackagePlatform platform, string folderOrArchive,
        CancellationToken cancellationToken)
    {
        if (options.Dehydrated)
        {
            // After normal packaging, move it to the devbuilds folder for the upload script
            var target = Path.Join(Dehydration.DEVBUILDS_FOLDER, Path.GetFileName(folderOrArchive));
            File.Move(folderOrArchive, target, true);

            if (cacheForNextMetaToWrite == null)
            {
                ColourConsole.WriteErrorLine("No existing dehydrated cache data to write to meta file");
                return false;
            }

            // Write meta file needed for upload
            await Dehydration.WriteMetaFile(Path.GetFileNameWithoutExtension(folderOrArchive), cacheForNextMetaToWrite,
                thriveVersion,
                ThriveProperties.GodotTargetFromPlatform(platform, steamMode), target, cancellationToken);

            cacheForNextMetaToWrite = null;

            var message = $"Converted to devbuild: {Path.GetFileName(folderOrArchive)}";
            ColourConsole.WriteInfoLine(message);
            AddReprintMessage(message);
        }

        return true;
    }

    protected override async Task<bool> OnAfterExport(CancellationToken cancellationToken)
    {
        if (!await base.OnAfterExport(cancellationToken))
            return false;

        // Clean up the revision file to not have it hang around unnecessarily
        BuildInfoWriter.DeleteBuildInfo();

        return true;
    }

    protected override IEnumerable<FileToPackage> GetFilesToPackage()
    {
        foreach (var fileToPackage in BaseFilesToPackage)
        {
            yield return fileToPackage;
        }

        yield return new FileToPackage(ReadmeFile, "README.txt");
        yield return new FileToPackage(RevisionFile, "revision.txt");

        foreach (var fileToPackage in LicenseFiles)
        {
            yield return fileToPackage;
        }

        if (!steamMode)
        {
            foreach (var fileToPackage in NonSteamLicenseFiles)
            {
                yield return fileToPackage;
            }
        }

        // TODO: use LicensesDisplay.LoadSteamLicenseFile to generate this
        // if (steamMode)
        //     yield return new FileToPackage(SteamLicenseFile, "LICENSE.txt");
    }

    private static string MacZipInFolder(string folder)
    {
        return Path.Join(folder, "Thrive.zip");
    }

    private async Task<bool> PrepareDehydratedFolder(PackagePlatform platform, string folder,
        CancellationToken cancellationToken)
    {
        _ = platform;

        Directory.CreateDirectory(Dehydration.DEVBUILDS_FOLDER);
        Directory.CreateDirectory(Dehydration.DEHYDRATE_CACHE);

        var pck = Path.Join(folder, "Thrive.pck");

        var extractFolder = Path.Join(options.OutputFolder, "temp_extracted", Path.GetFileName(folder));

        var pckCache = new DehydrateCache(extractFolder);

        await Dehydration.DehydrateThrivePck(pck, extractFolder, pckCache, cancellationToken);

        var normalCache = new DehydrateCache(folder);

        cancellationToken.ThrowIfCancellationRequested();

        ColourConsole.WriteNormalLine(".pck dehydration complete, dehydrating other files");

        // Dehydrate other files
        foreach (var file in Directory.EnumerateFiles(folder, "*.*", SearchOption.AllDirectories))
        {
            // Always ignore some files despite their sizes
            if (DehydrateIgnoreFiles.Contains(file.Replace($"{folder}/", string.Empty)))
                continue;

            if (ColourConsole.DebugPrintingEnabled)
                ColourConsole.WriteDebugLine($"Dehydrating: {file}");

            cancellationToken.ThrowIfCancellationRequested();

            await Dehydration.PerformDehydrationOnFileIfNeeded(file, normalCache, cancellationToken);
        }

        normalCache.AddPck(pck, pckCache);

        await normalCache.WriteTo(folder, cancellationToken);

        // Meta file is written later once we know the hash of the compressed archive. This variable stores the data
        // until it is ready to be written
        cacheForNextMetaToWrite = normalCache;

        return true;
    }

    private void PrintSteamModeWarning()
    {
        if (!steamMode)
            return;

        ColourConsole.WriteWarningLine(STEAM_BUILD_MESSAGE);
        AddReprintMessage(STEAM_BUILD_MESSAGE);
    }

    private async Task<bool> CheckGodotIsAvailable(CancellationToken cancellationToken,
        string requiredVersion = GodotVersion.GODOT_VERSION)
    {
        if (checkedGodot)
            return true;

        var godot = ExecutableFinder.Which("godot");

        if (godot == null)
        {
            ColourConsole.WriteErrorLine("Godot not found in PATH with name \"godot\" please make it available");
            return false;
        }

        // Version check
        var startInfo = new ProcessStartInfo(godot);
        startInfo.ArgumentList.Add("--version");

        var result = await ProcessRunHelpers.RunProcessAsync(startInfo, cancellationToken, true);

        // Seems like Godot sometimes gives 255 for the version reading
        if (result.ExitCode != 0 && result.ExitCode != 255)
        {
            ColourConsole.WriteErrorLine(
                $"Running godot for version check failed (exit: {result.ExitCode}): {result.FullOutput}");
            return false;
        }

        var match = GodotVersionRegex.Match(result.FullOutput);

        if (!match.Success)
        {
            ColourConsole.WriteErrorLine(
                "Godot is installed but it is either not the mono version or the version could not be detected " +
                $"for some reason from: {result.FullOutput}");
            return false;
        }

        var version = match.Groups[1].Value;

        if (version != requiredVersion)
        {
            ColourConsole.WriteErrorLine(
                $"Godot is available but it is the wrong version (installed) {version} != " +
                $"{requiredVersion} (required)");
            return false;
        }

        checkedGodot = true;
        return true;
    }

    private async Task CreateDynamicallyGeneratedFiles(CancellationToken cancellationToken)
    {
        await using var readme = File.CreateText(ReadmeFile);

        if (!steamMode)
        {
            await readme.WriteLineAsync("Thrive");
            await readme.WriteLineAsync(string.Empty);
            await readme.WriteLineAsync("This is a compiled version of the game. Run the executable 'Thrive' to play.");
            await readme.WriteLineAsync(string.Empty);
            await readme.WriteLineAsync(
                "Source code is available online: https://github.com/Revolutionary-Games/Thrive");
            await readme.WriteLineAsync(string.Empty);
            await readme.WriteLineAsync("Exact commit this build is made from is in revision.txt");
        }
        else
        {
            await readme.WriteLineAsync("Thrive");
            await readme.WriteLineAsync(string.Empty);
            await readme.WriteLineAsync(
                "This is the Steam version of the game. Run the executable 'ThriveLauncher' to play.");
            await readme.WriteLineAsync(string.Empty);
            await readme.WriteLineAsync(
                "Source code is available online: https://github.com/Revolutionary-Games/Thrive");
            await readme.WriteLineAsync(
                "This version of Thrive is specially licensed and *not* under the GPLv3 license.");
            await readme.WriteLineAsync(string.Empty);
            await readme.WriteLineAsync("Exact commit this build is made from is in revision.txt");
        }

        cancellationToken.ThrowIfCancellationRequested();

        await using var revision = File.CreateText(RevisionFile);

        await revision.WriteLineAsync(await GitRunHelpers.Log("./", 1, cancellationToken));
        await revision.WriteLineAsync(string.Empty);

        var diff = (await GitRunHelpers.Diff("./", cancellationToken, false, false)).Trim();

        if (!string.IsNullOrEmpty(diff))
        {
            await readme.WriteLineAsync("dirty, diff:");
            await readme.WriteLineAsync(diff);
        }

        // TODO: use LicensesDisplay.LoadSteamLicenseFile to generate this
        // See: https://github.com/Revolutionary-Games/Thrive/issues/3771
        // if (steamMode)
        // {
        //     await using var steamLicense = File.CreateText(SteamLicenseFile);
        //
        //     var template = await File.ReadAllTextAsync(STEAM_README_TEMPLATE, cancellationToken);
        //     var normalLicense = await File.ReadAllTextAsync("LICENSE.txt", cancellationToken);
        // }
    }
}
