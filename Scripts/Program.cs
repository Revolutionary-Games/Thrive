using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
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

        // This has too many verbs now, so some more manual work is required here as this has run out of the template
        // arguments available from the library
        var parserResult = CommandLineHelpers.CreateParser()
            .ParseArguments(args, typeof(CheckOptions), typeof(NativeLibOptions), typeof(TestOptions),
                typeof(ChangesOptions), typeof(LocalizationOptions), typeof(CleanupOptions), typeof(PackageOptions),
                typeof(UploadOptions), typeof(ContainerOptions), typeof(SteamOptions), typeof(GodotTemplateOptions),
                typeof(TranslationProgressOptions), typeof(CreditsOptions), typeof(WikiOptions),
                typeof(GeneratorOptions), typeof(GodotProjectValidMakerOptions));

        int result;
        if (parserResult is Parsed<object> parsed)
        {
            result = parsed.Value switch
            {
                CheckOptions value => RunChecks(value),
                NativeLibOptions value => RunNativeLibsTool(value),
                TestOptions value => RunTests(value),
                ChangesOptions value => RunChangesFinding(value),
                LocalizationOptions value => RunLocalization(value),
                CleanupOptions value => RunCleanup(value),
                PackageOptions value => RunPackage(value),
                UploadOptions value => RunUpload(value),
                ContainerOptions value => RunContainer(value),
                SteamOptions value => SetSteamOptions(value),
                GodotTemplateOptions value => RunTemplateInstall(value),
                TranslationProgressOptions value => RunTranslationProgress(value),
                CreditsOptions value => RunCreditsUpdate(value),
                WikiOptions value => RunWikiUpdate(value),
                GeneratorOptions value => RunFileGenerator(value),
                GodotProjectValidMakerOptions value => RunProjectValidMaker(value),
                _ => throw new InvalidOperationException(),
            };
        }
        else
        {
            result = CommandLineHelpers.PrintCommandLineErrors(((NotParsed<object>)parserResult).Errors);
        }

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

        ColourConsole.WriteInfoLine("Running 'dotnet test'");

        var godot = ExecutableFinder.Which("godot");

        if (string.IsNullOrEmpty(godot))
        {
            ColourConsole.WriteErrorLine("Could not find 'godot' executable, make sure it is in PATH");
            return 2;
        }

        TestRunningHelpers.GenerateRunSettings(godot, AssemblyInfoReader.ReadRunTimeFromCsproj("Thrive.csproj"), false);

        var tokenSource = ConsoleHelpers.CreateSimpleConsoleCancellationSource();

        int result = -1;
        const int maxTries = 2;

        // gdUnit can randomly fail once to detect available tests, that's why the tests run multiple times on fail
        // (which is not ideal, but it should hopefully be relatively rare for the tests to actually fail for real)
        for (int i = 0; i < maxTries; ++i)
        {
            var startInfo = new ProcessStartInfo("dotnet");
            startInfo.ArgumentList.Add("test");
            startInfo.ArgumentList.Add("--settings");
            startInfo.ArgumentList.Add(TestRunningHelpers.RUN_SETTINGS_FILE);
            startInfo.ArgumentList.Add("--verbosity");
            startInfo.ArgumentList.Add("normal");

            result = ProcessRunHelpers.RunProcessAsync(startInfo, tokenSource.Token, false)
                .Result.ExitCode;

            if (result == 0)
                break;

            if (i + 1 < maxTries)
            {
                ColourConsole.WriteErrorLine("Failed to run tests, retrying");
            }
        }

        // Edit the gdUnit wrapper to suppress warnings in it
        if (File.Exists("gdunit4_testadapter/GdUnit4TestRunnerScene.cs"))
            TestRunningHelpers.EnsureStartsWithPragmaSuppression("gdunit4_testadapter/GdUnit4TestRunnerScene.cs");

        return result;
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

        var tool = new WikiUpdater();

        return tool.Run(tokenSource.Token).Result ? 0 : 1;
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

        ColourConsole.WriteInfoLine("Attempting to compile C# Thrive code with Godot");

        var tokenSource = ConsoleHelpers.CreateSimpleConsoleCancellationSource();

        var tool = new GodotProjectCompiler(options);

        return tool.Run(tokenSource.Token).Result;
    }

    public class CheckOptions : CheckOptionsBase;

    [Verb("native", HelpText = "Handling for native libraries needed by Thrive")]
    public class NativeLibOptions : SymbolUploadOptionsBase
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
            ///   Installs a library to work with Godot editor (only needed for specific libraries)
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

            /// <summary>
            ///   Just upload only symbols that exist locally but are missing from the server
            /// </summary>
            Symbols,
        }

        [Usage(ApplicationAlias = "dotnet run --project Scripts --")]
        public static IEnumerable<Example> Examples
        {
            get
            {
                yield return new Example("download all available libraries",
                    new NativeLibOptions { Operations = [OperationMode.Fetch], Url = string.Empty });
                yield return new Example("compile and install libraries locally",
                    new NativeLibOptions
                        { Operations = [OperationMode.Build, OperationMode.Install], Url = string.Empty });
                yield return new Example("prepare library versions for distribution or uploading with podman",
                    new NativeLibOptions { Operations = [OperationMode.Package], Url = string.Empty });
                yield return new Example("build only release mode libraries",
                    new NativeLibOptions
                        { Operations = [OperationMode.Build], Url = string.Empty, DebugLibrary = false });
            }
        }

        [Value(0, MetaName = "OPERATIONS", Required = false, HelpText = "What native operation(s) to do")]
        public IList<OperationMode> Operations { get; set; } = new List<OperationMode> { OperationMode.Check };

        [Option('l', "library", Required = false, Default = null, MetaValue = "LIBRARIES",
            HelpText = "Libraries to work on, default is all.")]
        public IList<NativeConstants.Library>? Libraries { get; set; } = new List<NativeConstants.Library>();

        [Option('d', "debug", Required = false, Default = null,
            HelpText = "Set to false or true to only use debug mode or disable it. Default is to do both.")]
        public bool? DebugLibrary { get; set; }

        [Option("disable-avx", Required = false, Default = false,
            HelpText = "Disable building locally with AVX (container builds always make both variants)")]
        public bool DisableLocalAvx { get; set; }

        [Option('t', "platform", Required = false, Default = null,
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

        [Option("prepare-api-file", Required = false, Default = true,
            HelpText = "Can be set to false to skip preparing Godot API files")]
        public bool PrepareGodotAPI { get; set; }
    }

    [Verb("test", HelpText = "Run tests using 'dotnet' command")]
    public class TestOptions : ScriptOptionsBase;

    public class ChangesOptions : ChangesOptionsBase
    {
        [Option('b', "branch", Required = false, Default = "master", HelpText = "The git remote branch name")]
        public override string RemoteBranch { get; set; } = "master";
    }

    public class LocalizationOptions : LocalizationOptionsBase;

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

        [Option("mac-signing-key", Default = null,
            HelpText = "Use a specific signing key for mac builds (defaults to 'SelfSigned')")]
        public string? MacSigningKey { get; set; }

        [Option("apple-team-id", Default = null, HelpText = "Specify Apple developer team ID for signing purposes")]
        public string? MacTeamId { get; set; }

        [Option("app-notarization-user", Default = null,
            HelpText = "Specify Apple developer account email for signing")]
        public string? AppleId { get; set; }

        [Option("app-specific-password", Default = null, HelpText = "Apple developer account login")]
        public string? AppleAppPassword { get; set; }

        [Option("skip-godot-check", Default = false,
            HelpText = "Skip checking if godot is installed and correct version and just try to use it")]
        public bool SkipGodotCheck { get; set; }

        public override bool Compress => CompressRaw == true;
    }

    [Verb("upload", HelpText = "Upload created devbuilds to ThriveDevCenter")]
    public class UploadOptions : ScriptOptionsBase
    {
        [Option('k', "key", Required = false, Default = null, MetaValue = "KEY",
            HelpText = "Set to a valid DevCenter key (not user token) to use non-anonymous uploading.")]
        public string? Key { get; set; }

        [Option('u', "devcenter-url", Required = false, Default = Uploader.DEFAULT_DEVCENTER_URL,
            MetaValue = "DEVCENTER_URL", HelpText = "DevCenter URL to upload to.")]
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
    public class GodotTemplateOptions : ScriptOptionsBase;

    [Verb("translation-progress", HelpText = "Updates the translation progress file")]
    public class TranslationProgressOptions : ScriptOptionsBase;

    [Verb("credits", HelpText = "Updates credits with some automatically (and some needing manual) retrieved files")]
    public class CreditsOptions : ScriptOptionsBase;

    [Verb("wiki", HelpText = "Updates the Thriveopedia with content from the online wiki")]
    public class WikiOptions : ScriptOptionsBase;

    [Verb("generate", HelpText = "Generates various kinds of files")]
    public class GeneratorOptions : ScriptOptionsBase
    {
        [Value(0, MetaName = "TYPE", Required = true,
            HelpText = "Which type of file to generate ('List' prints a list of available types)")]
        public FileTypeToGenerate Type { get; set; }
    }

    [Verb("make-project-valid", HelpText = "Makes the Godot project valid for C# compile (deprecated)")]
    public class GodotProjectValidMakerOptions : ScriptOptionsBase;
}
