using System;
using Godot;

/// <summary>
///   Helpers for querying the Godot features.
///   https://docs.godotengine.org/en/stable/getting_started/workflow/export/feature_tags.html#feature-tags
/// </summary>
public static class FeatureInformation
{
    private const string PlatformWindows = "windows";
    private const string PlatformLinux = "linux";

    // TODO: check that this is correct for Godot 4
    private const string PlatformMac = "osx";

    private static readonly Lazy<OS.RenderingDriver> CachedDriver = new(DetectRenderer);

    private static readonly Lazy<string> ResolvedOS = new(GetOSHelper);

    private static readonly string[] SimpleFeaturePlatforms =
    {
        PlatformLinux,
        PlatformWindows,
        PlatformMac,

        // TODO: check that these are correct for Godot 4
        "android",
        "html5",
        "iOS",
    };

    public static string GetOS()
    {
        return ResolvedOS.Value;
    }

    public static OS.RenderingDriver GetVideoDriver()
    {
        return CachedDriver.Value;
    }

    public static bool IsLinux()
    {
        return GetOS() == PlatformLinux;
    }

    private static string GetOSHelper()
    {
        foreach (var feature in SimpleFeaturePlatforms)
        {
            if (OS.HasFeature(feature))
                return feature;
        }

        GD.PrintErr("unknown current OS");
        return "unknown";
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
