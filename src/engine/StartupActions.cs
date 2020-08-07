using Godot;

/// <summary>
///   This is the first autoloaded class. Used to perform some actions that should happen
///   as the first things in the game
/// </summary>
public class StartupActions : Node
{
    private StartupActions()
    {
        var userDir = OS.GetUserDataDir().Replace('\\', '/');

        GD.Print("user:// directory is: ", userDir);

        // Print the logs folder to see in the output where they are stored
        GD.Print("Game logs are written to: ", PathUtils.Join(userDir, Constants.LOGS_FOLDER_NAME),
            " latest log is 'log.txt'");
    }
}
