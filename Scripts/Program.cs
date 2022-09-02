using System;
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
                PackageOptions,
                UploadOptions>(args)
            .MapResult(
                (CheckOptions opts) => RunChecks(opts),
                (TestOptions opts) => RunTests(opts),
                (ChangesOptions opts) => RunChangesFinding(opts),
                (LocalizationOptions opts) => RunLocalization(opts),
                (PackageOptions opts) => RunPackage(opts),
                (UploadOptions opts) => RunUpload(opts),
                CommandLineHelpers.PrintCommandLineErrors);

        ConsoleHelpers.CleanConsoleStateForExit();

        return result;
    }

    private static int RunChecks(CheckOptions opts)
    {
        CommandLineHelpers.HandleDefaultOptions(opts);

        ColourConsole.WriteDebugLine("Running in check mode");
        ColourConsole.WriteDebugLine($"Manually specified checks: {string.Join(' ', opts.Checks)}");

        var checker = new CodeChecks(opts);

        return checker.Run().Result;
    }

    private static int RunTests(TestOptions opts)
    {
        CommandLineHelpers.HandleDefaultOptions(opts);

        ColourConsole.WriteDebugLine("Running dotnet tests");

        var tokenSource = ConsoleHelpers.CreateSimpleConsoleCancellationSource();

        return ProcessRunHelpers.RunProcessAsync(new ProcessStartInfo("dotnet", "test"), tokenSource.Token, false)
            .Result.ExitCode;
    }

    private static int RunChangesFinding(ChangesOptions opts)
    {
        CommandLineHelpers.HandleDefaultOptions(opts);

        ColourConsole.WriteDebugLine("Running changes finding tool");

        return OnlyChangedFileDetector.BuildListOfChangedFiles(opts).Result ? 0 : 1;
    }

    private static int RunPackage(PackageOptions opts)
    {
        CommandLineHelpers.HandleDefaultOptions(opts);

        ColourConsole.WriteDebugLine("Running packaging tool");

        var tokenSource = ConsoleHelpers.CreateSimpleConsoleCancellationSource();

        throw new NotImplementedException();

        // var checker = new IconProcessor(opts);
        //
        // return checker.Run(tokenSource.Token).Result ? 0 : 1;
    }

    private static int RunLocalization(LocalizationOptions opts)
    {
        CommandLineHelpers.HandleDefaultOptions(opts);

        ColourConsole.WriteDebugLine("Running localization update tool");

        var tokenSource = ConsoleHelpers.CreateSimpleConsoleCancellationSource();

        throw new NotImplementedException();

        // var checker = new IconProcessor(opts);
        //
        // return checker.Run(tokenSource.Token).Result ? 0 : 1;
    }

    private static int RunUpload(UploadOptions opts)
    {
        CommandLineHelpers.HandleDefaultOptions(opts);

        ColourConsole.WriteDebugLine("Running upload tool");

        var tokenSource = ConsoleHelpers.CreateSimpleConsoleCancellationSource();

        throw new NotImplementedException();

        // var checker = new IconProcessor(opts);
        //
        // return checker.Run(tokenSource.Token).Result ? 0 : 1;
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

    [Verb("localization", HelpText = "Update localization files")]
    public class LocalizationOptions : ScriptOptionsBase
    {
    }

    [Verb("cleanup", HelpText = "Cleanup Godot temporary files")]
    public class CleanupOptions : ScriptOptionsBase
    {
    }

    public class PackageOptions : PackageOptionsBase
    {
    }

    [Verb("upload", HelpText = "Upload created devbuilds to ThriveDevCenter")]
    public class UploadOptions : ScriptOptionsBase
    {
    }
}
