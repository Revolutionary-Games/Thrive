using System;
using System.Linq;
using Godot;

/// <summary>
///   Provides access to Godot launch options
/// </summary>
public static class LaunchOptions
{
    private static readonly Lazy<string[]> GodotLaunchOptions = new(OS.GetCmdlineArgs);
    private static readonly Lazy<bool> DisableVideosOption = new(ReadDisableVideo);

    public static bool VideosEnabled => !DisableVideosOption.Value;

    private static bool ReadDisableVideo()
    {
        bool value = GodotLaunchOptions.Value.Any(o => o == Constants.DISABLE_VIDEOS_LAUNCH_OPTION);

        if (value)
            GD.Print("Videos are disabled with a command line option");

        return value;
    }
}
