﻿using System.Text;
using Godot;

/// <summary>
///   Unhandled exception logger for any unhandled C# exceptions
/// </summary>
public static class UnhandledExceptionLogger
{
    private static bool modsEnabled;

    public static void OnUnhandledException(object sender, UnhandledExceptionArgs args)
    {
        var builder = new StringBuilder(500);
        builder.Append("------------ Begin of Unhandled Exception Log ------------\n");

        if (modsEnabled)
        {
            builder.Append("The following exception (potentially from a mod) prevented the game from running:\n\n");
        }
        else
        {
            builder.Append("The following exception prevented the game from running:\n\n");
        }

        builder.Append(args.Exception);

        if (modsEnabled)
        {
            builder.Append(
                "\n\nPlease provide us with this log after making sure that a loaded mod didn't cause this, " +
                "thank you.\n");
            builder.Append("If this problem was caused by a mod, please send your report instead to the mod author.\n");
        }
        else
        {
            builder.Append("\n\nPlease provide us with this log, thank you.\n");
        }

        builder.Append("------------  End of Unhandled Exception Log  ------------");

        GD.PrintErr(builder.ToString());
    }

    /// <summary>
    ///   Called by the mod loader to report when mods are loaded. This modifies the message printing to make it clear
    ///   that there are enabled mods.
    /// </summary>
    public static void ReportModsEnabled()
    {
        if (modsEnabled)
            return;

        GD.Print($"Mods reported enabled to {nameof(UnhandledExceptionLogger)}");
        modsEnabled = true;
    }
}
