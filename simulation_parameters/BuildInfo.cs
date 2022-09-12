using System;
using Newtonsoft.Json;

/// <summary>
///   Some general info about when the game was exported (only really valid in exported versions of the game as the
///   package script is the only place that updates this)
/// </summary>
public class BuildInfo : IRegistryType
{
    public BuildInfo(string commit, string branch, DateTime builtAt, bool devBuild)
    {
        Commit = commit;
        Branch = branch;
        BuiltAt = builtAt;
        DevBuild = devBuild;
    }

    [JsonProperty]
    public string Commit { get; }

    [JsonProperty]
    public string Branch { get; }

    [JsonProperty]
    public DateTime BuiltAt { get; }

    [JsonProperty]
    public bool DevBuild { get; }

    /// <summary>
    ///   Unused
    /// </summary>
    public string InternalName { get; set; } = null!;

    public void Check(string name)
    {
        if (string.IsNullOrEmpty(Commit))
        {
            throw new InvalidRegistryDataException("BuildInfo", GetType().Name,
                "commit is empty");
        }

        if (string.IsNullOrEmpty(Branch))
        {
            throw new InvalidRegistryDataException("BuildInfo", GetType().Name,
                "branch is empty");
        }
    }

    public void ApplyTranslations()
    {
    }
}
