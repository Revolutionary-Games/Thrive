using Godot;

/// <summary>
///   Helpers for querying the Godot features.
///   https://docs.godotengine.org/en/stable/getting_started/workflow/export/feature_tags.html#feature-tags
/// </summary>
public static class FeatureInformation
{
    private static readonly string[] SimpleFeaturePlatforms =
    {
        "Android",
        "HTML5",
        "Windows",
        "OSX",
        "iOS",
    };

    public static string GetOS()
    {
        if (OS.HasFeature("X11"))
            return "Linux";

        foreach (var feature in SimpleFeaturePlatforms)
        {
            if (OS.HasFeature(feature))
                return feature;
        }

        GD.PrintErr("unknown current OS");
        return "unknown";
    }
}
