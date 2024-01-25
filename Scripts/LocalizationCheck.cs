namespace Scripts;

using System;
using System.Threading;
using System.Threading.Tasks;
using ScriptsBase.Checks;
using ScriptsBase.Models;

public class LocalizationCheck : LocalizationCheckBase
{
    private const string LocalizationCommand = "\"dotnet run --project Scripts -- localization\"";

    public LocalizationCheck(Func<LocalizationOptionsBase, CancellationToken, Task<bool>> runLocalizationTool) : base(
        runLocalizationTool)
    {
    }

    public override async Task Run(CodeCheckRun runData, CancellationToken cancellationToken)
    {
        await base.Run(runData, cancellationToken);

        if (cancellationToken.IsCancellationRequested)
            return;

        if (issuesFound)
        {
            runData.ReportError("Translations are not up to date");

            runData.OutputInfoWithMutex("Please verify your installed gettext tools are new enough and " +
                $"rerun {LocalizationCommand}");
        }
    }
}
