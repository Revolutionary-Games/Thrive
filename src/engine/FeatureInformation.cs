using System;
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

    private static readonly Lazy<OS.RenderingDriver> CachedDriver = new(DetectRenderer);

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

    public static OS.RenderingDriver GetVideoDriver()
    {
        return CachedDriver.Value;
    }

    private static OS.RenderingDriver DetectRenderer()
    {
        // TODO: switch to a proper approach when Godot adds support for reading this
        // For now OpenGL is detected by not having available to the modern rendering engine
        if (RenderingServer.GetRenderingDevice() == null)
            return OS.RenderingDriver.Opengl3;

        return OS.RenderingDriver.Vulkan;
    }
}
