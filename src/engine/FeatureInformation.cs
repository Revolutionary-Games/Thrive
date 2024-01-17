using Godot;

/// <summary>
///   Helpers for querying the Godot features.
///   https://docs.godotengine.org/en/stable/getting_started/workflow/export/feature_tags.html#feature-tags
/// </summary>
public static class FeatureInformation
{
    public const string PlatformWindows = "Windows";
    public const string PlatformLinux = "Linux";
    public const string PlatformMac = "OSX";

    private static readonly string[] SimpleFeaturePlatforms =
    {
        "Android",
        "HTML5",
        PlatformWindows,
        PlatformMac,
        "iOS",
    };

    public static string GetOS()
    {
        // TODO: fix this for Godot 4
        if (OS.HasFeature("X11"))
            return PlatformLinux;

        foreach (var feature in SimpleFeaturePlatforms)
        {
            if (OS.HasFeature(feature))
                return feature;
        }

        GD.PrintErr("unknown current OS");
        return "unknown";
    }
}
