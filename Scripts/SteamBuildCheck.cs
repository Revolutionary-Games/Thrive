namespace Scripts;

using System.Threading;
using System.Threading.Tasks;
using ScriptsBase.Checks;

public class SteamBuildCheck : CodeCheck
{
    public override async Task Run(CodeCheckRun runData, CancellationToken cancellationToken)
    {
        if (await SteamBuild.IsSteamBuildEnabled(cancellationToken))
            runData.ReportError("Steam build is enabled, it should not be enabled when committing");
    }
}
