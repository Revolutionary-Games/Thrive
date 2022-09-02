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

    protected override IEnumerable<string> ExtraIgnoredJetbrainsInspectWildcards => new[] { "third_party/*" };

    protected override string MainSolutionFile => "Thrive.sln";
}
