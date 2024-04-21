using System;
using System.Diagnostics;
using Godot;
using Newtonsoft.Json;

/// <summary>
///   Handles starting the game with features disabled if there have been failed startup attempts
/// </summary>
public static class SafeModeStartupHandler
{
    private static readonly Lazy<StartupAttemptInfo?> PreviousFailedStartup = new(LoadExistingStartupInfo);
    private static readonly StartupAttemptInfo CurrentStartup = new();

    private static bool startupSucceeded;

    private static bool loggedModsDisabled;
    private static bool loggedVideosDisabled;

    private static bool wroteInfoFile;

    public static bool ModLoadingSkipped { get; set; }

    public static bool AreVideosAllowed()
    {
        // If mods are in safe mode, assume videos don't need to be
        if (!AreModsAllowed())
            return true;

        var previous = PreviousFailedStartup.Value;

        if (previous == null)
            return true;

        if (previous.VideosEnabled)
        {
            if (!loggedVideosDisabled)
            {
                GD.PrintErr("Due to safe startup mode videos are disabled");
                loggedVideosDisabled = true;
            }

            return false;
        }

        return true;
    }

    public static bool AreModsAllowed()
    {
        // Allow mod loading to happen after the initial load if the user goes to the mod manager and applies changes
        if (startupSucceeded)
            return true;

        var previous = PreviousFailedStartup.Value;

        if (previous == null)
            return true;

        if (previous.ModsEnabled)
        {
            if (!loggedModsDisabled)
            {
                GD.PrintErr("Due to safe startup mode mods are prevented from loading");
                loggedModsDisabled = true;
            }

            return false;
        }

        return true;
    }

    public static bool StartedInSafeMode()
    {
        return !AreVideosAllowed() || !AreModsAllowed();
    }

    public static void ReportModLoadingStart()
    {
        // There's no check for wroteInfoFile as we assume this is always called before ReportBeforeVideoPlaying is
        // potentially called
        if (startupSucceeded)
            return;

        CurrentStartup.ModsEnabled = true;

        // Detect the video playing option to write correctly to the info file
        CurrentStartup.VideosEnabled = Settings.Instance.PlayIntroVideo && LaunchOptions.VideosEnabled;

        SaveCurrentStartupInfo();
    }

    public static void ReportBeforeVideoPlaying()
    {
        if (wroteInfoFile || startupSucceeded)
            return;

        CurrentStartup.VideosEnabled = true;

        SaveCurrentStartupInfo();
    }

    public static void ReportGameStartSuccessful()
    {
        startupSucceeded = true;

        DeleteCurrentStartupInfoFile();

        if (!LaunchOptions.LaunchedThroughLauncher)
        {
            GD.Print(Constants.STARTUP_SUCCEEDED_MESSAGE);
        }
        else
        {
            GD.PrintErr("The following is not an error, but is printed as an error to ensure launcher always " +
                "sees it without buffering:");
            GD.PrintErr(Constants.STARTUP_SUCCEEDED_MESSAGE);
        }
    }

    private static StartupAttemptInfo? LoadExistingStartupInfo()
    {
        using var file = FileAccess.Open(Constants.STARTUP_ATTEMPT_INFO_FILE, FileAccess.ModeFlags.Read);
        if (file == null)
        {
            // No previous startup info
            return null;
        }

        try
        {
            var data = file.GetAsText();

            var previous = JsonConvert.DeserializeObject<StartupAttemptInfo>(data);

            if (previous == null)
                return null;

            GD.Print($"Found failed Thrive start at {previous.StartedAt.ToLocalTime():G}, " +
                "will likely enter safe mode for this launch");

            return previous;
        }
        catch (Exception e)
        {
            GD.PrintErr($"Failed to load previous startup info file for safe mode detection: {e}");
            return null;
        }
    }

    private static void SaveCurrentStartupInfo()
    {
        // Ensure previous info is loaded before we write a fresh file
        _ = PreviousFailedStartup.Value;

        wroteInfoFile = true;

        // When using a debugger, we don't want to do safe mode start ups
        if (Debugger.IsAttached)
        {
            GD.Print("Not writing startup info as debugger is attached");
            return;
        }

        using var file = FileAccess.Open(Constants.STARTUP_ATTEMPT_INFO_FILE, FileAccess.ModeFlags.Write);
        if (file == null)
        {
            GD.PrintErr("Failed to open startup info file for writing");
            return;
        }

        file.StoreString(JsonConvert.SerializeObject(CurrentStartup));
    }

    private static void DeleteCurrentStartupInfoFile()
    {
        if (!FileAccess.FileExists(Constants.STARTUP_ATTEMPT_INFO_FILE))
            return;

        GD.Print("Startup successful, removing startup info file");
        if (DirAccess.RemoveAbsolute(Constants.STARTUP_ATTEMPT_INFO_FILE) != Error.Ok)
            GD.PrintErr("Failed to delete startup info file, game will incorrectly enter safe mode on next start");
    }
}
