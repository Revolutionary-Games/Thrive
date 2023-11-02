using System;
using System.Collections.Generic;
using System.Diagnostics;
using CommandLine;
using CommandLine.Text;
using Scripts;
using ScriptsBase.Models;
using ScriptsBase.Utilities;
using SharedBase.Models;
using SharedBase.Utilities;

public class Program
{
    [STAThread]
    public static int Main(string[] args)
    {
        RunFolderChecker.EnsureRightRunningFolder("Thrive.sln");

        var result = CommandLineHelpers.CreateParser()
            .ParseArguments<CheckOptions, NativeLibOptions, TestOptions, ChangesOptions, LocalizationOptions,
                CleanupOptions,
                PackageOptions, UploadOptions, ContainerOptions, SteamOptions, GodotTemplateOptions,
                TranslationProgressOptions, CreditsOptions, WikiOptions, GeneratorOptions,
                GodotProjectValidMakerOptions>(args)
            .MapResult(
                (CheckOptions options) => RunChecks(options),
                (NativeLibOptions options) => RunNativeLibsTool(options),
                (TestOptions options) => RunTests(options),
                (ChangesOptions options) => RunChangesFinding(options),
                (LocalizationOptions options) => RunLocalization(options),
                (CleanupOptions options) => RunCleanup(options),
                (PackageOptions options) => RunPackage(options),
                (UploadOptions options) => RunUpload(options),
                (ContainerOptions options) => RunContainer(options),
                (SteamOptions options) => SetSteamOptions(options),
                (GodotTemplateOptions options) => RunTemplateInstall(options),
                (TranslationProgressOptions options) => RunTranslationProgress(options),
                (CreditsOptions options) => RunCreditsUpdate(options),
                (WikiOptions options) => RunWikiUpdate(options),
                (GeneratorOptions options) => RunFileGenerator(options),
                (GodotProjectValidMakerOptions options) => RunProjectValidMaker(options),
                CommandLineHelpers.PrintCommandLineErrors);

        ConsoleHelpers.CleanConsoleStateForExit();

        return result;
    }

    private static int RunChecks(CheckOptions options)
    {
        CommandLineHelpers.HandleDefaultOptions(options);

        ColourConsole.WriteDebugLine("Running in check mode");
        ColourConsole.WriteDebugLine($"Manually specified checks: {string.Join(' ', options.Checks)}");

        var checker = new CodeChecks(options, (localizationOptions, cancellationToken) =>
        {
            var updater = new LocalizationUpdate(localizationOptions);

            return updater.Run(cancellationToken);
        });

        return checker.Run().Result;
    }

    private static int RunNativeLibsTool(NativeLibOptions options)
    {
        CommandLineHelpers.HandleDefaultOptions(options);

        ColourConsole.WriteDebugLine("Running native library handling tool");

        var tokenSource = ConsoleHelpers.CreateSimpleConsoleCancellationSource();

        var tool = new NativeLibs(options);

        return tool.Run(tokenSource.Token).Result ? 0 : 1;
    }

    private static int RunTests(TestOptions options)
    {
        CommandLineHelpers.HandleDefaultOptions(options);

        ColourConsole.WriteDebugLine("Running dotnet tests");

        // TODO: we should maybe think about writing some tests runnable through dotnet
        ColourConsole.WriteWarningLine("Thrive doesn't currently have any implemented dotnet test compatible tests");

        var tokenSource = ConsoleHelpers.CreateSimpleConsoleCancellationSource();

        return ProcessRunHelpers.RunProcessAsync(new ProcessStartInfo("dotnet", "test"), tokenSource.Token, false)
            .Result.ExitCode;
    }

    private static int RunChangesFinding(ChangesOptions options)
    {
        CommandLineHelpers.HandleDefaultOptions(options);

        ColourConsole.WriteDebugLine("Running changes finding tool");

        return OnlyChangedFileDetector.BuildListOfChangedFiles(options).Result ? 0 : 1;
    }

    private static int RunPackage(PackageOptions options)
    {
        CommandLineHelpers.HandleDefaultOptions(options);

        ColourConsole.WriteDebugLine("Running packaging tool");

        var tokenSource = ConsoleHelpers.CreateSimpleConsoleCancellationSource();

        var packager = new PackageTool(options);

        return packager.Run(tokenSource.Token).Result ? 0 : 1;
    }

