using Godot;

/// <summary>
///   Unhandled exception logger for any unhandled Mono exceptions
/// </summary>
public class UnhandledExceptionLogger
{
    public static void UnhandledException(object sender, UnhandledExceptionArgs args)
    {
        GD.PrintErr("------------ Begin of Unhandled Exception Log ------------\n",
            "The following exception prevented the game from running:\n\n",
            args.Exception.ToString(),
            "\n\nPlease provide us with this log, thank you.\n",
            "------------  End of Unhandled Exception Log  ------------");
    }
}
