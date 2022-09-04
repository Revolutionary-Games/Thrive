namespace Scripts;

using System.Collections.Generic;
using System.Text.RegularExpressions;
using ScriptsBase.Checks;
using ScriptsBase.Checks.FileTypes;
using ScriptsBase.Utilities;

public class CodeChecks : CodeChecksBase<Program.CheckOptions>
{
    public CodeChecks(Program.CheckOptions opts) : base(opts)
    {
        FilePathsToAlwaysIgnore.Add(new Regex(@"/?third_party/", RegexOptions.IgnoreCase));
        FilePathsToAlwaysIgnore.Add(new Regex(@"mono_crash\..+"));
        FilePathsToAlwaysIgnore.Add(new Regex(@"RevolutionaryGamesCommon/"));

        // We ignore the .import files for now as checking those takes quite a bit of time
        FilePathsToAlwaysIgnore.Add(new Regex(@"\.import$"));
    }

    protected override Dictionary<string, CodeCheck> ValidChecks { get; } = new()
    {
        {
            "files",
            new FileChecks(true,
                new BomChecker(BomChecker.Mode.Required, ".cs"),
                new CfgCheck(AssemblyInfoReader.ReadVersionFromAssemblyInfo()),
                new DisallowedFileType(".gd", ".mo"))
        },
        { "compile", new CompileCheck() },
        { "inspectcode", new InspectCode() },
        { "cleanupcode", new CleanupCode() },
        { "localization", new LocalizationCheck() },
        { "steam-build", new SteamBuildCheck() },
    };

    protected override IEnumerable<string> ExtraIgnoredJetbrainsInspectWildcards => new[]
    {
        "third_party/*",
        "RevolutionaryGamesCommon/*",
    };

    protected override string MainSolutionFile => "Thrive.sln";
}
