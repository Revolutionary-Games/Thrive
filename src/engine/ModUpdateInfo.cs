using System;
using Godot;
using Newtonsoft.Json;

public class ModUpdateInfo : Resource
{
    [JsonProperty("Thrive Version")]
    public string ThriveVersion { get; set; }

    [JsonProperty("Latest Stable")]
    public string LatestStableVersion { get; set; }

    [JsonProperty("Latest Unstable")]
    public string LatestUnstableVersion { get; set; }

    [JsonProperty("Download URL")]
    public Uri DownloadUrl { get; set; }

    public override bool Equals(object other)
    {
        var item = other as ModUpdateInfo;

        if (item == null)
        {
            return false;
        }

        return ThriveVersion == item.ThriveVersion && LatestStableVersion == item.LatestStableVersion &&
            LatestUnstableVersion == item.LatestUnstableVersion && DownloadUrl == item.DownloadUrl;
    }

    public override int GetHashCode()
    {
        return (ThriveVersion, LatestStableVersion, LatestUnstableVersion, DownloadUrl).GetHashCode();
    }
}
