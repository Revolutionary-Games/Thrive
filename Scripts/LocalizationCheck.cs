namespace Scripts;

using System;
using System.Threading;
using System.Threading.Tasks;
using ScriptsBase.Checks;
using ScriptsBase.Models;
using ScriptsBase.Utilities;

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

            var (matches, installed, wanted) =
                await PipPackageVersionChecker.CompareInstalledBabelThriveVersion(cancellationToken);

            runData.OutputTextWithMutex($"Babel-Thrive version installed: {installed}; required: {wanted}");

            if (matches)
            {
                runData.OutputInfoWithMutex(
                    "Please verify your Babel-Thrive version (and gettext tools) meets the requirement and " +
                    $"rerun {LocalizationCommand}");
            }
            else
            {
                runData.OutputErrorWithMutex("Mismatching Babel-Thrive version detected. Please update " +
                    $"\"pip install -r docker/ci/requirements.txt --user\" and rerun {LocalizationCommand}");
            }
        }
    }
}
