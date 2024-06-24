using System;
using System.Linq;
using Godot;
using LauncherThriveShared;

/// <summary>
///   Provides access to Godot launch options
/// </summary>
public static class LaunchOptions
{
    private static readonly Lazy<string[]> GodotLaunchOptions = new(OS.GetCmdlineArgs);
    private static readonly Lazy<bool> DisableVideosOption = new(ReadDisableVideo);
    private static readonly Lazy<bool> SkipCPUCheckOption = new(ReadSkipCPUCheck);
    private static readonly Lazy<bool> DisableAvxOption = new(ReadDisableAvx);

    private static readonly Lazy<bool> LaunchedThroughLauncherHolder = new(ReadLaunchedThroughLauncher);
    private static readonly Lazy<bool> LaunchingLauncherIsHiddenHolder = new(ReadLaunchingLauncherIsHidden);

    private static readonly Lazy<string?> StoreNameHolder = new(ReadStoreName);

    private static readonly Lazy<string> LaunchId = new(ReadOrGenerateLaunchId);

    public static bool VideosEnabled => !DisableVideosOption.Value;

    public static bool SkipCPUCheck => SkipCPUCheckOption.Value;

    public static bool ForceDisableAvx => DisableAvxOption.Value;

    public static bool LaunchedThroughLauncher => LaunchedThroughLauncherHolder.Value;

    public static bool LaunchingLauncherIsHidden => LaunchedThroughLauncher && LaunchingLauncherIsHiddenHolder.Value;

    public static string? StoreVersionName => StoreNameHolder.Value;

    /// <summary>
    ///   Unique identifier for the current Thrive launch. Either communicated from the launcher or a random GUID.
    /// </summary>
    public static string StartId => LaunchId.Value;

    private static bool ReadDisableVideo()
    {
        bool value = GodotLaunchOptions.Value.Any(o => o == ThriveLauncherSharedConstants.DISABLE_VIDEOS_LAUNCH_OPTION);

        if (value)
            GD.Print("Videos are disabled with a command line option");

        return value;
    }

    private static bool ReadLaunchedThroughLauncher()
    {
        bool value =
            GodotLaunchOptions.Value.Any(o => o == ThriveLauncherSharedConstants.OPENED_THROUGH_LAUNCHER_OPTION);

        if (value)
            GD.Print("We were opened through the Thrive Launcher");

        return value;
    }

    private static bool ReadSkipCPUCheck()
    {
        bool value = GodotLaunchOptions.Value.Any(o => o == ThriveLauncherSharedConstants.SKIP_CPU_CHECK_OPTION);

        if (value)
            GD.Print("CPU feature check is disabled with a command line option");

        return value;
    }

    private static bool ReadDisableAvx()
    {
        bool value = GodotLaunchOptions.Value.Any(o => o == ThriveLauncherSharedConstants.DISABLE_CPU_AVX_OPTION);

        if (value)
            GD.Print("AVX CPU feature usage is disabled with a command line option");

        return value;
    }

    private static bool ReadLaunchingLauncherIsHidden()
    {
        bool value = GodotLaunchOptions.Value.Any(o => o == ThriveLauncherSharedConstants.OPENING_LAUNCHER_IS_HIDDEN);

        if (value)
            GD.Print("Launcher opening us is hidden");

        return value;
    }

    private static string? ReadStoreName()
    {
        var rawValue =
            GodotLaunchOptions.Value.FirstOrDefault(o =>
                o.StartsWith(ThriveLauncherSharedConstants.THRIVE_LAUNCHER_STORE_PREFIX));

        string? value = null;

        if (rawValue != null)
        {
            var splits = rawValue.Split('=');

            if (splits.Length == 2)
            {
                value = splits[1];
            }
            else
            {
                GD.PrintErr("Bad format for value in store name parameter: ", rawValue);
            }
        }

        if (!string.IsNullOrWhiteSpace(value))
        {
            GD.Print("Detected store name from launch parameters: ", value);
            return value;
        }

        return null;
    }

    private static string ReadOrGenerateLaunchId()
    {
        var rawValue =
            GodotLaunchOptions.Value.FirstOrDefault(o =>
                o.StartsWith(ThriveLauncherSharedConstants.THRIVE_LAUNCH_ID_PREFIX));

        string? value = null;

        if (rawValue != null)
        {
            var splits = rawValue.Split('=');

            if (splits.Length == 2)
            {
                value = splits[1];
            }
            else
            {
                GD.PrintErr("Bad format for value in launch id parameter: ", rawValue);
            }
        }

        if (!string.IsNullOrWhiteSpace(value))
        {
            GD.Print("Detected launch ID from parameters: ", value);
            return value;
        }

        return Guid.NewGuid().ToString();
    }
}