    private static int RunLocalization(LocalizationOptions options)
    {
        CommandLineHelpers.HandleDefaultOptions(options);

        ColourConsole.WriteDebugLine("Running localization update tool");

        var tokenSource = ConsoleHelpers.CreateSimpleConsoleCancellationSource();

        var updater = new LocalizationUpdate(options);

        return updater.Run(tokenSource.Token).Result ? 0 : 1;
    }

    private static int RunCleanup(CleanupOptions options)
    {
        CommandLineHelpers.HandleDefaultOptions(options);

        ColourConsole.WriteDebugLine("Running cleanup tool");

        var tokenSource = ConsoleHelpers.CreateSimpleConsoleCancellationSource();

        return Cleanup.Run(options, tokenSource.Token).Result ? 0 : 1;
    }

    private static int RunUpload(UploadOptions options)
    {
        CommandLineHelpers.HandleDefaultOptions(options);

        ColourConsole.WriteDebugLine("Running upload tool");

        var tokenSource = ConsoleHelpers.CreateSimpleConsoleCancellationSource();

        var uploader = new Uploader(options);

        return uploader.Run(tokenSource.Token).Result ? 0 : 1;
    }

    private static int RunContainer(ContainerOptions options)
    {
        CommandLineHelpers.HandleDefaultOptions(options);

        ColourConsole.WriteDebugLine("Running container tool");

        var tokenSource = ConsoleHelpers.CreateSimpleConsoleCancellationSource();

        var tool = new ContainerTool(options);

        return tool.Run(tokenSource.Token).Result ? 0 : 1;
    }

    private static int SetSteamOptions(SteamOptions options)
    {
        CommandLineHelpers.HandleDefaultOptions(options);

        ColourConsole.WriteDebugLine("Running container tool");

        var tokenSource = ConsoleHelpers.CreateSimpleConsoleCancellationSource();

        bool wantedMode;

        switch (options.Mode.ToLowerInvariant())
        {
            case "disable":
            case "disabled":
                wantedMode = false;
                break;
            case "enable":
            case "enabled":
                wantedMode = true;
                break;
            default:
                ColourConsole.WriteErrorLine("Invalid mode. Valid values are 'disable' and 'enable'");
                return 1;
        }

        if (wantedMode == SteamBuild.IsSteamBuildEnabled(tokenSource.Token).Result)
        {
            ColourConsole.WriteInfoLine("Already in desired mode");
            return 0;
        }

        if (!SteamBuild.SetBuildMode(wantedMode, true, tokenSource.Token).Result)
        {
            ColourConsole.WriteErrorLine("Failed to update Steam build mode");
            return 2;
        }

        ColourConsole.WriteSuccessLine("Steam build mode changed");
        return 0;
    }

    private static int RunTemplateInstall(GodotTemplateOptions options)
    {
        CommandLineHelpers.HandleDefaultOptions(options);

        ColourConsole.WriteDebugLine("Running Godot templates tool");

        var tokenSource = ConsoleHelpers.CreateSimpleConsoleCancellationSource();

        return GodotTemplateInstaller.Run(tokenSource.Token).Result ? 0 : 1;
    }

    private static int RunTranslationProgress(TranslationProgressOptions options)
    {
        CommandLineHelpers.HandleDefaultOptions(options);

        ColourConsole.WriteDebugLine("Running translation progress update tool");

        var tokenSource = ConsoleHelpers.CreateSimpleConsoleCancellationSource();

        return TranslationProgressTool.Run(tokenSource.Token).Result ? 0 : 1;
    }

    private static int RunCreditsUpdate(CreditsOptions options)
    {
        CommandLineHelpers.HandleDefaultOptions(options);

        ColourConsole.WriteDebugLine("Running credit updating tool");

        var tokenSource = ConsoleHelpers.CreateSimpleConsoleCancellationSource();

        return CreditsUpdater.Run(tokenSource.Token).Result ? 0 : 1;
    }

    private static int RunWikiUpdate(WikiOptions options)
    {
        CommandLineHelpers.HandleDefaultOptions(options);

        ColourConsole.WriteDebugLine("Running wiki updating tool");

        var tokenSource = ConsoleHelpers.CreateSimpleConsoleCancellationSource();

        return WikiUpdater.Run(tokenSource.Token).Result ? 0 : 1;
    }

