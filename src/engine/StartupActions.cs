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

        bool skipNative = false;

        try
        {
            NativeInterop.Load();
        }
        catch (DllNotFoundException)
        {
            if (Engine.EditorHint)
            {
                skipNative = true;
                GD.Print("Skipping native library load in editor as it is not available");
            }
            else
            {
                GD.PrintErr("Native library is missing (or unloadable). If you downloaded a compiled Thrive " +
                    "version, this version is broken. If you are trying to compile Thrive you need to compile the " +
                    "native modules as well");
                GD.PrintErr("Please do not report to us the next unhandled exception error about this, unless " +
                    "this is an official Thrive release that has this issue");
                throw;
            }
        }

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

        if (!skipNative)
            NativeInterop.Init(Settings.Instance);
    }
}
