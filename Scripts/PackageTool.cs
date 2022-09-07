namespace Scripts;

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using ScriptsBase.Models;
using ScriptsBase.ToolBases;
using ScriptsBase.Utilities;
using SharedBase.Utilities;

public class PackageTool : PackageToolBase<Program.PackageOptions>
{
    private const string EXPECTED_THRIVE_DATA_FOLDER = "data_Thrive";

    private const string STEAM_BUILD_MESSAGE = "This is the Steam build. This can only be distributed by " +
        "Revolutionary Games Studio (under a special license) due to Steam being incompatible with the GPL license!";

    private static readonly Regex GodotVersionRegex = new(@"([\d\.]+)\..*mono");

    private static readonly IReadOnlyList<PackagePlatform> ThrivePlatforms = new List<PackagePlatform>
    {
        PackagePlatform.Linux,
        PackagePlatform.Windows,
        PackagePlatform.Windows32,
        PackagePlatform.Mac,
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
        "gpl.txt",
        "LICENSE.txt",
        "README.txt",
    };

    private static readonly IReadOnlyCollection<FileToPackage> LicenseFiles =
        new List<FileToPackage>
        {
            new("LICENSE.txt"),
            new("gpl.txt"),
            new("assets/LICENSE.txt", "ThriveAssetsLICENSE.txt"),
            new("assets/README.txt", "ThriveAssetsREADME.txt"),
            new("doc/GodotLicense.txt", "GodotLicense.txt"),
        };

    private static readonly IReadOnlyCollection<string> SourceItemsToPackage =
        new List<string>
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

    private readonly string thriveVersion;
    private string dehydratedVersion;

