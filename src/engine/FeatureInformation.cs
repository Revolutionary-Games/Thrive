using System;
using System.Runtime.CompilerServices;
using Godot;

/// <summary>
///   Helpers for querying the Godot features.
///   https://docs.godotengine.org/en/stable/getting_started/workflow/export/feature_tags.html#feature-tags
/// </summary>
public static class FeatureInformation
{
    private const string PlatformWindows = "windows";
    private const string PlatformLinux = "linux";

    private const string PlatformMac = "macos";

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

    public static bool IsMobile => OperatingSystem.IsIOS() || OperatingSystem.IsAndroid();

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
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

    public static bool IsWindows()
    {
        return GetOS() == PlatformWindows;
    }

    public static bool IsMac()
    {
        return GetOS() == PlatformMac;
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
        var renderer = RenderingServer.GetCurrentRenderingDriverName();
        switch (renderer)
        {
            case "vulkan":
                return OS.RenderingDriver.Vulkan;
            case "d3d12":
                return OS.RenderingDriver.D3D12;
            case "metal":
                return OS.RenderingDriver.Metal;
            case "opengl3":
                return OS.RenderingDriver.Opengl3;

            // These are theoretically slightly different, but Godot doesn't provide enum values for them
            case "opengl3_es":
                return OS.RenderingDriver.Opengl3;
            case "opengl3_angle":
                return OS.RenderingDriver.Opengl3;

            default:
                GD.PrintErr("Unknown rendering driver name: ", renderer);
                break;
        }

        // Fall back to basic detection if name-matching failed

        if (RenderingServer.GetRenderingDevice() == null)
            return OS.RenderingDriver.Opengl3;

        return OS.RenderingDriver.Vulkan;
    }
}
