using System.Globalization;
using Godot;

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
        GD.Print("This is Thrive version: ", Constants.Version);

        GD.Print("Startup C# locale is: ", CultureInfo.CurrentCulture, " Godot locale is: ",
            TranslationServer.GetLocale());

        var userDir = OS.GetUserDataDir().Replace('\\', '/');

        GD.Print("user:// directory is: ", userDir);

        // Print the logs folder to see in the output where they are stored
        GD.Print("Game logs are written to: ", PathUtils.Join(userDir, Constants.LOGS_FOLDER_NAME),
            " latest log is 'log.txt'");

        // Load settings here, to make sure locales etc. are applied to the main loaded and autoloaded scenes
        if (Settings.Instance == null)
            GD.PrintErr("Failed to initialize settings.");
    }
}
