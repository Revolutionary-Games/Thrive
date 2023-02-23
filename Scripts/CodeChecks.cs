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
    public CodeChecks(Program.CheckOptions opts,
        Func<LocalizationOptionsBase, CancellationToken, Task<bool>> runLocalizationTool) :
        base(opts)
    {
        ValidChecks = new Dictionary<string, CodeCheck>
        {
            {
                "files",
                new FileChecks(true,
                    new BomChecker(BomChecker.Mode.Required, ".cs", ".json")
                    {
                        IgnoredFiles = new List<string> { "global.json" },
                    },
                    new BomChecker(BomChecker.Mode.Disallowed, "global.json"),
                    new CfgCheck(AssemblyInfoReader.ReadVersionFromAssemblyInfo()),
                    new DisallowedFileType(".gd", ".mo"))
            },
            { "compile", new CompileCheck() },
            { "inspectcode", new InspectCode() },
            { "cleanupcode", new CleanupCode() },
            { "localization", new LocalizationCheck(runLocalizationTool) },
            { "steam-build", new SteamBuildCheck() },
            { "rewrite", new RewriteTool() },
        };

        FilePathsToAlwaysIgnore.Add(new Regex(@"/?third_party/", RegexOptions.IgnoreCase));
        FilePathsToAlwaysIgnore.Add(new Regex(@"mono_crash\..+"));
        FilePathsToAlwaysIgnore.Add(new Regex(@"RevolutionaryGamesCommon/"));

        // Downloaded json files
        FilePathsToAlwaysIgnore.Add(new Regex(@"patrons\.json"));
        FilePathsToAlwaysIgnore.Add(new Regex(@"translators\.json"));

        // Generated json files that are intentionally minimized
        FilePathsToAlwaysIgnore.Add(new Regex(@"older_patch_notes\.json$"));

        // We ignore the .import files for now as checking those takes quite a bit of time
        FilePathsToAlwaysIgnore.Add(new Regex(@"\.import$"));
    }

    protected override Dictionary<string, CodeCheck> ValidChecks { get; }

    protected override IEnumerable<string> ExtraIgnoredJetbrainsInspectWildcards => new[]
    {
        "third_party/*",
        "RevolutionaryGamesCommon/*",
    };

    protected override string MainSolutionFile => "Thrive.sln";
}
