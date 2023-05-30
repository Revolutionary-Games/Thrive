using System;
using System.Diagnostics;
using System.Globalization;
using Godot;
using Path = System.IO.Path;

/// <summary>
///   This is the first autoloaded class. Used to perform some actions that should happen
///   as the first things in the game
/// </summary>
public class StartupActions : Node
{
    private StartupActions()
    {
        // Print game version
        // TODO: for devbuilds it would be nice to print the hash here
        GD.Print("This is Thrive version: ", Constants.Version, " (see below for exact build info)");

        // Add unhandled exception logger if debugger is not attached
        if (!Debugger.IsAttached)
        {
            GD.UnhandledException += UnhandledExceptionLogger.OnUnhandledException;
            GD.Print("Unhandled exception logger attached");
        }

        GD.Print("Startup C# locale is: ", CultureInfo.CurrentCulture, " Godot locale is: ",
            TranslationServer.GetLocale());

        var userDir = Constants.UserFolderAsNativePath;

        GD.Print("user:// directory is: ", userDir);

        // Print the logs folder to see in the output where they are stored
        GD.Print("Game logs are written to: ", Path.Combine(userDir, Constants.LOGS_FOLDER_NAME),
            " latest log is 'log.txt'");

        NativeInterop.Load();

        // Load settings here, to make sure locales etc. are applied to the main loaded and autoloaded scenes
        try
        {
            // We just want to do something to ensure settings instance is fine
            // ReSharper disable once UnusedVariable
            var hashCode = Settings.Instance.GetHashCode();
        }
        catch (Exception e)
        {
            GD.PrintErr("Failed to initialize settings: ", e);
        }

        NativeInterop.Init(Settings.Instance);
    }
}
