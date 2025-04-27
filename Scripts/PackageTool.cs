namespace Scripts;

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using DevCenterCommunication.Models.Enums;
using DevCenterCommunication.Utilities;
using ScriptsBase.Models;
using ScriptsBase.ToolBases;
using ScriptsBase.Utilities;
using SharedBase.Models;
using SharedBase.Utilities;

public class PackageTool : PackageToolBase<Program.PackageOptions>
{
    public const string GODOT_HEADLESS_FLAG = "--headless";

    private const string EXPECTED_THRIVE_PCK_FILE = "Thrive.pck";

    private const string STEAM_BUILD_MESSAGE = "This is the Steam build. This can only be distributed by " +
        "Revolutionary Games Studio (under a special license) due to Steam being incompatible with the GPL license!";

    private const string STEAM_README_TEMPLATE = "doc/steam_license_readme.txt";

    private const string MONO_IDENTIFIER = ".mono.";

    private const string MAC_ZIP_LIBRARIES_PATH = "Thrive.app/Contents/MacOS";
    private const string MAC_ZIP_GD_EXTENSION_TARGET_PATH = "Thrive.app/Contents/MacOS";
    private const string MAC_MAIN_EXECUTABLE = "Thrive.app/Contents/MacOS/Thrive";

    private const string MAC_ENTITLEMENTS = "Scripts/Thrive.entitlements";

    private const string GODOT_PROJECT_FILE = "project.godot";

    private static readonly Regex GodotVersionRegex = new(@"([\d\.]+)\..*mono");

