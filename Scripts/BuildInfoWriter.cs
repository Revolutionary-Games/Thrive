namespace Scripts;

using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using ScriptsBase.Utilities;
using ThriveScriptsShared;

public static class BuildInfoWriter
{
    public static Task WriteBuildInfo(string commit, string branch, bool devbuild, CancellationToken cancellationToken)
    {
        var info = new BuildInfo(commit, branch, DateTime.UtcNow, devbuild);

        return JsonWriteHelper.WriteJsonWithBom(ThriveScriptConstants.BUILD_INFO_FILE, info, cancellationToken);
    }

    public static void DeleteBuildInfo()
    {
        if (File.Exists(ThriveScriptConstants.BUILD_INFO_FILE))
            File.Delete(ThriveScriptConstants.BUILD_INFO_FILE);
    }
}
