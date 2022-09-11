namespace Scripts;

using System;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using ScriptsBase.Utilities;

public static class BuildInfoWriter
{
    public const string BUILD_INFO_LOCATION = "simulation_parameters/revision.json";

    public static Task WriteBuildInfo(string commit, string branch, bool devbuild, CancellationToken cancellationToken)
    {
        var info = new BuildInfo(commit, branch, DateTime.UtcNow, devbuild);

        return JsonWriteHelper.WriteJsonWithBom(BUILD_INFO_LOCATION, info, cancellationToken);
    }

    /// <summary>
    ///   Needs to match the class in the simulation_parameters folder
    /// </summary>
    private class BuildInfo
    {
        [JsonConstructor]
        public BuildInfo(string commit, string branch, DateTime builtAt, bool devBuild)
        {
            Commit = commit;
            Branch = branch;
            BuiltAt = builtAt;
            DevBuild = devBuild;
        }

        [JsonInclude]
        public string Commit { get; }

        [JsonInclude]
        public string Branch { get; }

        [JsonInclude]
        public DateTime BuiltAt { get; }

        [JsonInclude]
        public bool DevBuild { get; }
    }
}