    private static int RunFileGenerator(GeneratorOptions options)
    {
        CommandLineHelpers.HandleDefaultOptions(options);

        ColourConsole.WriteDebugLine("Running file generating tool");

        var tokenSource = ConsoleHelpers.CreateSimpleConsoleCancellationSource();

        var tool = new FileGenerator(options);

        return tool.Run(tokenSource.Token).Result;
    }

    private static int RunProjectValidMaker(GodotProjectValidMakerOptions options)
    {
        CommandLineHelpers.HandleDefaultOptions(options);

        ColourConsole.WriteInfoLine("Attempting to make Thrive Godot project valid for C# compile...");

        var tokenSource = ConsoleHelpers.CreateSimpleConsoleCancellationSource();

        var tool = new GodotProjectValidMaker(options);

        return tool.Run(tokenSource.Token).Result;
    }

    public class CheckOptions : CheckOptionsBase
    {
    }

    [Verb("native", HelpText = "Handling for native libraries needed by Thrive")]
    public class NativeLibOptions : ScriptOptionsBase
    {
        public enum OperationMode
        {
            /// <summary>
            ///   Check if libraries are present
            /// </summary>
            Check,

            /// <summary>
            ///   Check if libraries are present for distribution
            /// </summary>
            CheckDistributable,

            /// <summary>
            ///   Installs a library to work with Godot editor
            /// </summary>
            Install,

            /// <summary>
            ///   Downloads required libraries (if available)
            /// </summary>
            Fetch,

            /// <summary>
            ///   Build a locally working version with native tools
            /// </summary>
            Build,

            // TODO: add a command to clean old library versions

            /// <summary>
            ///   Build libraries for distribution or uploading using podman
            /// </summary>
            Package,

            /// <summary>
            ///   Upload packaged libraries missing from the server
            /// </summary>
            Upload,
        }

        [Usage(ApplicationAlias = "dotnet run --project Scripts --")]
        public static IEnumerable<Example> Examples
        {
            get
            {
                yield return new Example("download all available libraries",
                    new NativeLibOptions { Operations = new[] { OperationMode.Fetch } });
                yield return new Example("install library locally to make Godot Editor debugging work",
                    new NativeLibOptions { Operations = new[] { OperationMode.Install } });
                yield return new Example("compile libraries locally",
                    new NativeLibOptions { Operations = new[] { OperationMode.Build } });
                yield return new Example("prepare library versions for distribution or uploading with podman",
                    new NativeLibOptions { Operations = new[] { OperationMode.Package } });
            }
        }

        [Value(0, MetaName = "OPERATIONS", Required = false, HelpText = "What native operation(s) to do")]
        public IList<OperationMode> Operations { get; set; } = new List<OperationMode> { OperationMode.Check };

        [Option('l', "library", Required = false, Default = null, MetaValue = "LIBRARIES",
            HelpText = "Libraries to work on, default is all.")]
        public IList<NativeLibs.Library>? Libraries { get; set; } = new List<NativeLibs.Library>();

        [Option('d', "debug", Required = false, Default = false,
            HelpText = "Set to work on debug versions of the libraries")]
        public bool DebugLibrary { get; set; }

        [Option('p', "platform", Required = false, Default = null,
            HelpText = "Use to override detected platforms for selected operation")]
        public IList<PackagePlatform>? Platforms { get; set; } = new List<PackagePlatform>();

        [Option('c', "compiler", Required = false, Default = null, MetaValue = "COMPILER",
            HelpText = "Manually specify compiler to use")]
        public string? Compiler { get; set; }

        [Option("c-compiler", Required = false, Default = null, MetaValue = "COMPILER",
            HelpText = "Manually specify C compiler to use")]
        public string? CCompiler { get; set; }

        [Option('g', "generator", Required = false, Default = null, MetaValue = "GENERATOR",
            HelpText = "Manually specify which CMake generator to use")]
        public string? CmakeGenerator { get; set; }

        [Option('s', "symbolic-links", Required = false, Default = false,
            HelpText = "If specified prefer to use symlinks even on Windows")]
        public bool UseSymlinks { get; set; }
    }

    [Verb("test", HelpText = "Run tests using 'dotnet' command")]
    public class TestOptions : ScriptOptionsBase
    {
    }

    public class ChangesOptions : ChangesOptionsBase
    {
        [Option('b', "branch", Required = false, Default = "master", HelpText = "The git remote branch name")]
        public override string RemoteBranch { get; set; } = "master";
    }

