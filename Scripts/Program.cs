﻿using System;
using System.Diagnostics;
using CommandLine;
using Scripts;
using ScriptsBase.Models;
using ScriptsBase.Utilities;
using SharedBase.Utilities;

public class Program
{
    [STAThread]
    public static int Main(string[] args)
    {
        RunFolderChecker.EnsureRightRunningFolder("Thrive.sln");

        var result = CommandLineHelpers.CreateParser()
            .ParseArguments<CheckOptions, TestOptions, ChangesOptions, LocalizationOptions, CleanupOptions,
                PackageOptions, UploadOptions, ContainerOptions, SteamOptions>(args)
            .MapResult(
                (CheckOptions options) => RunChecks(options),
                (TestOptions options) => RunTests(options),
                (ChangesOptions options) => RunChangesFinding(options),
                (LocalizationOptions options) => RunLocalization(options),
                (CleanupOptions options) => RunCleanup(options),
                (PackageOptions options) => RunPackage(options),
                (UploadOptions options) => RunUpload(options),
                (ContainerOptions options) => RunContainer(options),
                (SteamOptions options) => SetSteamOptions(options),
                CommandLineHelpers.PrintCommandLineErrors);

        ConsoleHelpers.CleanConsoleStateForExit();

        return result;
    }

    private static int RunChecks(CheckOptions options)
    {
        CommandLineHelpers.HandleDefaultOptions(options);

        ColourConsole.WriteDebugLine("Running in check mode");
        ColourConsole.WriteDebugLine($"Manually specified checks: {string.Join(' ', options.Checks)}");

        var checker = new CodeChecks(options);

        return checker.Run().Result;
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

        throw new NotImplementedException();

        // var checker = new IconProcessor(options);
        //
        // return checker.Run(tokenSource.Token).Result ? 0 : 1;
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

    public class CheckOptions : CheckOptionsBase
    {
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

        public override bool Compress => CompressRaw == true;
    }

    [Verb("upload", HelpText = "Upload created devbuilds to ThriveDevCenter")]
    public class UploadOptions : ScriptOptionsBase
    {
    }

    public class ContainerOptions : ContainerOptionsBase
    {
    }

    [Verb("steam", HelpText = "Control Steam build variant building")]
    public class SteamOptions : ScriptOptionsBase
    {
        [Value(0, MetaName = "Mode", Required = true, HelpText = "Which mode to set Thrive to")]
        public string Mode { get; set; } = string.Empty;
    }
}
