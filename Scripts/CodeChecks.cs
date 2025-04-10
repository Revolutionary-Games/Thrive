namespace Scripts;

using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using ScriptsBase.Checks;
using ScriptsBase.Checks.FileTypes;
using ScriptsBase.Models;
using ScriptsBase.Utilities;

public class CodeChecks : CodeChecksBase<Program.CheckOptions>
{
    private static readonly string[] FilesNotAllowedToHaveBom = { "global.json", "dotnet-tools.json" };

    public CodeChecks(Program.CheckOptions opts,
        Func<LocalizationOptionsBase, CancellationToken, Task<bool>> runLocalizationTool) :
        base(opts)
    {
        var inspectCode = new InspectCode();

        // Full paths are a bit not helpful in CI output
        if (IsRunningInCI())
        {
            inspectCode.DisableFullPathPrinting();
        }

        var thriveVersion = AssemblyInfoReader.ReadVersionFromCsproj("Thrive.csproj");

        ValidChecks = new Dictionary<string, CodeCheck>
        {
            {
                "files",
                new FileChecks(true,
                    new BomChecker(BomChecker.Mode.Required, ".cs", ".json")
                    {
                        IgnoredFiles = new List<string>(FilesNotAllowedToHaveBom),
                    },
                    new BomChecker(BomChecker.Mode.Disallowed, FilesNotAllowedToHaveBom),
                    new CfgCheck(thriveVersion),
                    new ProjectGodotCheck(thriveVersion),
                    new DisallowedFileType(".gd", ".mo", ".gltf")
                    {
                        ExtraErrorMessages =
                        {
                            { ".gltf", "glTF files should be compressed into .glb files to save space" },
                        },
                    })
            },
            { "compile", new CompileCheck(!opts.NoExtraRebuild) },
            { "inspectcode", inspectCode },
            { "cleanupcode", new CleanupCode() },
            { "localization", new LocalizationCheck(runLocalizationTool) },
            { "steam-build", new SteamBuildCheck() },
            { "rewrite", new RewriteTool() },
        };

        FilePathsToAlwaysIgnore.Add(new Regex("/?third_party/", RegexOptions.IgnoreCase));
        FilePathsToAlwaysIgnore.Add(new Regex("/?addons/", RegexOptions.IgnoreCase));
        FilePathsToAlwaysIgnore.Add(new Regex(@"mono_crash\..+"));
        FilePathsToAlwaysIgnore.Add(new Regex(@"RevolutionaryGamesCommon\/"));

        // Downloaded JSON files
        FilePathsToAlwaysIgnore.Add(new Regex(@"patrons\.json"));
        FilePathsToAlwaysIgnore.Add(new Regex(@"translators\.json"));

        // Generated JSON files that are intentionally minimized
        FilePathsToAlwaysIgnore.Add(new Regex(@"older_patch_notes\.json$"));
        FilePathsToAlwaysIgnore.Add(new Regex(@"extension_api\.json$"));

        FilePathsToAlwaysIgnore.Add(new Regex(@"gdunit4_testadapter\/"));

        // We ignore the .godot folder as it has godot temporary data and a bunch of files that don't conform to any
        // styles
        FilePathsToAlwaysIgnore.Add(new Regex(@"\.godot\/"));

        FilePathsToAlwaysIgnore.Add(new Regex(@"Scripts\/GodotAPIData"));

        FilePathsToAlwaysIgnore.Add(new Regex(@"\.generated\.cs$"));
    }

    protected override Dictionary<string, CodeCheck> ValidChecks { get; }

    protected override IEnumerable<string> ExtraIgnoredJetbrainsInspectWildcards =>
    [
        "third_party/**",
        "RevolutionaryGamesCommon/**",
        "src/native/**.cpp",
        "src/native/**.hpp",
        "third_party/**.hpp",
        "Scripts/GodotAPIData/*",
        "addons/**",
        "*.generated.cs",
    ];

    protected override IEnumerable<string> ExtraIgnoredJetbrainsCleanUpWildcards =>
    [
        "third_party/boost/**",
        "third_party/concurrentqueue/**",
        "third_party/JoltPhysics/**",
        "third_party/godot-cpp/**",
        "Scripts/GodotAPIData/*",
        "addons/**",
        "*.generated.cs",
    ];

    protected override string MainSolutionFile => "Thrive.sln";
}
