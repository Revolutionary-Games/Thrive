namespace Scripts;

using System.Threading;
using System.Threading.Tasks;
using ScriptsBase.Checks;
using ScriptsBase.Utilities;

public class LocalizationCheck : LocalizationCheckBase
{
    private const string LocalizationCommand = "\"dotnet run --project Scripts -- localization\"";

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
                runData.OutputInfoWithMutex("Please verify your Babel-Thrive version meets teh requirement and " +
                    $"rerun {LocalizationCommand}");
            }
            else
            {
                runData.OutputErrorWithMutex($"Mismatching Babel-Thrive version detected. Please update " +
                    $"\"pip install -r docker/lint/requirements.txt --user\" and rerun {LocalizationCommand}");
            }
        }
    }
}
