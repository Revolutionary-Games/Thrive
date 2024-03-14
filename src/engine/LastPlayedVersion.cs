using System;
using System.Linq;
using Godot;

/// <summary>
///   Loads and allows updating the last played Thrive version
/// </summary>
public static class LastPlayedVersion
{
    private static Lazy<string?> lastPlayed = new(ReadLastPlayedVersion);

    private static bool handledCurrentVersionPlayed;

    public static string? LastPlayed => lastPlayed.Value;

    public static void MarkCurrentVersionAsPlayed()
    {
        if (handledCurrentVersionPlayed)
            return;

        handledCurrentVersionPlayed = true;

        // Don't write an older latest played version
        var version = Constants.Version;

        if (LastPlayed != null && VersionUtils.Compare(version, LastPlayed) <= 0)
        {
            GD.Print("We are not playing a newer Thrive version than last played, not updating latest");
            return;
        }

        Invoke.Instance.Queue(WriteCurrentVersion);

        // Override the last played value for the current run if something will still read this (for example exiting
        // back to the menu)
        lastPlayed = new Lazy<string?>(() => version);
    }

    private static string? ReadLastPlayedVersion()
    {
        using var file = FileAccess.Open(Constants.LAST_PLAYED_VERSION_FILE, FileAccess.ModeFlags.Read);

        if (file == null)
        {
            // File doesn't exist or we can't read it, no last played version
            return null;
        }

        var version = file.GetLine()?.Trim();

        if (string.IsNullOrEmpty(version))
        {
            GD.PrintErr("Read last played version file, but we got just a blank line");
            return null;
        }

        // Test that the version number is correct
        try
        {
            // Parsing the first part as valid version number needs to work
            Version.Parse(version!.Split('-').First());

            // Version comparison needs to work
            if (VersionUtils.Compare("1.0.0", version) == int.MaxValue)
            {
                // The failing comparison already prints an error message
                return null;
            }
        }
        catch (Exception e)
        {
            GD.PrintErr($"Last played version is corrupt ({version}): {e}");
            return null;
        }

        return version;
    }

    private static void WriteCurrentVersion()
    {
        var version = Constants.Version;
        GD.Print($"Saving latest played Thrive version to be: {version}");

        using var file = FileAccess.Open(Constants.LAST_PLAYED_VERSION_FILE, FileAccess.ModeFlags.Write);

        if (file == null)
        {
            GD.PrintErr("Failed to open latest played version file for writing");
            return;
        }

        file.StoreLine(version);
    }
}