    private static readonly Regex ClearColourOptionRegex =
        new("environment\\/defaults\\/default_clear_color=.+[\n\r]+");

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
        "ThriveAssetsREADME.txt",
        "GodotLicense.txt",
        "RuntimeLicenses.txt",
        "gpl.txt",
        "LICENSE.txt",
        "LicenseTexts.txt",
        "README.txt",
        "Thrive.dll",
        "Thrive.pdb",
    };

    private static readonly IReadOnlyCollection<FileToPackage> LicenseFiles = new List<FileToPackage>
    {
        new("assets/LICENSE.txt", "ThriveAssetsLICENSE.txt"),
        new("assets/README.txt", "ThriveAssetsREADME.txt"),
        new("doc/GodotLicense.txt", "GodotLicense.txt"),
        new("doc/RuntimeLicenses.txt", "RuntimeLicenses.txt"),
        new("doc/LicenseTexts.txt", "LicenseTexts.txt"),
    };

    private static readonly IReadOnlyCollection<FileToPackage> NonSteamLicenseFiles = new List<FileToPackage>
    {
        new("LICENSE.txt"),
        new("gpl.txt"),
    };

    private static readonly IReadOnlyCollection<string> SourceItemsToPackage = new List<string>
    {
        // Need a renamed git submodule file to include it in the package
        "gitmodules",
        "default_bus_layout.tres",
        "default_env.tres",
        "export_presets.cfg",
        "global.json",
        "LICENSE.txt",
        "project.godot",
        "Thrive.csproj",
        "Thrive.sln",
        "Thrive.sln.DotSettings",
        "doc",
        "shaders",
        "simulation_parameters",
        "src",
        "third_party/ThirdParty.csproj",
        "third_party/FastNoiseLite.cs",
        "third_party/StyleCop.ruleset",
        "README.md",
        "RevolutionaryGamesCommon",
    };

    /// <summary>
    ///   Base files (non-Steam, no licence) that are copied to the exported game folders
    /// </summary>
    private static readonly IReadOnlyCollection<FileToPackage> BaseFilesToPackage = new List<FileToPackage>
    {
        new("assets/misc/Thrive.desktop", "Thrive.desktop", PackagePlatform.Linux),
        new("assets/misc/thrive_logo_big.png", "Thrive.png", PackagePlatform.Linux),
    };

    private static bool checkedGodot;

    private string thriveVersion;

    private bool steamMode;

    private bool notarize;

    private IDehydrateCache? cacheForNextMetaToWrite;

    public PackageTool(Program.PackageOptions options) : base(options)
    {
        if (options.Dehydrated)
        {
            DefaultPlatforms = [PackagePlatform.Linux, PackagePlatform.Windows];
        }
        else
        {
            // For now our Mac builds kind of need to be done on a Mac, so this reflects that
            if (OperatingSystem.IsMacOS())
            {
                DefaultPlatforms = [PackagePlatform.Mac];
                notarize = !string.IsNullOrEmpty(options.MacTeamId);
            }
            else
            {
                DefaultPlatforms = ThrivePlatforms.Where(p =>
                    p != PackagePlatform.Mac && p != PackagePlatform.Web && p != PackagePlatform.Windows32).ToList();
            }
        }

        thriveVersion = AssemblyInfoReader.ReadVersionFromCsproj("Thrive.csproj", true);
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

    public static async Task<bool> CheckGodotIsAvailable(CancellationToken cancellationToken,
        string requiredVersion = GodotVersion.GODOT_VERSION)
    {
        if (checkedGodot)
            return true;

        var godot = ExecutableFinder.Which("godot");

        if (godot == null)
        {
            ExecutableFinder.PrintPathInfo(Console.Out);
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
            ColourConsole.WriteErrorLine($"Godot is available but it is the wrong version (installed) {version} != " +
                $"{requiredVersion} (required)");
            return false;
        }

        if (!result.FullOutput.Contains(MONO_IDENTIFIER))
        {
            ColourConsole.WriteErrorLine(
                "Godot is available but it doesn't seem like it is the .NET (mono) version. Check output: " +
                result.FullOutput);
            return false;
        }

        checkedGodot = true;
        return true;
    }

    protected override async Task<bool> OnBeforeStartExport(CancellationToken cancellationToken)
    {
        // Make sure Godot Editor is configured with the right native libraries as it exports them itself
        ColourConsole.WriteInfoLine("Making sure GDExtension is installed in Godot as the distributable version");

        var nativeLibraryTool = new NativeLibs(new Program.NativeLibOptions
        {
            DebugLibrary = false,
            DisableColour = options.DisableColour,
            Verbose = options.Verbose,
            PrepareGodotAPI = true,
        });

        if (!await nativeLibraryTool.InstallEditorLibrariesBeforeRelease())
        {
            ColourConsole.WriteErrorLine(
                "Failed to prepare editor libraries. Please run the native 'Fetch' tool first.");
            return false;
        }

        // By default, disable Steam mode to make the script easier to use
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

        // Apply export project settings
        await ApplyExportOnlyProjectGodotSettings(cancellationToken);

        // Make sure Thrive has been compiled as this seems to be able to cause an issue where the back button from
        // new game settings doesn't work
        ColourConsole.WriteNormalLine("Making sure Thrive C# code is compiled");

        var startInfo = new ProcessStartInfo("dotnet");
        startInfo.ArgumentList.Add("build");
        startInfo.ArgumentList.Add(SteamBuild.THRIVE_CSPROJ);

        var result = await ProcessRunHelpers.RunProcessAsync(startInfo, cancellationToken, false);

        if (result.ExitCode != 0)
        {
            ColourConsole.WriteWarningLine("Building Thrive with dotnet failed");
            return false;
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

        // By default, source code is included in non-Steam builds
        options.SourceCode ??= !steamMode;

        await CreateDynamicallyGeneratedFiles(cancellationToken);

        // Make sure godot ignores the builds folder in terms of imports
        await EnsureGodotIgnoreFileExistsInFolder(options.OutputFolder);

        if (!options.SkipGodotCheck)
        {
            if (!await CheckGodotIsAvailable(cancellationToken))
                return false;
        }

        // For CI, we need to get the branch from a special variable
        var currentBranch = Environment.GetEnvironmentVariable("CI_BRANCH");

        if (string.IsNullOrWhiteSpace(currentBranch))
        {
            currentBranch = await GitRunHelpers.GetCurrentBranch("./", cancellationToken);
        }

        await BuildInfoWriter.WriteBuildInfo(currentCommit, currentBranch, options.Dehydrated, cancellationToken);

        ColourConsole.WriteSuccessLine("Pre-build operations succeeded");
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
        // TODO: Mac steam support
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
        startInfo.ArgumentList.Add(GODOT_HEADLESS_FLAG);
        startInfo.ArgumentList.Add("--export-release");
        startInfo.ArgumentList.Add(target);
        startInfo.ArgumentList.Add(targetFile);

        var result = await ProcessRunHelpers.RunProcessAsync(startInfo, cancellationToken, false);

        if (result.ExitCode != 0)
        {
            ColourConsole.WriteWarningLine("Exporting with Godot failed");
            return false;
        }

        if (platform != PackagePlatform.Mac)
        {
            var expectedFile = Path.Join(folder, ThriveProperties.GetThriveExecutableName(platform));

            if (!File.Exists(expectedFile))
            {
                ColourConsole.WriteErrorLine(
                    $"Expected Thrive executable ({expectedFile}) was not created on export. " +
                    "Are export templates installed?");
                return false;
            }

            // Check .pck file exists
            var expectedPck = Path.Join(folder, EXPECTED_THRIVE_PCK_FILE);

            if (!File.Exists(expectedPck))
            {
                ColourConsole.WriteErrorLine($"Expected pck file ({expectedPck}) was not created on export. " +
                    "Are export templates installed?");
                return false;
            }

            var expectedDataFolder = Path.Join(folder, ThriveProperties.GetDataFolderName(platform));

            if (!Directory.Exists(expectedDataFolder))
            {
                ColourConsole.WriteErrorLine($"Expected data folder ({expectedDataFolder}) was not created on " +
                    $"export. Are export templates installed? Or did code build fail?");
                return false;
            }
        }
        else
        {
            // TODO: it would be pretty nice to create a plain .app folder if possible as we need to re-zip it
            // ourselves anyway for signing to work so one extra compression is a bit of an unnecessary step

            if (!File.Exists(MacZipInFolder(folder)))
            {
                ColourConsole.WriteErrorLine("Expected Thrive .zip for Mac was not created on export. " +
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
            // Create the Steam-specific resources

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
            var potentialCache = Path.Join(folder, IDehydrateCache.CacheFileName);

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
            PrepareGodotAPI = true,

            // Only the ThriveNative library is needed for manual copy (extension is copied by Godot)
            Libraries = [NativeConstants.Library.ThriveNative],
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

        if (platform == PackagePlatform.Mac)
        {
            ColourConsole.WriteNormalLine("Fixing Mac GDExtension by ourselves copying it into the .zip");

            if (!await CopyGdExtensionToZip(folder, cancellationToken))
                return false;

            ColourConsole.WriteNormalLine("Copying native libraries into Thrive Mac .zip");
            if (!await nativeLibraryTool.MoveInstalledLibrariesToZip(folder, MacZipInFolder(folder),
                    MAC_ZIP_LIBRARIES_PATH, cancellationToken))
            {
                ColourConsole.WriteErrorLine("Failed to move native libraries into zip");
                return false;
            }

            // Make sure there isn't a confusing Thrive.app folder remaining
            var unwantedApp = Path.Join(folder, "Thrive.app");

            if (Directory.Exists(unwantedApp))
                Directory.Delete(unwantedApp, true);
        }

        ColourConsole.WriteSuccessLine("Native library operations succeeded");

        return true;
    }

    protected override async Task<bool> CompressSourceCode(CancellationToken cancellationToken)
    {
        // Prepare git modules before compressing (see the comment on the file list why this is like this)
        File.Copy(".gitmodules", "gitmodules", true);

        var result = await base.CompressSourceCode(cancellationToken);

        // Remove the copied file to not have it hang around
        File.Delete("gitmodules");

        return result;
    }

    protected override async Task<bool> Compress(PackagePlatform platform, string folder, string archiveFile,
        CancellationToken cancellationToken)
    {
        if (platform == PackagePlatform.Mac)
        {
            ColourConsole.WriteInfoLine("Mac target is already zipped, will create a signed zip");

            if (File.Exists(archiveFile))
                File.Delete(archiveFile);

            if (!await CreateAndSignMacZip(folder, archiveFile, cancellationToken))
                return false;

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
                thriveVersion, ThriveProperties.GodotTargetFromPlatform(platform, steamMode), target,
                cancellationToken);

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

        await CleanUpExportOnlyProjectSettings(cancellationToken);

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

    /// <summary>
    ///   The Godot engine export for some reason doesn't put the extension to the right place on Mac, so we do that
    ///   ourselves.
    /// </summary>
    /// <param name="folder">
    ///   The prepared Mac release folder that needs to already contain the Thrive.zip
    /// </param>
    /// <param name="cancellationToken">Cancellation</param>
    /// <returns>True on success</returns>
    private async Task<bool> CopyGdExtensionToZip(string folder, CancellationToken cancellationToken)
    {
        var targetPath = Path.Join(folder, MAC_ZIP_GD_EXTENSION_TARGET_PATH);

        if (Directory.Exists(targetPath))
            Directory.Delete(targetPath, true);

        Directory.CreateDirectory(targetPath);

        var name = NativeConstants.GetLibraryDllName(NativeConstants.Library.ThriveExtension, PackagePlatform.Mac,
            PrecompiledTag.WithoutAvx);

        // As the normal library install doesn't uselessly copy the extension file, we copy it here from the real
        // storage location
        File.Copy(NativeConstants.GetPathToLibraryDll(NativeConstants.Library.ThriveExtension, PackagePlatform.Mac,
                NativeConstants.ExtensionVersion.ToString(), true, PrecompiledTag.WithoutAvx),
            Path.Join(targetPath, name));

        var startInfo = new ProcessStartInfo("zip")
        {
            WorkingDirectory = folder,
        };
        startInfo.ArgumentList.Add("-9");
        startInfo.ArgumentList.Add("-u");
        startInfo.ArgumentList.Add(Path.GetFileName(MacZipInFolder(folder)));

        // Relative path to working directory to ensure correct copying
        startInfo.ArgumentList.Add(Path.Join(MAC_ZIP_GD_EXTENSION_TARGET_PATH, name));

        var result = await ProcessRunHelpers.RunProcessAsync(startInfo, cancellationToken, true);

        // Delete the temporary folder as the data either failed or is in the zip now
        Directory.Delete(targetPath, true);

        if (result.ExitCode != 0)
        {
            ColourConsole.WriteErrorLine($"Running zip update for GDExtension include failed " +
                $"(exit: {result.ExitCode}): {result.FullOutput}");
            return false;
        }

        return true;
    }

    /// <summary>
    ///   Unzips the Godot-created zip, signs everything and then re-creates a new zip at the target location. This is
    ///   required as macOS otherwise detects the game as a broken app (due to signatures).
    /// </summary>
    /// <returns>True on success</returns>
    private async Task<bool> CreateAndSignMacZip(string folder, string archiveFile,
        CancellationToken cancellationToken)
    {
        var sourceZip = MacZipInFolder(folder);

        var parentFolder = Path.GetDirectoryName(archiveFile) ??
            throw new Exception("Unknown folder to create archive in");

        var tempFolder = Path.Combine(parentFolder, "mac_temp_uncompressed");

        if (Directory.Exists(tempFolder))
            Directory.Delete(tempFolder, true);

        Directory.CreateDirectory(tempFolder);

        ColourConsole.WriteDebugLine($"Using C# zip library to extract to temporary folder: {tempFolder}");
        using var archiveZip = new ZipArchive(File.OpenRead(sourceZip), ZipArchiveMode.Read, false);
        archiveZip.ExtractToDirectory(tempFolder);

        ColourConsole.WriteInfoLine("Performing final signing for Mac build");

        if (string.IsNullOrEmpty(options.MacSigningKey))
        {
            ColourConsole.WriteWarningLine(
                "Signing without a specific key for mac (this should work but in an optimal case " +
                "a signing key would be set)");

            if (Debugger.IsAttached)
                Debugger.Break();
        }
        else
        {
            ColourConsole.WriteInfoLine($"Signing mac build with key {options.MacSigningKey}");
        }

        ColourConsole.WriteInfoLine("Signing all parts of the Mac build");
        ColourConsole.WriteNormalLine("This may take a while as there are many items");

        foreach (var item in Directory.EnumerateFiles(tempFolder, "*.*", SearchOption.AllDirectories))
        {
            // Skip stuff that shouldn't be signed
            // TODO: would it offer any extra security if the .pck file was signed as well?
            if (item.EndsWith(".txt") || item.EndsWith(".pck") || item.EndsWith(".md") || item.EndsWith(".7z"))
            {
                continue;
            }

            // The main executable must be signed last
            if (item.EndsWith(MAC_MAIN_EXECUTABLE))
                continue;

            if (!await BinaryHelpers.SignFileForMac(item, MAC_ENTITLEMENTS, options.MacSigningKey,
                    cancellationToken))
            {
                ColourConsole.WriteErrorLine($"Failed to sign part of Mac build: {item}");
                return false;
            }
        }

        ColourConsole.WriteSuccessLine("Successfully signed individual parts");

        // Sign the main file last
        if (!await BinaryHelpers.SignFileForMac(Path.Join(tempFolder, MAC_MAIN_EXECUTABLE), MAC_ENTITLEMENTS,
                options.MacSigningKey,
                cancellationToken))
        {
            ColourConsole.WriteErrorLine("Failed to sign main of Mac build");
            return false;
        }

        ColourConsole.WriteSuccessLine("Signed the main file");

        ColourConsole.WriteInfoLine("Creating final archive file from signed items");
        var startInfo = new ProcessStartInfo("zip")
        {
            WorkingDirectory = tempFolder,
        };

        if (notarize)
        {
            ColourConsole.WriteNormalLine("Will notarize the result, so intermediate compression is used for now");
            startInfo.ArgumentList.Add("-6");
        }
        else
        {
            startInfo.ArgumentList.Add("-9");
        }

        startInfo.ArgumentList.Add("-r");

        startInfo.ArgumentList.Add(Path.GetFullPath(archiveFile));

        foreach (var item in Directory.EnumerateFileSystemEntries(tempFolder))
        {
            // Need to remove the prefix to have items in a relative path to the temp folder for the zip process
            // to work
            startInfo.ArgumentList.Add(item.Substring(tempFolder.Length + 1));
        }

        var result = await ProcessRunHelpers.RunProcessAsync(startInfo, cancellationToken, true);

        // Delete the temp folder
        Directory.Delete(tempFolder, true);

        if (result.ExitCode != 0)
        {
            ColourConsole.WriteErrorLine("Running final zip create failed " +
                $"(exit: {result.ExitCode}): {result.FullOutput}");
            return false;
        }

        // Notarization has to extract and re-create the zip, so this is before the final signature
        if (notarize)
        {
            ColourConsole.WriteInfoLine("Notarizing Mac build");

            if (string.IsNullOrEmpty(options.MacTeamId) || string.IsNullOrEmpty(options.AppleId) ||
                string.IsNullOrEmpty(options.AppleAppPassword))
            {
                ColourConsole.WriteErrorLine("Notarizing Mac build requires Apple developer credentials and team id");
                return false;
            }

            if (!await BinaryHelpers.NotarizeFile(archiveFile, options.MacTeamId,
                    options.AppleId, options.AppleAppPassword, cancellationToken))
            {
                ColourConsole.WriteErrorLine("Failed to notarize Mac build (.app)");
                return false;
            }
        }
        else
        {
            ColourConsole.WriteErrorLine("Not notarizing App. Macs will not really want to run the result!");
        }

        ColourConsole.WriteInfoLine("Signing final zip");

        if (!await BinaryHelpers.SignFileForMac(archiveFile, MAC_ENTITLEMENTS, options.MacSigningKey,
                cancellationToken))
        {
            ColourConsole.WriteErrorLine("Failed to sign Mac build");
            return false;
        }

        ColourConsole.WriteSuccessLine("Signed Mac zip created");
        return true;
    }

    private async Task<bool> PrepareDehydratedFolder(PackagePlatform platform, string folder,
        CancellationToken cancellationToken)
    {
        _ = platform;

        Directory.CreateDirectory(Dehydration.DEVBUILDS_FOLDER);
        Directory.CreateDirectory(Dehydration.DEHYDRATE_CACHE);

        var pck = Path.Join(folder, "Thrive.pck");

        var extractFolder = Path.Join(options.OutputFolder, "temp_extracted", Path.GetFileName(folder));

        var pckCache = new DehydrateCacheV2(extractFolder);

        await Dehydration.DehydrateThrivePck(pck, extractFolder, pckCache, cancellationToken);

        var normalCache = new DehydrateCacheV2(folder);

        cancellationToken.ThrowIfCancellationRequested();

        ColourConsole.WriteNormalLine(".pck dehydration complete, dehydrating other files");

        // Dehydrate other files
        foreach (var file in Directory.EnumerateFiles(folder, "*.*", SearchOption.AllDirectories))
        {
            // Always ignore some files despite their sizes
            var fileWithoutPath = file.Replace($"{folder}/", string.Empty);
            if (DehydrateIgnoreFiles.Any(i => fileWithoutPath.EndsWith(i)))
            {
                if (ColourConsole.DebugPrintingEnabled)
                    ColourConsole.WriteDebugLine($"Ignoring file in dehydration: {file}");

                continue;
            }

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

        var submoduleLines = await GitRunHelpers.SubmoduleStatusInfo("./", cancellationToken);

        foreach (var submoduleLine in submoduleLines)
        {
            await revision.WriteLineAsync(submoduleLine);
        }

        await revision.WriteLineAsync(string.Empty);
        await revision.WriteLineAsync("Submodules used by native libraries may be newer than what precompiled files " +
            "were used. Please cross reference the reported native library version with Thrive repository to see " +
            "exact used submodule version");
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

    private async Task ApplyExportOnlyProjectGodotSettings(CancellationToken cancellationToken)
    {
        var content = Encoding.UTF8.GetString(await File.ReadAllBytesAsync(GODOT_PROJECT_FILE, cancellationToken));

        // Skip if already present
        if (ClearColourOptionRegex.IsMatch(content))
        {
            ColourConsole.WriteInfoLine("Clear colour option already present in Godot project, skipping");
            return;
        }

        var index = content.LastIndexOf("[rendering]", StringComparison.InvariantCulture);

        if (index < 0)
            throw new Exception("Could not find '[rendering]' section in Godot project file");

        index += "[rendering]".Length;

        var lineEnding = "\n";

        if (content.Contains("\r\n"))
            lineEnding = "\r\n";

        // Find the start of the data to insert our customisation at
        while (char.IsWhiteSpace(content[index]) && index + 1 < content.Length)
        {
            ++index;
        }

        var newContent =
            content.Insert(index, "environment/defaults/default_clear_color=Color(0, 0, 0, 1)" + lineEnding);

        if (newContent != content)
        {
            await File.WriteAllBytesAsync(GODOT_PROJECT_FILE, Encoding.UTF8.GetBytes(newContent), cancellationToken);
            ColourConsole.WriteNormalLine("Added special export-only clear colour setting to the Godot project file");
        }
    }

    private async Task CleanUpExportOnlyProjectSettings(CancellationToken cancellationToken)
    {
        var content = Encoding.UTF8.GetString(await File.ReadAllBytesAsync(GODOT_PROJECT_FILE, cancellationToken));

        var newContent = ClearColourOptionRegex.Replace(content, "");

        if (newContent != content)
        {
            await File.WriteAllBytesAsync(GODOT_PROJECT_FILE, Encoding.UTF8.GetBytes(newContent), cancellationToken);
            ColourConsole.WriteNormalLine("Removed special export-only settings from the Godot project file");
        }
    }
}