    private bool checkedGodot;
    private bool steamMode;

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
                DefaultPlatforms = ThrivePlatforms.Where(p => p != PackagePlatform.Mac).ToList();
            }
        }

        thriveVersion = AssemblyInfoReader.ReadVersionFromAssemblyInfo(true);
        dehydratedVersion = thriveVersion;
    }

    protected override IReadOnlyCollection<PackagePlatform> ValidPlatforms => ThrivePlatforms;

    protected override IEnumerable<PackagePlatform> DefaultPlatforms { get; }

    protected override IEnumerable<string> SourceFilesToPackage => SourceItemsToPackage;

    private string ReadmeFile => Path.Join(options.OutputFolder, "README.txt");
    private string RevisionFile => Path.Join(options.OutputFolder, "revision.txt");

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

        if (options.Dehydrated)
        {
            if (steamMode)
            {
                ColourConsole.WriteErrorLine("Dehydrate option conflicts with Steam mode");
                return false;
            }

            ColourConsole.WriteNormalLine("Making dehydrated devbuilds");

            dehydratedVersion = await GitRunHelpers.GetCurrentCommit("./", cancellationToken);
        }

        if (steamMode)
        {
            options.CompressRaw = false;
        }

        // By default source code is included in non-Steam builds
        options.SourceCode ??= !steamMode;

        await CreateDynamicallyGeneratedFiles(cancellationToken);

        // Make sure godot ignores the builds folder in terms of imports
        var ignoreFile = Path.Join(options.OutputFolder, ".gdignore");

        if (!File.Exists(ignoreFile))
        {
            await using var writer = File.Create(ignoreFile);
        }

        if (!await CheckGodotIsAvailable(cancellationToken))
            return false;

        return true;
    }

    protected override string GetFolderNameForExport(PackagePlatform platform)
    {
        string suffix = string.Empty;

        if (steamMode)
        {
            suffix = "_steam";
        }

        string platformName;

        switch (platform)
        {
            case PackagePlatform.Linux:
                platformName = "linux_x11";
                break;
            case PackagePlatform.Windows:
                platformName = "windows_desktop";
                break;
            case PackagePlatform.Windows32:
                platformName = "windows_desktop_(32-bit)";
                break;
            case PackagePlatform.Mac:
                platformName = "mac_osx";
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(platform), platform, null);
        }

        return $"Thrive_{thriveVersion}_{platformName}{suffix}";
    }

    protected override async Task<bool> PrepareToExport(PackagePlatform platform, CancellationToken cancellationToken)
    {
        if (options.Steam != null)
        {
            if (!await SteamBuild.SetBuildMode(options.Steam.Value, true, cancellationToken,
                    SteamBuild.ConvertPackagePlatformToSteam(platform)))
            {
                ColourConsole.WriteErrorLine("Failed to set wanted Steam mode");
                return false;
            }
        }

        PrintSteamModeWarning();
        return true;
    }

    protected override async Task<bool> Export(PackagePlatform platform, string folder,
        CancellationToken cancellationToken)
    {
        var target = GodotTargetFromPlatform(platform);

        ColourConsole.WriteInfoLine($"Starting export for target: {target}");

        Directory.CreateDirectory(folder);

        ColourConsole.WriteNormalLine($"Exporting to folder: {folder}");

        var targetFile = Path.Join(folder, "Thrive" + GodotTargetExtension(platform));

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

        if (platform != PackagePlatform.Mac)
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
            startInfo.ArgumentList.Add(MacZipInFolder(folder));

            foreach (var file in GetFilesToPackage())
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
    }

    private static string MacZipInFolder(string folder)
    {
        return Path.Join(folder, "Thrive.zip");
    }

    private async Task<bool> PrepareDehydratedFolder(PackagePlatform platform, string folder,
        CancellationToken cancellationToken)
    {
        throw new NotImplementedException();

        // TODO:

        /*FileUtils.mkdir_p DEVBUILDS_FOLDER
        FileUtils.mkdir_p DEHYDRATE_CACHE
        FileUtils.mkdir_p 'builds/temp_extracted'

        extract_folder = "builds/temp_extracted/#{target_name}"

        FileUtils.mkdir_p extract_folder

        pck = File.join(target_folder, 'Thrive.pck')

        # Start by extracting the big files to be dehydrated, but ignore
        # a list of well compressing / often changing files
        if runSystemSafe(pck_tool, '--action', 'extract', pck,
                         '-o', extract_folder, '-q', '--min-size-filter',
                         DEHYDRATE_FILE_SIZE_THRESSHOLD.to_s,
                         '--exclude-regex-filter', DEHYDRATE_IGNORE_FILE_TYPES) != 0
          onError 'Failed to run extract. Do you have the right godotpcktool version?'
        end

        # And remove them from the .pck
        if runSystemSafe(pck_tool, '--action', 'repack', File.join(target_folder, 'Thrive.pck'),
                         '--max-size-filter', (DEHYDRATE_FILE_SIZE_THRESSHOLD - 1).to_s,
                         '--include-override-filter', DEHYDRATE_IGNORE_FILE_TYPES, '-q') != 0
          onError 'Failed to run repack'
        end

        pck_cache = DehydrateCache.new extract_folder

        # Dehydrate always all the unextracted files
        Dir.glob(File.join(extract_folder, '*#1#*'), File::FNM_DOTMATCH) do |file|
          raise "found file doesn't exist" unless File.exist? file
          next if File.directory? file

          dehydrate_file file, pck_cache
        end

        # No longer need the temp files
        FileUtils.rm_rf extract_folder, secure: true

        normal_cache = DehydrateCache.new target_folder

        # Dehydrate other files
        Dir.glob(File.join(target_folder, '*#1#*'), File::FNM_DOTMATCH) do |file|
          raise "found file doesn't exist" unless File.exist? file
          next if File.directory? file

          # Always ignore some files despite their sizes
          next if DEHYDRATE_IGNORE_FILES.include? file.sub("#{target_folder}/", '')

          check_dehydrate_file file, normal_cache
        end

        normal_cache.add_pck pck, pck_cache

        # Write the cache
        File.write(File.join(target_folder, 'dehydrated.json'), normal_cache.to_json)

        # Then do a normal zip after the contents have been adjusted
        final_file = zip_package target, target_name, target_folder, target_file

        # And move it to the devbuilds folder for the upload script
        FileUtils.mv final_file, DEVBUILDS_FOLDER

        # Write meta file needed for upload
        File.write(File.join(DEVBUILDS_FOLDER, "#{File.basename(final_file)}.meta.json"),
                   { dehydrated: { objects: normal_cache.hashes },
                     branch: git_branch,
                     platform: target,
                     version: THRIVE_VERSION }.to_json)

        message = "Created devbuild: #{File.join(DEVBUILDS_FOLDER, final_file)}"
        puts message
        @reprint_messages.append '', message*/
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

        if (result.ExitCode != 0)
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

    private string GodotTargetFromPlatform(PackagePlatform platform)
    {
        if (steamMode)
        {
            switch (platform)
            {
                case PackagePlatform.Linux:
                    return "Linux/X11_steam";
                case PackagePlatform.Windows:
                    return "Windows Desktop_steam";
                default:
                    throw new ArgumentOutOfRangeException(nameof(platform), platform, null);
            }
        }

        switch (platform)
        {
            case PackagePlatform.Linux:
                return "Linux/X11";
            case PackagePlatform.Windows:
                return "Windows Desktop";
            case PackagePlatform.Windows32:
                return "Windows Desktop (32-bit)";
            case PackagePlatform.Mac:
                return "Mac OSX";
            default:
                throw new ArgumentOutOfRangeException(nameof(platform), platform, null);
        }
    }

    private string GodotTargetExtension(PackagePlatform platform)
    {
        switch (platform)
        {
            case PackagePlatform.Linux:
                return "";
            case PackagePlatform.Windows32:
            case PackagePlatform.Windows:
                return ".exe";
            case PackagePlatform.Mac:
                return ".zip";
            default:
                throw new ArgumentOutOfRangeException(nameof(platform), platform, null);
        }
    }

    private async Task CreateDynamicallyGeneratedFiles(CancellationToken cancellationToken)
    {
        await using var readme = File.CreateText(ReadmeFile);

        if (steamMode)
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
    }
}
