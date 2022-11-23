﻿using System;
using System.Linq;
using Godot;

/// <summary>
///   Provides access to Godot launch options
/// </summary>
public static class LaunchOptions
{
    private static readonly Lazy<string[]> GodotLaunchOptions = new(OS.GetCmdlineArgs);
    private static readonly Lazy<bool> DisableVideosOption = new(ReadDisableVideo);

    private static readonly Lazy<bool> LaunchedThroughLauncherHolder = new(ReadLaunchedThroughLauncher);
    private static readonly Lazy<bool> LaunchingLauncherIsHiddenHolder = new(ReadLaunchingLauncherIsHidden);

    private static readonly Lazy<string?> StoreNameHolder = new(ReadStoreName);

    public static bool VideosEnabled => !DisableVideosOption.Value;

    public static bool LaunchedThroughLauncher => LaunchedThroughLauncherHolder.Value;

    public static bool LaunchingLauncherIsHidden => LaunchedThroughLauncher && LaunchingLauncherIsHiddenHolder.Value;

    public static string? StoreVersionName => StoreNameHolder.Value;

    private static bool ReadDisableVideo()
    {
        bool value = GodotLaunchOptions.Value.Any(o => o == Constants.DISABLE_VIDEOS_LAUNCH_OPTION);

        if (value)
            GD.Print("Videos are disabled with a command line option");

        return value;
    }

    private static bool ReadLaunchedThroughLauncher()
    {
        bool value = GodotLaunchOptions.Value.Any(o => o == Constants.OPENED_THROUGH_LAUNCHER_OPTION);

        if (value)
            GD.Print("We were opened through the Thrive Launcher");

        return value;
    }

    private static bool ReadLaunchingLauncherIsHidden()
    {
        bool value = GodotLaunchOptions.Value.Any(o => o == Constants.OPENING_LAUNCHER_IS_HIDDEN);

        if (value)
            GD.Print("Launcher opening us is hidden");

        return value;
    }

    private static string? ReadStoreName()
    {
        var rawValue =
            GodotLaunchOptions.Value.FirstOrDefault(o => o.StartsWith(Constants.THRIVE_LAUNCHER_STORE_PREFIX));

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
            GD.Print("Detected store name from launch parameters: ");
            return value;
        }

        return null;
    }
}
