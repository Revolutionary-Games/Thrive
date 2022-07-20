using Godot;

/// <summary>
///   Crash logger for any unhandled Mono exceptions
/// </summary>
public class CrashLogger
{
    public static void UnhandledException(object sender, UnhandledExceptionArgs args)
    {
        GD.PrintErr("------------ Begin of Crash Log ------------\n" +
            "The following exception prevented the game from running.\n\n" +
            $"{args.Exception}\n\n" +
            "Please provide us with this log, thank you.\n" +
            "------------  End of Crash Log  ------------");
    }
}