    public class LocalizationOptions : LocalizationOptionsBase
    {
    }

    [Verb("cleanup", HelpText = "Cleanup Godot temporary files. WARNING: will lose uncommitted changes")]
    public class CleanupOptions : ScriptOptionsBase
    {
        [Option('r', "reset", Required = false, Default = true,
            HelpText = "Run git reset --hard after cleaning folders. Set to false to keep uncommitted changes.")]
        public bool? GitReset { get; set; }
    }

    public class PackageOptions : PackageOptionsBase
    {
        [Option('s', "steam", Required = false, Default = null,
            HelpText =
                "Use to set Thrive to either build or not build in Steam mode. If unset, preserves current mode.")]
        public bool? Steam { get; set; }

        [Option('z', "compress", Default = true,
            HelpText = "Control whether the packages are compressed or left as folders")]
        public bool? CompressRaw { get; set; }

        [Option('d', "dehydrated", Default = false,
            HelpText = "Make dehydrated builds by separating out big files. For use with DevBuilds")]
        public bool Dehydrated { get; set; }

        [Option("fallback-native-local-only", Default = false,
            HelpText = "Fallback to using native library only meant for local play (not recommended for release)")]
        public bool FallbackToLocalNative { get; set; }

        public override bool Compress => CompressRaw == true;
    }

    [Verb("upload", HelpText = "Upload created devbuilds to ThriveDevCenter")]
    public class UploadOptions : ScriptOptionsBase
    {
        [Option('k', "key", Required = false, Default = null, MetaValue = "KEY",
            HelpText = "Set to a valid DevCenter key (not user token) to use non-anonymous uploading.")]
        public string? Key { get; set; }

        [Option('k', "key", Required = false, Default = Uploader.DEFAULT_DEVCENTER_URL, MetaValue = "DEVCENTER_URL",
            HelpText = "DevCenter URL to upload to.")]
        public string Url { get; set; } = Uploader.DEFAULT_DEVCENTER_URL;

        [Option('r', "retries", Required = false, Default = 3, MetaValue = "COUNT",
            HelpText = "How many upload retries to do to avoid spurious failures")]
        public int Retries { get; set; }

        [Option('p', "parallel", Required = false, Default = Uploader.DEFAULT_PARALLEL_UPLOADS, MetaValue = "COUNT",
            HelpText = "How many parallel uploads to do")]
        public int ParallelUploads { get; set; }

        [Option("delete-after-upload", Default = true,
            HelpText = "If specified dehydrated builds are deleted after upload (or server not wanting them)")]
        public bool? DeleteAfterUpload { get; set; }
    }

    public class ContainerOptions : ContainerOptionsBase
    {
        [Option('i', "image", Default = ImageType.CI, HelpText = "The image to build")]
        public ImageType Image { get; set; } = ImageType.CI;
    }

    [Verb("steam", HelpText = "Control Steam build variant building")]
    public class SteamOptions : ScriptOptionsBase
    {
        [Value(0, MetaName = "Mode", Required = true, HelpText = "Which mode to set Thrive to")]
        public string Mode { get; set; } = string.Empty;
    }

    [Verb("godot-templates", HelpText = "Tool to automatically install Godot templates")]
    public class GodotTemplateOptions : ScriptOptionsBase
    {
    }

    [Verb("translation-progress", HelpText = "Updates the translation progress file")]
    public class TranslationProgressOptions : ScriptOptionsBase
    {
    }

    [Verb("credits", HelpText = "Updates credits with some automatically (and some needing manual) retrieved files")]
    public class CreditsOptions : ScriptOptionsBase
    {
    }

    [Verb("wiki", HelpText = "Updates the Thriveopedia with content from the online wiki")]
    public class WikiOptions : ScriptOptionsBase
    {
    }

    [Verb("generate", HelpText = "Generates various kinds of files")]
    public class GeneratorOptions : ScriptOptionsBase
    {
        [Value(0, MetaName = "TYPE", Required = true,
            HelpText = "Which type of file to generate ('List' prints a list of available types)")]
        public FileTypeToGenerate Type { get; set; }
    }

    [Verb("make-project-valid", HelpText = "Makes the Godot project valid for C# compile")]
    public class GodotProjectValidMakerOptions : ScriptOptionsBase
    {
    }
}
